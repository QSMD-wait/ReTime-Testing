using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// TimeTop 桌面组件主配置
    /// </summary>
    public class TimeTopSetting
    {
        /// <summary>
        /// 配置版本号
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 选中的时间计划ID
        /// </summary>
        [JsonPropertyName("selectedScheduleId")]
        public string SelectedScheduleId { get; set; } = "Default";

        /// <summary>
        /// 是否启用时间计划控制进度条
        /// </summary>
        [JsonPropertyName("enableTimeSchedule")]
        public bool EnableTimeSchedule { get; set; } = true;

        /// <summary>
        /// 时间设置
        /// </summary>
        [JsonPropertyName("timeSettings")]
        public TimeSettingsData TimeSettings { get; set; } = new();

        /// <summary>
        /// 状态样式配置
        /// </summary>
        [JsonPropertyName("stateStyles")]
        public Dictionary<string, StyleConfigData> StateStyles { get; set; } = new();
    }

    /// <summary>
    /// 时间设置数据
    /// </summary>
    public class TimeSettingsData
    {
        /// <summary>
        /// 校准设置
        /// </summary>
        [JsonPropertyName("calibration")]
        public CalibrationSettings Calibration { get; set; } = new();

        /// <summary>
        /// 失败策略
        /// </summary>
        [JsonPropertyName("fallback")]
        public FallbackSettings Fallback { get; set; } = new();

        /// <summary>
        /// 阈值设置
        /// </summary>
        [JsonPropertyName("threshold")]
        public ThresholdSettings Threshold { get; set; } = new();
    }

    /// <summary>
    /// 校准设置
    /// </summary>
    public class CalibrationSettings
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
        /// 超时时间（秒）
        /// </summary>
        [JsonPropertyName("timeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 3;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        [JsonPropertyName("maxRetryCount")]
        public int MaxRetryCount { get; set; } = 5;

        /// <summary>
        /// 退避乘数
        /// </summary>
        [JsonPropertyName("backoffMultiplier")]
        public double BackoffMultiplier { get; set; } = 2.0;
    }

    /// <summary>
    /// 失败策略
    /// </summary>
    public class FallbackSettings
    {
        /// <summary>
        /// 启动失败策略
        /// </summary>
        [JsonPropertyName("onStartFailure")]
        public string OnStartFailure { get; set; } = "systemTime";

        /// <summary>
        /// 运行时失败策略
        /// </summary>
        [JsonPropertyName("onRuntimeFailure")]
        public string OnRuntimeFailure { get; set; } = "keepCurrent";
    }

    /// <summary>
    /// 阈值设置
    /// </summary>
    public class ThresholdSettings
    {
        /// <summary>
        /// 触发校准的偏差阈值（秒）
        /// </summary>
        [JsonPropertyName("calibrationTriggerSeconds")]
        public int CalibrationTriggerSeconds { get; set; } = 5;

        /// <summary>
        /// 警告阈值（秒）
        /// </summary>
        [JsonPropertyName("warningThresholdSeconds")]
        public int WarningThresholdSeconds { get; set; } = 60;

        /// <summary>
        /// 休眠阈值（分钟）
        /// </summary>
        [JsonPropertyName("sleepThresholdMinutes")]
        public int SleepThresholdMinutes { get; set; } = 5;
    }

    /// <summary>
    /// 样式配置数据
    /// </summary>
    public class StyleConfigData
    {
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
