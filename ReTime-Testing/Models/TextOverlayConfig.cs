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
    NextSegment,

    /// <summary>
    /// 当前日期
    /// </summary>
    CurrentDate,

    /// <summary>
    /// 当前星期几
    /// </summary>
    CurrentDayOfWeek
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
/// 文字覆盖组配置（Left / Center / Right 各一组）
/// </summary>
public class TextOverlayGroupConfig
{
    /// <summary>
    /// 插槽列表
    /// </summary>
    [JsonPropertyName("slots")]
    public List<TextSlotConfig> Slots { get; set; } = [];

    /// <summary>
    /// 该组是否可见
    /// </summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;
}

/// <summary>
/// 文字覆盖布局配置
/// </summary>
public class TextOverlayLayoutConfig
{
    /// <summary>
    /// 左侧组（从左向右排列，优先级最高）
    /// </summary>
    [JsonPropertyName("left")]
    public TextOverlayGroupConfig Left { get; set; } = new();

    /// <summary>
    /// 中间组（居中排列，优先级最低）
    /// </summary>
    [JsonPropertyName("center")]
    public TextOverlayGroupConfig Center { get; set; } = new();

    /// <summary>
    /// 右侧组（从右向左排列，优先级居中）
    /// </summary>
    [JsonPropertyName("right")]
    public TextOverlayGroupConfig Right { get; set; } = new();
}

/// <summary>
/// 文字覆盖样式配置
/// </summary>
public class TextOverlayStyleConfig
{
    /// <summary>
    /// 字体大小
    /// </summary>
    [JsonPropertyName("fontSize")]
    public double FontSize { get; set; } = 12;

    /// <summary>
    /// 文字透明度（0.0 ~ 1.0）
    /// </summary>
    [JsonPropertyName("opacity")]
    public double Opacity { get; set; } = 0.8;

    /// <summary>
    /// 组件之间的间隔（像素）
    /// </summary>
    [JsonPropertyName("itemSpacing")]
    public double ItemSpacing { get; set; } = 8;

    /// <summary>
    /// 左边距（像素）
    /// </summary>
    [JsonPropertyName("leftMargin")]
    public double LeftMargin { get; set; } = 16;

    /// <summary>
    /// 右边距（像素）
    /// </summary>
    [JsonPropertyName("rightMargin")]
    public double RightMargin { get; set; } = 16;
}

/// <summary>
/// 文字覆盖配置（TimeTopSetting 第7域）
/// </summary>
public class TextOverlayConfig
{
    /// <summary>
    /// 是否启用文字覆盖
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 布局配置
    /// </summary>
    [JsonPropertyName("layout")]
    public TextOverlayLayoutConfig Layout { get; set; } = new();

    /// <summary>
    /// 样式配置
    /// </summary>
    [JsonPropertyName("style")]
    public TextOverlayStyleConfig Style { get; set; } = new();
}
