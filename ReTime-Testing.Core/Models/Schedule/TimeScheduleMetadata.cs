using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 时间计划元数据（对齐 ClassIsland 的 TimeRule + ClassPlan 基础属性）
    /// </summary>
    public class TimeScheduleMetadata
    {
        /// <summary>
        /// 时间表名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 时间表描述
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 创建时间（ISO 8601 格式）
        /// </summary>
        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

        /// <summary>
        /// 最后修改时间（ISO 8601 格式）
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

        /// <summary>
        /// 所属计划表组ID（必填，默认 "default"）
        /// 参考 ClassIsland 的 ClassPlan.AssociatedGroup
        /// </summary>
        [JsonPropertyName("associatedGroupId")]
        public string AssociatedGroupId { get; set; } = "default";

        /// <summary>
        /// 是否自动启用（主开关）
        /// 参考 ClassIsland 的 ClassPlan.IsEnabled
        /// </summary>
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 星期几（0=周日, 1=周一, ..., 6=周六）
        /// 参考 ClassIsland 的 TimeRule.WeekDay
        /// </summary>
        [JsonPropertyName("dayOfWeek")]
        public int DayOfWeek { get; set; } = (int)DateTime.Today.DayOfWeek;

        /// <summary>
        /// 轮换周数（1=不轮换/每周, 2=双周, 3=三周, 4=四周）
        /// 参考 ClassIsland 的 TimeRule.WeekCountDivTotal
        /// </summary>
        [JsonPropertyName("rotationCycleCount")]
        public int RotationCycleCount { get; set; } = 1;

        /// <summary>
        /// 轮换周索引（0=每周, 1=第1轮换周, ..., N=第N轮换周）
        /// 当 RotationCycleCount=1 时此字段无意义
        /// 参考 ClassIsland 的 TimeRule.WeekCountDiv
        /// </summary>
        [JsonPropertyName("rotationWeekIndex")]
        public int RotationWeekIndex { get; set; } = 0;
    }
}
