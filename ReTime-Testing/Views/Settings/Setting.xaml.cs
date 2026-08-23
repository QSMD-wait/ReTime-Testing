using System.Collections.Generic;
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

    /// <summary>
    /// 页面实例缓存：页面只创建一次，切换导航时复用，
    /// 避免每次进入页面都重建视觉树并重播控件入场动画（闪烁）
    /// </summary>
    private readonly Dictionary<string, SettingsPageBase> _pageCache = new();

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
            NavigateToPage(tag);
        }
    }

    /// <summary>
    /// 导航到指定页面：ViewModel 与页面实例均缓存复用，仅注入新的导航上下文
    /// </summary>
    private void NavigateToPage(string tag)
    {
        if (_viewModel == null) return;

        // 确保该页 ViewModel 已创建/缓存
        _viewModel.NavigateTo(tag);

        if (!_pageCache.TryGetValue(tag, out var page))
        {
            page = CreatePageForTag(tag);
            page.DataContext = _viewModel.CurrentPage;
            _pageCache[tag] = page;
        }

        // 通知旧页面离开，再切换内容并通知新页面进入
        if (_currentPage != null && !ReferenceEquals(_currentPage, page))
        {
            _currentPage.NavigationContext = null;
        }

        PageHost.Content = page;
        _currentPage = page;

        if (_currentPage.NavigationContext == null)
        {
            _currentPage.NavigationContext = new SettingsNavigationContext { PageTag = tag };
        }
    }

    private static SettingsPageBase CreatePageForTag(string tag) => tag switch
    {
        "Appearance" => new AppearancePage(),
        "Time" => new TimePage(),
        "TextOverlay" => new TextOverlayPage(),
        "TextOverlayLayout" => new TextOverlayLayoutPage(),
        "Window" => new WindowPage(),
        "Theme" => new ThemePage(),
        "About" => new AboutPage(),
        _ => new BasicPage()
    };

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
            System.Diagnostics.Process.Start(Environment.ProcessPath ?? string.Empty);
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
        foreach (var page in _pageCache.Values)
        {
            page.NavigationContext = null;
        }

        _viewModel?.Cleanup();
        MainNavigation.SelectionChanged -= MainNavigation_SelectionChanged;
    }
}