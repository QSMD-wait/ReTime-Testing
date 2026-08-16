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
/// 文字插槽组件专属配置（不同 Source 类型读取不同字段）
/// </summary>
public class TextSlotSourceSettings
{
    /// <summary>
    /// 自定义文本内容（仅 Source=CustomText 时生效）
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// 格式字符串（Source=CurrentTime/CurrentDate/CurrentDayOfWeek 时生效）
    /// CurrentTime 默认 "HH:mm:ss"，CurrentDate 默认 "yyyy/MM/dd"，CurrentDayOfWeek 默认 "long"
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }

    /// <summary>
    /// 是否显示秒（Source=RemainingTime/ElapsedTime 时生效，默认 true）
    /// </summary>
    [JsonPropertyName("showSeconds")]
    public bool? ShowSeconds { get; set; }

    /// <summary>
    /// 小数位数（Source=ProgressPercent 时生效，默认 1）
    /// </summary>
    [JsonPropertyName("decimalPlaces")]
    public int? DecimalPlaces { get; set; }

    /// <summary>
    /// 无数据时的回退文本（Source=SegmentName/NextSegment 时生效）
    /// </summary>
    [JsonPropertyName("fallback")]
    public string? Fallback { get; set; }

    /// <summary>
    /// 是否同时显示开始时间（Source=NextSegment 时生效，默认 false）
    /// </summary>
    [JsonPropertyName("showTime")]
    public bool? ShowTime { get; set; }
}

/// <summary>
/// 文字插槽通用自定义项（所有 Source 类型共享）
/// </summary>
public class TextSlotCommonSettings
{
    /// <summary>
    /// 该项是否可见（可临时隐藏而不删除）
    /// </summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    /// <summary>
    /// 前缀文本（显示在内容之前，如 "⏱"）
    /// </summary>
    [JsonPropertyName("prefix")]
    public string? Prefix { get; set; }

    /// <summary>
    /// 后缀文本（显示在内容之后，如 "%"）
    /// </summary>
    [JsonPropertyName("suffix")]
    public string? Suffix { get; set; }

    /// <summary>
    /// 单项字体覆盖（null 表示使用全局字体，值为字体系列名称如 "Microsoft YaHei"）
    /// </summary>
    [JsonPropertyName("fontFamily")]
    public string? FontFamily { get; set; }

    /// <summary>
    /// 单项字体大小覆盖（null 表示使用全局字体大小）
    /// </summary>
    [JsonPropertyName("fontSizeOverride")]
    public double? FontSizeOverride { get; set; }

    /// <summary>
    /// 单项颜色覆盖（null 表示使用全局文字颜色，ARGB 十六进制如 "#FF0000"）
    /// </summary>
    [JsonPropertyName("colorOverride")]
    public string? ColorOverride { get; set; }
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
    /// 组件专属配置（不同 Source 类型读取不同字段）
    /// </summary>
    [JsonPropertyName("sourceSettings")]
    public TextSlotSourceSettings SourceSettings { get; set; } = new();

    /// <summary>
    /// 通用自定义项（所有 Source 类型共享）
    /// </summary>
    [JsonPropertyName("commonSettings")]
    public TextSlotCommonSettings CommonSettings { get; set; } = new();
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
    /// 默认左侧组件：日期、星期、时间
    /// </summary>
    private static readonly TextSourceType[] DefaultLeftSources =
        [TextSourceType.CurrentDate, TextSourceType.CurrentDayOfWeek, TextSourceType.CurrentTime];

    /// <summary>
    /// 默认右侧组件：当前时间段、剩余时间、进度百分比
    /// </summary>
    private static readonly TextSourceType[] DefaultRightSources =
        [TextSourceType.SegmentName, TextSourceType.RemainingTime, TextSourceType.ProgressPercent];

    public TextOverlayLayoutConfig()
    {
        Left = CreateGroup(DefaultLeftSources);
        Center = new TextOverlayGroupConfig();
        Right = CreateGroup(DefaultRightSources);
    }

    /// <summary>
    /// 按数据源列表创建带默认格式的插槽组
    /// </summary>
    private static TextOverlayGroupConfig CreateGroup(TextSourceType[] sources)
    {
        var group = new TextOverlayGroupConfig();
        foreach (var source in sources)
        {
            group.Slots.Add(new TextSlotConfig
            {
                Source = source,
                SourceSettings = new TextSlotSourceSettings
                {
                    Format = source switch
                    {
                        TextSourceType.CurrentTime => "HH:mm:ss",
                        TextSourceType.CurrentDate => "yyyy/MM/dd",
                        TextSourceType.CurrentDayOfWeek => "星期X",
                        _ => null
                    }
                }
            });
        }
        return group;
    }

    /// <summary>
    /// 左侧组（从左向右排列，优先级最高）
    /// </summary>
    [JsonPropertyName("left")]
    public TextOverlayGroupConfig Left { get; set; }

    /// <summary>
    /// 中间组（居中排列，优先级最低）
    /// </summary>
    [JsonPropertyName("center")]
    public TextOverlayGroupConfig Center { get; set; }

    /// <summary>
    /// 右侧组（从右向左排列，优先级居中）
    /// </summary>
    [JsonPropertyName("right")]
    public TextOverlayGroupConfig Right { get; set; }
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
    /// 文字颜色（ARGB 十六进制，如 "#E0E0E0"），默认偏灰白色
    /// </summary>
    [JsonPropertyName("textColor")]
    public string? TextColor { get; set; } = "#E0E0E0";

    /// <summary>
    /// 组件之间的间隔（像素）
    /// </summary>
    [JsonPropertyName("itemSpacing")]
    public double ItemSpacing { get; set; } = 8;

    /// <summary>
    /// 左区域水平偏移（正→右移，负→左移），默认 80（基础边距16px内置）
    /// </summary>
    [JsonPropertyName("leftOffset")]
    public double LeftOffset { get; set; } = 80;

    /// <summary>
    /// 中区域水平偏移（正→右移，负→左移），默认 0
    /// </summary>
    [JsonPropertyName("centerOffset")]
    public double CenterOffset { get; set; } = 0;

    /// <summary>
    /// 右区域水平偏移（正→右移，负→左移），默认 -80（基础边距16px内置）
    /// </summary>
    [JsonPropertyName("rightOffset")]
    public double RightOffset { get; set; } = -80;

    /// <summary>
    /// 创建浅拷贝，确保 WPF DependencyProperty 检测到引用变更
    /// </summary>
    public TextOverlayStyleConfig Clone() => new()
    {
        FontSize = FontSize,
        Opacity = Opacity,
        TextColor = TextColor,
        ItemSpacing = ItemSpacing,
        LeftOffset = LeftOffset,
        CenterOffset = CenterOffset,
        RightOffset = RightOffset,
        VerticalOffset = VerticalOffset,
        TextEffect = TextEffect,
    };

    /// <summary>
    /// 整体垂直偏移（正→上移，负→下移），不会遮挡进度条
    /// </summary>
    [JsonPropertyName("verticalOffset")]
    public double VerticalOffset { get; set; } = 0;

    /// <summary>
    /// 文字效果类型：none=无效果，shadow=阴影，outline=描边
    /// </summary>
    [JsonPropertyName("textEffect")]
    public string TextEffect { get; set; } = "shadow";
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
    public bool Enabled { get; set; } = true;

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