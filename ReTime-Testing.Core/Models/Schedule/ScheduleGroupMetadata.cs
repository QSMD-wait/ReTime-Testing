using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 计划表组元数据
    /// </summary>
    public class ScheduleGroupMetadata
    {
        /// <summary>
        /// 组名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 组描述
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
    }
}