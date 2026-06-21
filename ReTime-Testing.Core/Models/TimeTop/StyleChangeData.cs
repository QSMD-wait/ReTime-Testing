using System.Text.Json.Serialization;

namespace ReTime_Testing.Models;

/// <summary>
/// 样式变更数据
/// </summary>
public class StyleChangeData
{
    /// <summary>
    /// 前景色（可选，格式如 #00FF00）
    /// </summary>
    public string? ForegroundColor { get; set; }

    /// <summary>
    /// 背景色（可选，格式如 #FF0000）
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// 透明度（可选，范围 0.0-1.0）
    /// </summary>
    public double? Opacity { get; set; }
}