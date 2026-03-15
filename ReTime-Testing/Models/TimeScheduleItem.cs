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
        /// 样式覆盖配置
        /// </summary>
        [JsonPropertyName("styles")]
        public StyleOverridesData? Styles { get; set; }
    }

    /// <summary>
    /// 样式覆盖配置数据
    /// </summary>
    public class StyleOverridesData
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

        /// <summary>
        /// 可见性
        /// </summary>
        [JsonPropertyName("visibility")]
        public string? Visibility { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [JsonPropertyName("isEnabled")]
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// 是否不确定动画
        /// </summary>
        [JsonPropertyName("isIndeterminate")]
        public bool? IsIndeterminate { get; set; }
    }
}
