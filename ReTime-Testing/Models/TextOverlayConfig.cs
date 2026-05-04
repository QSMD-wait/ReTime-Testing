using System.Text.Json.Serialization;

namespace ReTime_Testing.Models;

/// <summary>
/// 文字数据源类型
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextSourceType
{
    /// <summary>
    /// 不显示
    /// </summary>
    None,

    /// <summary>
    /// 自定义文本
    /// </summary>
    CustomText,

    /// <summary>
    /// 当前时间段名称
    /// </summary>
    SegmentName,

    /// <summary>
    /// 剩余时间
    /// </summary>
    RemainingTime,

    /// <summary>
    /// 已过时间
    /// </summary>
    ElapsedTime,

    /// <summary>
    /// 进度百分比
    /// </summary>
    ProgressPercent,

    /// <summary>
    /// 当前系统时间
    /// </summary>
    CurrentTime,

    /// <summary>
    /// 下一时间段名称
    /// </summary>
    NextSegment
}

/// <summary>
/// 文字插槽配置
/// </summary>
public class TextSlotConfig
{
    /// <summary>
    /// 数据源类型
    /// </summary>
    [JsonPropertyName("source")]
    public TextSourceType Source { get; set; } = TextSourceType.None;

    /// <summary>
    /// 自定义文本（仅 Source=CustomText 时生效）
    /// </summary>
    [JsonPropertyName("customText")]
    public string CustomText { get; set; } = "";

    /// <summary>
    /// 项间分隔符
    /// </summary>
    [JsonPropertyName("separator")]
    public string Separator { get; set; } = "  ";
}

/// <summary>
/// 文字覆盖配置（TimeTopSetting 第7域）
/// 三组自由排列：Left / Center / Right
/// 溢出裁剪优先级：Center > Right > Left
/// </summary>
public class TextOverlayConfig
{
    /// <summary>
    /// 是否启用文字覆盖
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 左侧文字插槽列表（从左向右排列，优先级最高）
    /// </summary>
    [JsonPropertyName("left")]
    public List<TextSlotConfig> Left { get; set; } = [];

    /// <summary>
    /// 中间文字插槽列表（居中排列，优先级最低）
    /// </summary>
    [JsonPropertyName("center")]
    public List<TextSlotConfig> Center { get; set; } = [];

    /// <summary>
    /// 右侧文字插槽列表（从右向左排列，优先级居中）
    /// </summary>
    [JsonPropertyName("right")]
    public List<TextSlotConfig> Right { get; set; } = [];
}
