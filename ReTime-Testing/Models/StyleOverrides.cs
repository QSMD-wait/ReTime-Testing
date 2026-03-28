using System.Windows.Media;

namespace ReTime_Testing.Models;

/// <summary>
/// 样式覆盖配置
/// 用于覆盖默认样式设置
/// </summary>
public class StyleOverrides
{
    /// <summary>
    /// 前景色（null 表示使用默认值）
    /// </summary>
    public Brush? ForegroundColor { get; set; }

    /// <summary>
    /// 背景色（null 表示使用默认值）
    /// </summary>
    public Brush? BackgroundColor { get; set; }

    /// <summary>
    /// 透明度（null 表示使用默认值，范围 0.0-1.0）
    /// </summary>
    public double? Opacity { get; set; }

    /// <summary>
    /// 创建空的样式覆盖（全部使用默认值）
    /// </summary>
    public static StyleOverrides None => new StyleOverrides();

    /// <summary>
    /// 是否有任何覆盖设置
    /// </summary>
    public bool HasAnyOverride =>
        ForegroundColor != null ||
        BackgroundColor != null ||
        Opacity != null;
}