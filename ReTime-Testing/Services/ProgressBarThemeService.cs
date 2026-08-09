using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using ReTime_Testing.Core.Models.Theme;
using ReTime_Testing.Core.Services;

namespace ReTime_Testing.Services;

public class ProgressBarThemeService : IProgressBarThemeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly IConfigurationManager _configManager;
    private readonly List<ProgressBarThemeManifest> _availableThemes = new();
    private ProgressBarThemeManifest _currentTheme = new();

    public string CurrentThemeId => _currentTheme.Id;

    public ProgressBarThemeManifest CurrentTheme => _currentTheme;

    public IReadOnlyList<ProgressBarThemeManifest> AvailableThemes => _availableThemes.AsReadOnly();

    public ProgressBarThemeService(IConfigurationManager configManager)
    {
        _configManager = configManager;
    }

    public void LoadAllThemes()
    {
        _availableThemes.Clear();

        LoadBuiltInThemes();
        LoadThirdPartyThemes();

        if (_availableThemes.Count == 0)
        {
            LoadFallbackDefaultTheme();
        }
    }

    public void ApplyTheme(string themeId)
    {
        var theme = _availableThemes.FirstOrDefault(t => t.Id == themeId);
        if (theme == null)
        {
            theme = _availableThemes.FirstOrDefault(t => t.Id == "default")
                   ?? CreateDefaultManifest();
        }

        try
        {
            var app = Application.Current;
            if (app == null) return;

            var merged = app.Resources.MergedDictionaries;

            RemoveCurrentThemeResource(merged);

            if (theme.IsBuiltIn)
            {
                ApplyBuiltInTheme(theme, merged);
            }
            else
            {
                ApplyThirdPartyTheme(theme, merged);
            }

            _currentTheme = theme;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"应用主题失败: {themeId}, {ex.Message}");
            _currentTheme = CreateDefaultManifest();
        }
    }

    private void LoadBuiltInThemes()
    {
        _availableThemes.Add(CreateDefaultManifest());
    }

    private void LoadThirdPartyThemes()
    {
        try
        {
            var themesDir = _configManager.ProgressBarThemesDirectory;

            if (!Directory.Exists(themesDir))
                return;

            foreach (var themeDir in Directory.EnumerateDirectories(themesDir))
            {
                var manifestPath = Path.Combine(themeDir, "manifest.json");
                if (!File.Exists(manifestPath)) continue;

                try
                {
                    var json = File.ReadAllText(manifestPath);
                    var manifest = JsonSerializer.Deserialize<ProgressBarThemeManifest>(json, JsonOptions);
                    if (manifest != null)
                    {
                        manifest.IsBuiltIn = false;
                        _availableThemes.Add(manifest);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"加载第三方主题清单失败: {themeDir}, {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"扫描第三方主题目录失败: {ex.Message}");
        }
    }

    private static void RemoveCurrentThemeResource(IList<ResourceDictionary> merged)
    {
        var oldTheme = merged.FirstOrDefault(d =>
            d.Source != null &&
            d.Source.OriginalString.Contains("ProgressBarThemes"));

        if (oldTheme != null)
        {
            merged.Remove(oldTheme);
        }
    }

    private static void ApplyBuiltInTheme(ProgressBarThemeManifest theme, IList<ResourceDictionary> merged)
    {
        if (theme.Id == "default")
            return;

        var packUri = $"pack://application:,,,/Themes/ProgressBarThemes/{theme.Id}.xaml";
        try
        {
            var resourceDict = new ResourceDictionary { Source = new Uri(packUri) };
            merged.Add(resourceDict);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载内置主题资源失败: {packUri}, {ex.Message}");
        }
    }

    private void ApplyThirdPartyTheme(ProgressBarThemeManifest theme, IList<ResourceDictionary> merged)
    {
        var themeDir = Path.Combine(_configManager.ProgressBarThemesDirectory, theme.Id);
        var themeXamlPath = Path.Combine(themeDir, "Theme.xaml");

        if (!File.Exists(themeXamlPath))
            return;

        try
        {
            var xamlContent = File.ReadAllText(themeXamlPath);
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xamlContent));
            var resourceDict = (ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream);
            merged.Add(resourceDict);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载第三方主题资源失败: {themeXamlPath}, {ex.Message}");
        }
    }

    private void LoadFallbackDefaultTheme()
    {
        var defaultManifest = CreateDefaultManifest();
        _availableThemes.Add(defaultManifest);
        _currentTheme = defaultManifest;
    }

    private static ProgressBarThemeManifest CreateDefaultManifest()
    {
        return new ProgressBarThemeManifest
        {
            Id = "default",
            Name = "默认",
            Author = "ReTime - Testing",
            Version = "1.0.0",
            Description = "默认进度条主题，采用组件库原生样式，提供简洁标准的进度条外观",
            IsBuiltIn = true
        };
    }
}