using System.Windows;

namespace ReTime_Testing.Views.Settings
{
    public partial class DebugTest : Window
    {
        public DebugTest()
        {
            InitializeComponent();
            
            // 设置 ViewModel
            if (DataContext is null)
            {
                DataContext = new ViewModels.TimeTopSettingViewModel();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // 清理 ViewModel 资源
            if (DataContext is ViewModels.TimeTopSettingViewModel viewModel)
            {
                viewModel.Cleanup();
            }
        }
    }
}