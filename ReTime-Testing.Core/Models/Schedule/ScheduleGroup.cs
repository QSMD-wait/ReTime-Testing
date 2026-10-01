using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 计划表组配置（时间表集合）
    /// 组持有天→表映射、轮转配置和日期覆盖
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
        /// 基础天→表映射（每周都生效）
        /// Key: 星期几（"0"=周日, "1"=周一, ..., "6"=周六）
        /// Value: 计划表ID
        /// </summary>
        [JsonPropertyName("dayScheduleMap")]
        public Dictionary<string, string> DayScheduleMap { get; set; } = new();

        /// <summary>
        /// 轮转周期数（1=不轮换, 2=双周, 3=三周...）
        /// </summary>
        [JsonPropertyName("rotationCycleCount")]
        public int RotationCycleCount { get; set; } = 1;

        /// <summary>
        /// 轮转起始日期（ISO 8601 日期字符串，如 "2026-01-05"）
        /// 作为多周轮换的计算起点，null 时默认为本周周一
        /// </summary>
        [JsonPropertyName("rotationStartDate")]
        public string? RotationStartDate { get; set; }

        /// <summary>
        /// 轮转偏移量（用于微调轮转周计算）
        /// </summary>
        [JsonPropertyName("rotationOffset")]
        public int RotationOffset { get; set; } = 0;

        /// <summary>
        /// 轮转周覆盖映射（只写差异）
        /// Key: 轮转周索引（"1" ~ "N"，0 表示基础周，无覆盖）
        /// Value: 该轮转周内变化的天→表映射
        /// </summary>
        [JsonPropertyName("rotatedDayScheduleMaps")]
        public Dictionary<string, Dictionary<string, string>> RotatedDayScheduleMaps { get; set; } = new();

        /// <summary>
        /// 日期级别覆盖（最高优先级）
        /// Key: 日期（"yyyy-MM-dd"）
        /// Value: 计划表ID
        /// </summary>
        [JsonPropertyName("dateOverrides")]
        public Dictionary<string, string> DateOverrides { get; set; } = new();

        /// <summary>
        /// 默认组ID常量
        /// </summary>
        public const string DefaultGroupId = "default";
    }
}
