using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.Logging;
using ReTime_Testing.Core.Models.Theme;
using ReTime_Testing.Core.Services;

namespace ReTime_Testing.Services;

public class ProgressBarThemeService : IProgressBarThemeService
{
        private readonly ILogger<ProgressBarThemeService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly IConfigurationManager _configManager;
    private readonly IApplicationResourceProvider _resourceProvider;
    private readonly List<ProgressBarThemeManifest> _availableThemes = new();
    private ProgressBarThemeManifest _currentTheme = new();

    public string CurrentThemeId => _currentTheme.Id;

    public ProgressBarThemeManifest CurrentTheme => _currentTheme;

    public IReadOnlyList<ProgressBarThemeManifest> AvailableThemes => _availableThemes.AsReadOnly();

    public event Action<string>? ThemeChanged;

    public ProgressBarThemeService(IConfigurationManager configManager, IApplicationResourceProvider resourceProvider, ILogger<ProgressBarThemeService> logger)
    {
        _logger = logger;
        _configManager = configManager;
        _resourceProvider = resourceProvider;
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
            theme = _availableThemes.FirstOrDefault(t => t.Id == ProgressBarThemeManifest.DefaultId)
                   ?? CreateDefaultManifest();
        }

        try
        {
            var merged = _resourceProvider.GetMergedDictionaries();

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
            ThemeChanged?.Invoke(theme.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用主题失败: {ThemeId}", themeId);
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
                    _logger.LogWarning(ex, "加载第三方主题清单失败: {ThemeDir}, {Message}", themeDir, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "扫描第三方主题目录失败: {Message}", ex.Message);
        }
    }

    private const string ThemeMarkerKey = "__ReTime_ProgressBarTheme__";

    private static void RemoveCurrentThemeResource(IList<ResourceDictionary> merged)
    {
        var oldTheme = merged.FirstOrDefault(d => d.Contains(ThemeMarkerKey));

        if (oldTheme != null)
        {
            merged.Remove(oldTheme);
        }
    }

    private void ApplyBuiltInTheme(ProgressBarThemeManifest theme, IList<ResourceDictionary> merged)
    {
        if (theme.Id == ProgressBarThemeManifest.DefaultId)
            return;

        var packUri = $"pack://application:,,,/Themes/ProgressBarThemes/{theme.Id}.xaml";
        try
        {
            var resourceDict = new ResourceDictionary { Source = new Uri(packUri) };
            resourceDict[ThemeMarkerKey] = theme.Id;
            merged.Add(resourceDict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载内置主题资源失败: {PackUri}, {Message}", packUri, ex.Message);
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
            resourceDict[ThemeMarkerKey] = theme.Id;
            merged.Add(resourceDict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载第三方主题资源失败: {ThemeXamlPath}, {Message}", themeXamlPath, ex.Message);
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
            Id = ProgressBarThemeManifest.DefaultId,
            Name = "默认",
            Author = "ReTime - Testing",
            Version = "1.0.0",
            Description = "默认进度条主题，采用组件库原生样式，提供简洁标准的进度条外观",
            IsBuiltIn = true
        };
    }
}