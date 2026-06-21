using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 时间段
    /// </summary>
    public class TimeScheduleItem
    {
        /// <summary>
        /// 时间段唯一标识
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 时间段名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 起始时间（HH:mm:ss 格式）
        /// </summary>
        [JsonPropertyName("startTime")]
        public string StartTime { get; set; } = "00:00:00";

        /// <summary>
        /// 结束时间（HH:mm:ss 格式）
        /// </summary>
        [JsonPropertyName("endTime")]
        public string EndTime { get; set; } = "00:00:00";

        /// <summary>
        /// 状态类型（已废弃，时间段固定为 Progress）
        /// 保留此字段仅用于向后兼容，运行时会被忽略
        /// </summary>
        [JsonIgnore]
        [Obsolete("时间段状态固定为 Progress，此字段已废弃")]
        public ProgressStateType State { get; set; } = ProgressStateType.Progress;

        /// <summary>
        /// 样式覆盖配置
        /// </summary>
        [JsonPropertyName("styles")]
        public StyleOverridesData? Styles { get; set; }

        /// <summary>
        /// 行为配置（可选）
        /// 控制时间段的调度和显示行为，如轮询间隔、倒计时模式等
        /// </summary>
        [JsonPropertyName("behavior")]
        public ScheduleBehaviorData? Behavior { get; set; }
    }

    /// <summary>
    /// 样式覆盖配置数据
    /// </summary>
    public class StyleOverridesData
    {
        /// <summary>
        /// 是否启用样式覆盖（null 或 false 表示使用默认样式）
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }

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
