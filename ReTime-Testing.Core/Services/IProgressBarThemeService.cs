using ReTime_Testing.Core.Models.Theme;

namespace ReTime_Testing.Core.Services;

public interface IProgressBarThemeService
{
    string CurrentThemeId { get; }

    ProgressBarThemeManifest CurrentTheme { get; }

    IReadOnlyList<ProgressBarThemeManifest> AvailableThemes { get; }

    event Action<string>? ThemeChanged;

    void ApplyTheme(string themeId);

    void LoadAllThemes();
}