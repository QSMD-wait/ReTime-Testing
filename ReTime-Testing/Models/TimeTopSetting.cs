using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// TimeTop 桌面组件主配置
    /// 六域结构：schedule / progressBar / behavior / calibration / stateStyles / defaultBehavior
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
        /// 云端校准配置
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
    }

    /// <summary>
    /// 时间计划表配置
    /// </summary>
    public class ScheduleConfig
    {
        /// <summary>
        /// 当前激活的时间计划表ID
        /// </summary>
        [JsonPropertyName("selectedId")]
        public string SelectedId { get; set; } = "Default";

        /// <summary>
        /// 是否启用时间计划控制进度条
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;
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
        /// 进度条高度（px）
        /// </summary>
        [JsonPropertyName("height")]
        public int Height { get; set; } = 5;

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
    /// 云端校准配置
    /// </summary>
    public class CalibrationConfig
    {
        /// <summary>
        /// 是否启用云端校准
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 校准间隔（秒）
        /// </summary>
        [JsonPropertyName("intervalSeconds")]
        public int IntervalSeconds { get; set; } = 300;

        /// <summary>
        /// 校准请求超时（秒）
        /// </summary>
        [JsonPropertyName("timeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 3;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        [JsonPropertyName("maxRetryCount")]
        public int MaxRetryCount { get; set; } = 5;

        /// <summary>
        /// 重试退避乘数
        /// </summary>
        [JsonPropertyName("backoffMultiplier")]
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// 失败策略
        /// </summary>
        [JsonPropertyName("fallback")]
        public CalibrationFallbackConfig Fallback { get; set; } = new();

        /// <summary>
        /// 阈值配置
        /// </summary>
        [JsonPropertyName("threshold")]
        public CalibrationThresholdConfig Threshold { get; set; } = new();
    }

    /// <summary>
    /// 校准失败策略配置
    /// </summary>
    public class CalibrationFallbackConfig
    {
        /// <summary>
        /// 启动时校准失败策略
        /// </summary>
        [JsonPropertyName("onStartFailure")]
        public string OnStartFailure { get; set; } = "systemTime";

        /// <summary>
        /// 运行时校准失败策略
        /// </summary>
        [JsonPropertyName("onRuntimeFailure")]
        public string OnRuntimeFailure { get; set; } = "keepCurrent";
    }

    /// <summary>
    /// 校准阈值配置
    /// </summary>
    public class CalibrationThresholdConfig
    {
        /// <summary>
        /// 触发校准的偏差阈值（秒）
        /// </summary>
        [JsonPropertyName("triggerSeconds")]
        public int TriggerSeconds { get; set; } = 5;

        /// <summary>
        /// 警告阈值（秒）
        /// </summary>
        [JsonPropertyName("warningSeconds")]
        public int WarningSeconds { get; set; } = 60;

        /// <summary>
        /// 休眠后重新校准阈值（分钟）
        /// </summary>
        [JsonPropertyName("sleepMinutes")]
        public int SleepMinutes { get; set; } = 5;
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
    }
}
