using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// TimeTop 桌面组件主配置
    /// 八域结构：schedule / progressBar / behavior / calibration / stateStyles / defaultBehavior / textOverlay / window
    /// </summary>
    public class TimeTopSetting
    {
        /// <summary>
        /// 配置版本号
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 时间计划表配置
        /// </summary>
        [JsonPropertyName("schedule")]
        public ScheduleConfig Schedule { get; set; } = new();

        /// <summary>
        /// 进度条外观配置
        /// </summary>
        [JsonPropertyName("progressBar")]
        public ProgressBarConfig ProgressBar { get; set; } = new();

        /// <summary>
        /// 进度条行为配置
        /// </summary>
        [JsonPropertyName("behavior")]
        public ProgressBarBehaviorConfig Behavior { get; set; } = new();

        /// <summary>
        /// 时间校准配置
        /// </summary>
        [JsonPropertyName("calibration")]
        public CalibrationConfig Calibration { get; set; } = new();

        /// <summary>
        /// 默认样式配置（各状态样式覆盖）
        /// </summary>
        [JsonPropertyName("stateStyles")]
        public StateStylesConfig StateStyles { get; set; } = new();

        /// <summary>
        /// 默认行为配置（时间段行为的三级优先级中间层）
        /// 时间段未指定行为时回退到此配置
        /// </summary>
        [JsonPropertyName("defaultBehavior")]
        public ScheduleBehaviorData DefaultBehavior { get; set; } = new();

        /// <summary>
        /// 文字覆盖配置
        /// </summary>
        [JsonPropertyName("textOverlay")]
        public TextOverlayConfig TextOverlay { get; set; } = new();

        /// <summary>
        /// 窗口配置
        /// </summary>
        [JsonPropertyName("window")]
        public WindowConfig Window { get; set; } = new();
    }

    /// <summary>
    /// 时间计划表配置
    /// </summary>
    public class ScheduleConfig
    {
        /// <summary>
        /// 是否启用时间计划控制进度条
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 当前激活的计划表组ID，null 表示未启用组轮换
        /// </summary>
        [JsonPropertyName("activeGroupId")]
        public string? ActiveGroupId { get; set; }

        /// <summary>
        /// 手动覆盖配置
        /// </summary>
        [JsonPropertyName("override")]
        public ScheduleOverrideConfig Override { get; set; } = new();
    }

    /// <summary>
    /// 计划表手动覆盖配置
    /// 启用后将忽略组轮换，直接使用指定的计划表
    /// </summary>
    public class ScheduleOverrideConfig
    {
        /// <summary>
        /// 是否启用手动覆盖（覆盖组轮换）
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 手动指定的计划表ID
        /// override.enabled=true 时生效；无组轮换时作为默认计划表
        /// </summary>
        [JsonPropertyName("scheduleId")]
        public string ScheduleId { get; set; } = "Default";
    }

    /// <summary>
    /// 进度条外观配置
    /// </summary>
    public class ProgressBarConfig
    {
        /// <summary>
        /// 进度条位置：top / bottom / left / right
        /// </summary>
        [JsonPropertyName("position")]
        public string Position { get; set; } = "top";

        /// <summary>
        /// 进度条整体缩放比例（0.5~3.0，1.0为原始大小）
        /// </summary>
        [JsonPropertyName("scale")]
        public double Scale { get; set; } = 1.0;

        /// <summary>
        /// 圆角半径（px）
        /// </summary>
        [JsonPropertyName("cornerRadius")]
        public int CornerRadius { get; set; } = 0;

        /// <summary>
        /// 是否启用发光效果
        /// </summary>
        [JsonPropertyName("glowEnabled")]
        public bool GlowEnabled { get; set; } = true;

        /// <summary>
        /// 发光颜色（null 跟随前景色）
        /// </summary>
        [JsonPropertyName("glowColor")]
        public string? GlowColor { get; set; }

        /// <summary>
        /// 是否启用阴影效果
        /// </summary>
        [JsonPropertyName("enableShadow")]
        public bool EnableShadow { get; set; } = true;
    }

    /// <summary>
    /// 进度条行为配置
    /// </summary>
    public class ProgressBarBehaviorConfig
    {
        /// <summary>
        /// 无活跃段时自动隐藏
        /// </summary>
        [JsonPropertyName("autoHide")]
        public bool AutoHide { get; set; } = false;

        /// <summary>
        /// 空闲时透明度（0.0–1.0）
        /// </summary>
        [JsonPropertyName("idleOpacity")]
        public double IdleOpacity { get; set; } = 0.3;
    }

    /// <summary>
    /// 校准源类型
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CalibrationSource
    {
        /// <summary>
        /// 系统时间校准
        /// </summary>
        System,

        /// <summary>
        /// 云端NTP校准
        /// </summary>
        Cloud
    }

    /// <summary>
    /// 时间校准配置
    /// </summary>
    public class CalibrationConfig
    {
        /// <summary>
        /// 是否启用时间校准
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 校准源类型：System = 系统时间, Cloud = 云端NTP
        /// </summary>
        [JsonPropertyName("source")]
        public CalibrationSource Source { get; set; } = CalibrationSource.System;

        /// <summary>
        /// 校准间隔（秒）
        /// </summary>
        [JsonPropertyName("intervalSeconds")]
        public int IntervalSeconds { get; set; } = 300;

        /// <summary>
        /// 触发校准的偏差阈值（秒）
        /// </summary>
        [JsonPropertyName("triggerSeconds")]
        public int TriggerSeconds { get; set; } = 5;

        /// <summary>
        /// 微调/跳跃分界阈值（秒）：偏差小于等于此值时微调，大于此值时跳跃
        /// </summary>
        [JsonPropertyName("minorThresholdSeconds")]
        public int MinorThresholdSeconds { get; set; } = 30;

        /// <summary>
        /// 休眠恢复校准阈值（秒）：系统休眠超过此时间后触发重新校准
        /// </summary>
        [JsonPropertyName("resumeThresholdSeconds")]
        public int ResumeThresholdSeconds { get; set; } = 300;

        /// <summary>
        /// 最大重试次数（所有校准源通用）
        /// </summary>
        [JsonPropertyName("maxRetryCount")]
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 退避乘数（所有校准源通用）
        /// </summary>
        [JsonPropertyName("backoffMultiplier")]
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// 用户时间偏移量（秒），正=向前（快）、负=向后（慢）
        /// </summary>
        [JsonPropertyName("userOffsetSeconds")]
        public double UserOffsetSeconds { get; set; } = 0;

        /// <summary>
        /// 云端校准专用配置（Source=Cloud时生效）
        /// </summary>
        [JsonPropertyName("cloud")]
        public CloudCalibrationConfig Cloud { get; set; } = new();
    }

    /// <summary>
    /// 云端校准专用配置
    /// </summary>
    public class CloudCalibrationConfig
    {
        /// <summary>
        /// 选中的NTP服务器地址
        /// </summary>
        [JsonPropertyName("selectedServerAddress")]
        public string SelectedServerAddress { get; set; } = NtpServerDefaults.DefaultServerAddress;

        /// <summary>
        /// NTP请求超时（秒）
        /// </summary>
        [JsonPropertyName("timeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 3;
    }

    /// <summary>
    /// NTP服务器默认配置（单一来源，消除硬编码重复）
    /// </summary>
    public static class NtpServerDefaults
    {
        /// <summary>
        /// 默认NTP服务器地址
        /// </summary>
        public const string DefaultServerAddress = "ntp.aliyun.com";

        /// <summary>
        /// 预定义NTP服务器地址列表
        /// </summary>
        public static readonly IReadOnlyList<string> Servers = new List<string>
        {
            "ntp.aliyun.com",
            "ntp.ntsc.ac.cn",
            "time.windows.com"
        }.AsReadOnly();

        /// <summary>
        /// 根据服务器地址查找索引，未找到返回0
        /// </summary>
        public static int IndexOf(string address)
        {
            for (int i = 0; i < Servers.Count; i++)
            {
                if (Servers[i] == address) return i;
            }
            return 0;
        }
    }

    /// <summary>
    /// 默认样式配置（含总开关和各状态样式）
    /// </summary>
    public class StateStylesConfig
    {
        /// <summary>
        /// 是否启用配置文件样式覆盖（总开关）
        /// 关闭则全部使用硬编码默认样式
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 各状态样式覆盖
        /// Key: 状态名（Loading/Progress/Success/Error/Paused/Hidden/Disabled）
        /// Value: 该状态的样式配置
        /// </summary>
        [JsonPropertyName("styles")]
        public Dictionary<string, StateStyleEntry> Styles { get; set; } = new()
        {
            ["Loading"] = new(),
            ["Progress"] = new(),
            ["Success"] = new(),
            ["Error"] = new(),
            ["Paused"] = new(),
            ["Hidden"] = new(),
            ["Disabled"] = new()
        };
    }

    /// <summary>
    /// 单个状态的样式配置（含开关）
    /// </summary>
    public class StateStyleEntry
    {
        /// <summary>
        /// 是否启用此状态的样式覆盖
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 前景色
        /// </summary>
        [JsonPropertyName("foregroundColor")]
        public string? ForegroundColor { get; set; }

        /// <summary>
        /// 背景色
        /// </summary>
        [JsonPropertyName("backgroundColor")]
        public string? BackgroundColor { get; set; }

        /// <summary>
        /// 透明度
        /// </summary>
        [JsonPropertyName("opacity")]
        public double? Opacity { get; set; }
        
        /// <summary>
        /// 是否启用阴影效果
        /// </summary>
        [JsonPropertyName("enableShadow")]
        public bool? EnableShadow { get; set; } = null;
    }

    /// <summary>
    /// 窗口层级维持模式
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TopmostMode
    {
        /// <summary>
        /// 仅初始化置顶，之后不维护（可能被其他窗口覆盖）
        /// </summary>
        None,

        /// <summary>
        /// 窗口失活时重新置顶
        /// </summary>
        OnDeactivated,

        /// <summary>
        /// 定时轮询置顶（500ms 间隔）
        /// </summary>
        Polling
    }

    /// <summary>
    /// 窗口配置（第8域）
    /// </summary>
    public class WindowConfig
    {
        [JsonPropertyName("topmostMode")]
        public TopmostMode TopmostMode { get; set; } = TopmostMode.OnDeactivated;

        [JsonPropertyName("useFullScreen")]
        public bool UseFullScreen { get; set; } = false;
    }
}