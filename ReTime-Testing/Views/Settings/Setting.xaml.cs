using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels;
using ReTime_Testing.Views.Settings.Pages;

namespace ReTime_Testing.Views.Settings;

public partial class Setting : Window
{
    private TimeTopSettingViewModel? _viewModel;
    private SettingsPageBase? _currentPage;

    public Setting()
    {
        InitializeComponent();

        var app = System.Windows.Application.Current as App;
        var services = app?.Services ?? throw new InvalidOperationException("DI 容器未初始化");

        _viewModel = ActivatorUtilities.CreateInstance<TimeTopSettingViewModel>(services);
        DataContext = _viewModel;

        Closing += Setting_Closing;

        CommandBindings.Add(new CommandBinding(SettingsPageBase.RequestRestartCommand, OnRequestRestart));
        CommandBindings.Add(new CommandBinding(SettingsPageBase.OpenDrawerCommand, OnOpenDrawer));
        CommandBindings.Add(new CommandBinding(SettingsPageBase.CloseDrawerCommand, OnCloseDrawer));

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

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        UpdateCurrentPage();
    }

    private void UpdateCurrentPage()
    {
        if (_currentPage != null)
        {
            _currentPage.NavigationContext = null;
        }

        _currentPage = FindSettingsPage(this);

        if (_currentPage != null)
        {
            var tag = MainNavigation.SelectedItem?.GetType().GetProperty("Tag")?.GetValue(MainNavigation.SelectedItem)?.ToString() ?? string.Empty;
            _currentPage.NavigationContext = new SettingsNavigationContext { PageTag = tag };
        }
    }

    private static SettingsPageBase? FindSettingsPage(DependencyObject root)
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is SettingsPageBase page)
            {
                return page;
            }

            var result = FindSettingsPage(child);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private async void OnRequestRestart(object sender, ExecutedRoutedEventArgs e)
    {
        var dialog = new iNKORE.UI.WPF.Modern.Controls.ContentDialog
        {
            Title = "重启应用",
            Content = "部分设置需要重启应用才能生效，是否立即重启？",
            PrimaryButtonText = "重启",
            CloseButtonText = "取消",
            DefaultButton = iNKORE.UI.WPF.Modern.Controls.ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();

        if (result == iNKORE.UI.WPF.Modern.Controls.ContentDialogResult.Primary)
        {
            System.Diagnostics.Process.Start(Environment.ProcessPath ?? System.Reflection.Assembly.GetEntryAssembly()?.Location ?? string.Empty);
            System.Windows.Application.Current.Shutdown();
        }
    }

    private void OnOpenDrawer(object sender, ExecutedRoutedEventArgs e)
    {
        // TODO: 抽屉机制实现
    }

    private void OnCloseDrawer(object sender, ExecutedRoutedEventArgs e)
    {
        // TODO: 抽屉机制实现
    }

    private void Setting_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_currentPage != null)
        {
            _currentPage.NavigationContext = null;
        }

        _viewModel?.Cleanup();
        MainNavigation.SelectionChanged -= MainNavigation_SelectionChanged;
    }
}