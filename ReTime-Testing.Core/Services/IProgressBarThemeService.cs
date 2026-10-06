using ReTime_Testing.Core.Models.Theme;

namespace ReTime_Testing.Core.Services;

/// <summary>
/// 进度条主题服务：管理进度条主题清单的加载、切换与当前主题状态
/// </summary>
public interface IProgressBarThemeService
{
    /// <summary>当前生效的主题 ID</summary>
    string CurrentThemeId { get; }

    /// <summary>当前生效的主题清单</summary>
    ProgressBarThemeManifest CurrentTheme { get; }

    /// <summary>全部可用主题清单</summary>
    IReadOnlyList<ProgressBarThemeManifest> AvailableThemes { get; }

    /// <summary>主题切换事件（参数为新主题 ID）</summary>
    event Action<string>? ThemeChanged;

    /// <summary>按主题 ID 应用主题（无效 ID 忽略）</summary>
    /// <param name="themeId">目标主题 ID</param>
    void ApplyTheme(string themeId);

    /// <summary>重新扫描主题目录并加载全部主题</summary>
    void LoadAllThemes();
}
