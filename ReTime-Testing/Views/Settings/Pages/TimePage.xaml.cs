using System.Windows;

namespace ReTime_Testing.Views.Settings.Pages;

public partial class TimePage : SettingsPageBase
{
    public TimePage()
    {
        InitializeComponent();
    }

    protected override void OnPageNavigatedTo(SettingsNavigationContext context)
    {
        if (DataContext is ViewModels.TimePageViewModel viewModel)
        {
            viewModel.ResumeTimer();
        }
    }

    protected override void OnPageNavigatedFrom(SettingsNavigationContext context)
    {
        if (DataContext is ViewModels.TimePageViewModel viewModel)
        {
            viewModel.PauseTimer();
        }
    }
}