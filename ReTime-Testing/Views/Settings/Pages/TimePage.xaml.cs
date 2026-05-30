using System.Windows;
using System.Windows.Controls;

namespace ReTime_Testing.Views.Settings.Pages;

/// <summary>
/// TimePage.xaml 的交互逻辑
/// </summary>
public partial class TimePage : UserControl
{
    public TimePage()
    {
        InitializeComponent();
        Unloaded += TimePage_Unloaded;
    }

    private void TimePage_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.TimePageViewModel viewModel)
        {
            viewModel.Dispose();
        }
    }
}