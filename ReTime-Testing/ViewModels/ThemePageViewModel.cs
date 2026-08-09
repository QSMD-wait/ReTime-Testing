using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ReTime_Testing.Core.Models.Theme;
using ReTime_Testing.Core.Services;

namespace ReTime_Testing.ViewModels;

public partial class ThemePageViewModel : ObservableObject
{
    private readonly IProgressBarThemeService _themeService;
    private bool _isInitializing = true;

    [ObservableProperty]
    private ProgressBarThemeManifest _currentTheme = new();

    [ObservableProperty]
    private IReadOnlyList<ProgressBarThemeManifest> _availableThemes = Array.Empty<ProgressBarThemeManifest>();

    [ObservableProperty]
    private string _selectedThemeId = "default";

    [ObservableProperty]
    private bool _isThirdPartyFeatureEnabled = false;

    public ThemePageViewModel(IProgressBarThemeService themeService)
    {
        _themeService = themeService;

        AvailableThemes = _themeService.AvailableThemes;
        CurrentTheme = _themeService.CurrentTheme;
        SelectedThemeId = _themeService.CurrentThemeId;

        _isInitializing = false;
    }

    partial void OnSelectedThemeIdChanged(string value)
    {
        if (_isInitializing) return;
        if (string.IsNullOrWhiteSpace(value)) return;

        try
        {
            _themeService.ApplyTheme(value);
            CurrentTheme = _themeService.CurrentTheme;
        }
        catch (Exception)
        {
            CurrentTheme = _themeService.CurrentTheme;
        }
    }
}