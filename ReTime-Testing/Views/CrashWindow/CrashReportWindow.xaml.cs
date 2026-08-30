using System;
using System.Windows;
using ReTime_Testing.Services;

namespace ReTime_Testing.Views.CrashWindow
{
    /// <summary>
    /// 崩溃报告窗口：显示未处理异常的详细信息，提供复制、保存、忽略、重启、退出操作
    /// </summary>
    public partial class CrashReportWindow : Window
    {
        private readonly CrashReportService _crashService = new();

        /// <summary>
        /// 崩溃信息文本
        /// </summary>
        public string CrashInfo
        {
            get => (string)GetValue(CrashInfoProperty);
            set => SetValue(CrashInfoProperty, value);
        }

        public static readonly DependencyProperty CrashInfoProperty =
            DependencyProperty.Register(nameof(CrashInfo), typeof(string), typeof(CrashReportWindow),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// 是否允许忽略异常（非终止性异常时为 true）
        /// </summary>
        public bool AllowIgnore { get; set; }

        public CrashReportWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (AllowIgnore)
            {
                IgnoreButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 忽略异常，关闭崩溃窗口继续运行
        /// </summary>
        private void OnIgnoreClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 复制崩溃信息到剪贴板
        /// </summary>
        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(CrashInfo);
            }
            catch
            {
                // 剪贴板访问失败时静默忽略
            }
        }

        /// <summary>
        /// 保存崩溃日志到文件
        /// </summary>
        private void OnSaveLogClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePath = _crashService.SaveCrashLog(CrashInfo);
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    this,
                    $"崩溃日志已保存到：\n{filePath}",
                    "保存成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    this,
                    $"保存崩溃日志失败：\n{ex.Message}",
                    "保存失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 重启应用程序
        /// </summary>
        private void OnRestartClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = Application.Current as App;
                var mutexManager = app?.Services.GetService(typeof(IMutexManager)) as IMutexManager;
                Action? releaseMutex = mutexManager != null ? () => mutexManager.Release() : null;
                _crashService.RestartApplication(releaseMutex);
            }
            catch
            {
                _crashService.RestartApplication();
            }
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            _crashService.ExitApplication();
        }
    }
}
