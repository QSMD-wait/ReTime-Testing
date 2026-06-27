using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 计划表组配置
    /// 将多个计划表归为一组，支持按星期自动轮换
    /// </summary>
    public class ScheduleGroup
    {
        /// <summary>
        /// 组唯一标识
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 配置版本号
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 组元数据
        /// </summary>
        [JsonPropertyName("metadata")]
        public ScheduleGroupMetadata Metadata { get; set; } = new();

        /// <summary>
        /// 星期-计划表映射列表
        /// weekDay 值与 System.DayOfWeek 枚举一致：0=Sunday, 1=Monday, ..., 6=Saturday
        /// 未列出的星期表示该天没有计划表
        /// </summary>
        [JsonPropertyName("weekSchedule")]
        public List<WeekScheduleItem> WeekSchedule { get; set; } = new();
    }
}