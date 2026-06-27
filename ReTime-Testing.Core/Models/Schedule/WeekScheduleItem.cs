using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 星期-计划表映射项
    /// weekDay 值与 System.DayOfWeek 枚举一致：0=Sunday, 1=Monday, ..., 6=Saturday
    /// </summary>
    public class WeekScheduleItem
    {
        /// <summary>
        /// 星期几（0=Sunday, 1=Monday, ..., 6=Saturday）
        /// </summary>
        [JsonPropertyName("weekDay")]
        public int WeekDay { get; set; }

        /// <summary>
        /// 对应的时间计划表ID，null 表示该天没有计划表
        /// </summary>
        [JsonPropertyName("scheduleId")]
        public string? ScheduleId { get; set; }
    }
}