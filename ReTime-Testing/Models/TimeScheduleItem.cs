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
    }
}
