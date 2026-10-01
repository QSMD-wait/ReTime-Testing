using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 时间计划元数据
    /// 天→表映射、轮转、组归属已移至 ScheduleGroup 层
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
        /// 是否启用（主开关）
        /// </summary>
        [JsonPropertyName("isEnabled")]
        public bool IsEnabled { get; set; } = true;
    }
}
