using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 时间计划
    /// </summary>
    public class TimeSchedule
    {
        /// <summary>
        /// 时间表唯一标识
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 配置版本号
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 设置项大区
        /// </summary>
        [JsonPropertyName("settings")]
        public TimeScheduleSettings Settings { get; set; } = new();

        /// <summary>
        /// 时间计划大区
        /// </summary>
        [JsonPropertyName("schedules")]
        public List<TimeScheduleItem> Schedules { get; set; } = new();

        /// <summary>
        /// 自定义时间点列表（可选）
        /// 用于覆盖自动生成的开始/结束时间点，或添加额外的时间点
        /// </summary>
        [JsonPropertyName("timePoints")]
        public List<CustomTimePoint> TimePoints { get; set; } = new();
    }

    /// <summary>
    /// 时间计划设置项
    /// </summary>
    public class TimeScheduleSettings
    {
        /// <summary>
        /// 元数据子区
        /// </summary>
        [JsonPropertyName("metadata")]
        public TimeScheduleMetadata Metadata { get; set; } = new();
    }
}
