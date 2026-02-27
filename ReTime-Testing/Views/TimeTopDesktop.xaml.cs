using System.Windows;

namespace ReTime_Testing.Views
{
    public partial class TimeTopDesktop : Window
    {
        public TimeTopDesktop()
        {
            InitializeComponent();
            DataContext = new ViewModels.TimeTopDesktopViewModel();

            // 获取主屏幕工作区域
            var workingArea = SystemParameters.WorkArea;

            // 设置窗口宽度为屏幕宽度，高度为40
            Width = workingArea.Width;
            Height = 400;

            // 设置窗口贴顶显示
            Top = 0;
            Left = 0;
        }
    }
}