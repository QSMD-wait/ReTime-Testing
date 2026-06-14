using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.Settings
{
    public partial class Setting : Window
    {
        private TimeTopSettingViewModel? _viewModel;

        public Setting()
        {
            InitializeComponent();

            var app = System.Windows.Application.Current as App;
            var services = app?.Services ?? throw new InvalidOperationException("DI 容器未初始化");

            _viewModel = new TimeTopSettingViewModel(
                services.GetRequiredService<IGlobalTimeTopDesktopService>(),
                services.GetRequiredService<IMutexManager>(),
                services.GetRequiredService<ISettingsService>(),
                services.GetRequiredService<IDesktopWindowManager>()
            );
            DataContext = _viewModel;

            Closing += Setting_Closing;

            MainNavigation.SelectionChanged += MainNavigation_SelectionChanged;

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
            _viewModel?.Cleanup();
            MainNavigation.SelectionChanged -= MainNavigation_SelectionChanged;
        }
    }
}