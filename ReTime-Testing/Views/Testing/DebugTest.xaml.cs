using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.Testing
{
    public partial class DebugTest : Window
    {
        public DebugTest()
        {
            InitializeComponent();

            if (DataContext is null)
            {
                var app = System.Windows.Application.Current as App;
                var services = app?.Services ?? throw new InvalidOperationException("DI 容器未初始化");

                DataContext = new TimeTopSettingViewModel(
                    services.GetRequiredService<IGlobalTimeTopDesktopService>(),
                    services.GetRequiredService<IMutexManager>(),
                    services.GetRequiredService<ISettingsService>(),
                    services.GetRequiredService<IDesktopWindowManager>()
                );
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (DataContext is TimeTopSettingViewModel viewModel)
            {
                viewModel.Cleanup();
            }
        }
    }
}