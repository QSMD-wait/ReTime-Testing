using System.Windows;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.Settings
{
    public partial class Setting : Window
    {
        private TimeTopSettingViewModel? _viewModel;

        public Setting()
        {
            InitializeComponent();
            _viewModel = new TimeTopSettingViewModel();
            DataContext = _viewModel;

            // 注册窗口关闭事件
            Closing += Setting_Closing;

            // 注册导航事件
            MainNavigation.SelectionChanged += MainNavigation_SelectionChanged;

            // 默认选中第一个导航项（会触发 SelectionChanged → NavigateTo，无需额外调用 InitializeNavigation）
            MainNavigation.SelectedItem = MainNavigation.MenuItems[0];
        }

        private void MainNavigation_SelectionChanged(object sender, System.EventArgs e)
        {
            if (MainNavigation.SelectedItem != null)
            {
                var tagProperty = MainNavigation.SelectedItem.GetType().GetProperty("Tag");
                string tag = tagProperty?.GetValue(MainNavigation.SelectedItem)?.ToString() ?? string.Empty;
                _viewModel?.NavigateTo(tag);
            }
        }

        private void Setting_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 清理 ViewModel 资源
            _viewModel?.Cleanup();

            // 取消导航事件订阅
            MainNavigation.SelectionChanged -= MainNavigation_SelectionChanged;
        }
    }
}