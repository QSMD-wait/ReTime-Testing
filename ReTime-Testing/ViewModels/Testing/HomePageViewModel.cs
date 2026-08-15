using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Services;
using System;
using System.IO;
using System.Windows.Threading;

namespace ReTime_Testing.ViewModels.Testing
{
    /// <summary>
    /// 调试测试首页 ViewModel
    /// 职责：核心服务运行状态总览 + 快捷操作
    /// </summary>
    public partial class HomePageViewModel : ObservableObject
    {
        private const string LOG_TAG = "HomePageViewModel";

        private readonly IMutexManager _mutexManager;
        private readonly IDesktopWindowManager _desktopWindowManager;
        private readonly IConfigurationManager _configurationManager;
        private readonly ITimeService? _timeService;
        private readonly IScheduleManager? _scheduleManager;
        private DispatcherTimer? _refreshTimer;

        public string TabTitle => "首页";

        // ==================== 时间服务 ====================

        [ObservableProperty]
        private string _currentTime = "未知";

        [ObservableProperty]
        private bool _isCloudSynchronized;

        // ==================== 调度管理器 ====================

        [ObservableProperty]
        private bool _isScheduleManagerRunning;

        [ObservableProperty]
        private int _timePointCount;

        [ObservableProperty]
        private string _currentSegmentName = "未知";

        [ObservableProperty]
        private string _currentState = "未知";

        [ObservableProperty]
        private string _nextTimePoint = "无";

        [ObservableProperty]
        private bool _isActiveSegment;

        // ==================== 互斥锁 ====================

        [ObservableProperty]
        private bool _isMutexAcquired;

        [ObservableProperty]
        private string _mutexStatus = "未知";

        // ==================== 进度条 ====================

        [ObservableProperty]
        private string _positionText = "未知";

        public HomePageViewModel(
            IMutexManager mutexManager,
            IDesktopWindowManager desktopWindowManager,
            IConfigurationManager configurationManager,
            ITimeService? timeService = null,
            IScheduleManager? scheduleManager = null)
        {
            _mutexManager = mutexManager;
            _desktopWindowManager = desktopWindowManager;
            _configurationManager = configurationManager;
            _timeService = timeService;
            _scheduleManager = scheduleManager;

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += OnRefreshTimerTick;
            _refreshTimer.Start();

            RefreshStatus();
        }

        // ==================== 状态刷新 ====================

        private void OnRefreshTimerTick(object? sender, EventArgs e)
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            try
            {
                if (_timeService != null)
                {
                    CurrentTime = _timeService.GetCurrentTime().ToString("HH:mm:ss");
                    IsCloudSynchronized = _timeService.IsCloudSynchronized;
                }
                else
                {
                    CurrentTime = "未初始化";
                    IsCloudSynchronized = false;
                }

                if (_scheduleManager != null)
                {
                    IsScheduleManagerRunning = _scheduleManager.IsRunning;
                    var plan = _scheduleManager.CurrentPlan;
                    if (plan != null)
                    {
                        TimePointCount = plan.TimePoints.Count;
                        CurrentSegmentName = plan.CurrentSegment?.Name ?? "未知";
                        CurrentState = plan.CurrentSegment?.State.ToString() ?? "未知";
                        NextTimePoint = plan.NextTimePoint?.Name ?? "无";
                        IsActiveSegment = plan.CurrentSegment?.IsActive ?? false;
                    }
                    else
                    {
                        CurrentSegmentName = "未加载";
                        CurrentState = "未加载";
                        NextTimePoint = "未加载";
                        IsActiveSegment = false;
                    }
                }
                else
                {
                    IsScheduleManagerRunning = false;
                    CurrentSegmentName = "未初始化";
                    CurrentState = "未初始化";
                    NextTimePoint = "未初始化";
                    IsActiveSegment = false;
                }

                IsMutexAcquired = _mutexManager.IsAcquired;
                MutexStatus = IsMutexAcquired ? "已获取" : "未获取";

                PositionText = _desktopWindowManager.CurrentPosition switch
                {
                    Models.ProgressBarPosition.Top => "顶部",
                    Models.ProgressBarPosition.Bottom => "底部",
                    Models.ProgressBarPosition.Left => "左侧",
                    Models.ProgressBarPosition.Right => "右侧",
                    _ => "未知"
                };
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "刷新服务状态时发生异常", ex);
            }
        }

        /// <summary>
        /// 资源清理
        /// </summary>
        public void Cleanup()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= OnRefreshTimerTick;
            }
        }

        // ==================== 快捷操作 ====================

        [RelayCommand]
        private void OpenDataDirectory()
        {
            OpenDirectory(_configurationManager.DataDirectory, "数据目录");
        }

        [RelayCommand]
        private void OpenLogDirectory()
        {
            OpenDirectory(_configurationManager.LogsDirectory, "日志目录");
        }

        [RelayCommand]
        private void RestartApplication()
        {
            try
            {
                System.Diagnostics.Process.Start(Environment.ProcessPath ?? string.Empty);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "重启应用失败", ex);
            }
        }

        private static void OpenDirectory(string path, string name)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
                else
                {
                    Logger.Warn(LOG_TAG, $"{name}不存在: {path}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, $"打开{name}失败", ex);
            }
        }
    }
}