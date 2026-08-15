using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Models;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels
{
    /// <summary>
    /// 日志查看器 ViewModel
    /// 职责：订阅 Logger 内存缓冲，按等级/关键字过滤展示
    /// </summary>
    public partial class LogViewerViewModel : ObservableObject, IDisposable
    {
        private const string LOG_TAG = "LogViewerViewModel";
        private const int MaxVisibleEntries = 1000;

        private readonly Dispatcher _dispatcher;
        private readonly List<LogEntryItem> _allEntries = new();
        private bool _disposed;

        public ObservableCollection<LogEntryItem> Entries { get; } = new();

        // ==================== 过滤条件 ====================

        [ObservableProperty]
        private bool _isErrorFilterEnabled = true;

        [ObservableProperty]
        private bool _isWarningFilterEnabled = true;

        [ObservableProperty]
        private bool _isInfoFilterEnabled = true;

        [ObservableProperty]
        private bool _isDebugFilterEnabled = false;

        [ObservableProperty]
        private bool _isTraceFilterEnabled = false;

        [ObservableProperty]
        private string _keyword = string.Empty;

        [ObservableProperty]
        private bool _autoScroll = true;

        public int VisibleCount => Entries.Count;

        public LogViewerViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            Logger.LogEntryAdded += OnLogEntryAdded;

            foreach (var entry in Logger.GetRecentLogEntries())
            {
                _allEntries.Add(entry);
            }
            RebuildVisible();
        }

        partial void OnIsErrorFilterEnabledChanged(bool value) => RebuildVisible();

        partial void OnIsWarningFilterEnabledChanged(bool value) => RebuildVisible();

        partial void OnIsInfoFilterEnabledChanged(bool value) => RebuildVisible();

        partial void OnIsDebugFilterEnabledChanged(bool value) => RebuildVisible();

        partial void OnIsTraceFilterEnabledChanged(bool value) => RebuildVisible();

        partial void OnKeywordChanged(string value) => RebuildVisible();

        // ==================== 日志接收 ====================

        private void OnLogEntryAdded(LogEntryItem entry)
        {
            if (_disposed) return;

            try
            {
                _dispatcher.BeginInvoke(new Action(() =>
                {
                    _allEntries.Add(entry);
                    if (IsMatch(entry))
                    {
                        Entries.Add(entry);
                        TrimExcess();
                        OnPropertyChanged(nameof(VisibleCount));
                    }
                }));
            }
            catch
            {
                // 分发失败（窗口关闭等）时忽略
            }
        }

        private bool IsMatch(LogEntryItem entry)
        {
            if (!IsLevelEnabled(entry.Level)) return false;

            if (string.IsNullOrWhiteSpace(Keyword)) return true;

            return entry.Message.Contains(Keyword, StringComparison.OrdinalIgnoreCase)
                || entry.Module.Contains(Keyword, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsLevelEnabled(LogLevel level)
        {
            return level switch
            {
                LogLevel.ERR => IsErrorFilterEnabled,
                LogLevel.WRN => IsWarningFilterEnabled,
                LogLevel.INF => IsInfoFilterEnabled,
                LogLevel.DBG => IsDebugFilterEnabled,
                LogLevel.TRC => IsTraceFilterEnabled,
                _ => true
            };
        }

        private void RebuildVisible()
        {
            if (_disposed) return;

            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.BeginInvoke(new Action(RebuildVisible));
                return;
            }

            Entries.Clear();
            foreach (var entry in _allEntries)
            {
                if (IsMatch(entry))
                {
                    Entries.Add(entry);
                }
            }
            TrimExcess();
            OnPropertyChanged(nameof(VisibleCount));
        }

        private void TrimExcess()
        {
            while (Entries.Count > MaxVisibleEntries)
            {
                Entries.RemoveAt(0);
            }
        }

        // ==================== 操作命令 ====================

        [RelayCommand]
        private void ClearLogs()
        {
            Logger.ClearLogBuffer();
            _allEntries.Clear();
            Entries.Clear();
            Logger.Info(LOG_TAG, "日志查看器缓冲已清空");
        }

        [RelayCommand]
        private void CopySelected(IList? selectedItems)
        {
            if (selectedItems == null || selectedItems.Count == 0) return;

            try
            {
                var lines = new List<string>();
                foreach (var item in selectedItems)
                {
                    if (item is LogEntryItem entry)
                    {
                        lines.Add($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{entry.Level}] [{entry.Module}] {entry.Message}");
                    }
                }

                if (lines.Count > 0)
                {
                    System.Windows.Clipboard.SetText(string.Join(Environment.NewLine, lines));
                    Logger.Info(LOG_TAG, $"已复制 {lines.Count} 条日志到剪贴板");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "复制日志失败", ex);
            }
        }

        [RelayCommand]
        private void OpenLogDirectory()
        {
            try
            {
                var app = System.Windows.Application.Current as ReTime_Testing.App;
                var services = app?.Services;
                if (services == null) return;

                var configurationManager = services.GetService<IConfigurationManager>();
                var path = configurationManager?.LogsDirectory ?? string.Empty;

                if (Directory.Exists(path))
                {
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
                else
                {
                    Logger.Warn(LOG_TAG, $"日志目录不存在: {path}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "打开日志目录失败", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Logger.LogEntryAdded -= OnLogEntryAdded;
        }
    }
}