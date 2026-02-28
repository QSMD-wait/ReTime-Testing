using System;
using System.Windows;
using ReTime_Testing.Helpers;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views
{
    public partial class TimeTopDesktop : Window
    {
        private TimeTopDesktopViewModel? _viewModel;

        public TimeTopDesktop()
        {
            InitializeComponent();
            _viewModel = new TimeTopDesktopViewModel();
            DataContext = _viewModel;

            // 获取主屏幕工作区域
            var workingArea = SystemParameters.WorkArea;

            // 设置窗口宽度为屏幕宽度，高度为40
            WindowHelper.SetWindowPosition(this, 0, 0, workingArea.Width, 400);

            // 注册窗口关闭事件
            Closing += TimeTopDesktop_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 设置窗口为工具窗口，使窗口不在任务栏和 Alt+Tab 中显示
            WindowHelper.SetToolWindowStyle(this);
        }

        private void TimeTopDesktop_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 清理 ViewModel 资源
            _viewModel?.Cleanup();
        }
    }
}