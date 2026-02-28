using System.Windows;
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
            Width = workingArea.Width;
            Height = 400;

            // 设置窗口贴顶显示
            Top = 0;
            Left = 0;

            // 注册窗口关闭事件
            Closing += TimeTopDesktop_Closing;
        }

        private void TimeTopDesktop_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 清理 ViewModel 资源
            _viewModel?.Cleanup();
        }
    }
}