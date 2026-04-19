using System;
using System.Windows;
using System.Windows.Threading;
using ReTime_Testing.Helpers;
using ReTime_Testing.Models;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.TimeTopDesktop
{
    /// <summary>
    /// 顶部进度条窗口
    /// </summary>
    public partial class TimeTopDesktop : Window
    {
        private TimeTopDesktopViewModel? _viewModel;
        private readonly DispatcherTimer _autoCloseTimer;

        /// <summary>
        /// 窗口位置：顶部
        /// </summary>
        public ProgressBarPosition Position => ProgressBarPosition.Top;

        public TimeTopDesktop()
        {
            InitializeComponent();
            _viewModel = new TimeTopDesktopViewModel();
            DataContext = _viewModel;

            // 应用标准样式
            DesktopWindowHelper.ApplyStandardStyles(this);

            // 设置窗口位置
            DesktopWindowHelper.SetWindowPosition(this, Position);

            // 注册窗口关闭事件
            Closing += TimeTopDesktop_Closing;

// 启动10秒后隐藏测试文本的定时器
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer.Stop();
                TestText.Visibility = Visibility.Collapsed;
            };
            _autoCloseTimer.Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 设置工具窗口样式（必须在 OnSourceInitialized 之后调用）
            DesktopWindowHelper.SetToolWindowStyle(this);
        }

        private void TimeTopDesktop_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 清理 ViewModel 资源
            _viewModel?.Cleanup();
        }
    }
}