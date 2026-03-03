using System;
using System.Windows;
using ReTime_Testing.Helpers;
using ReTime_Testing.Models;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.TimeTopDesktop
{
    /// <summary>
    /// 底部进度条窗口
    /// </summary>
    public partial class TimeTopDesktop_Bottom : Window
    {
        private TimeTopDesktopViewModel? _viewModel;

        /// <summary>
        /// 窗口位置：底部
        /// </summary>
        public ProgressBarPosition Position => ProgressBarPosition.Bottom;

        public TimeTopDesktop_Bottom()
        {
            InitializeComponent();
            _viewModel = new TimeTopDesktopViewModel();
            DataContext = _viewModel;

            // 应用标准样式
            DesktopWindowHelper.ApplyStandardStyles(this);

            // 设置窗口位置
            DesktopWindowHelper.SetWindowPosition(this, Position);

            // 注册窗口关闭事件
            Closing += TimeTopDesktop_Bottom_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 设置工具窗口样式（必须在 OnSourceInitialized 之后调用）
            DesktopWindowHelper.SetToolWindowStyle(this);
        }

        private void TimeTopDesktop_Bottom_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 清理 ViewModel 资源
            _viewModel?.Cleanup();
        }
    }
}