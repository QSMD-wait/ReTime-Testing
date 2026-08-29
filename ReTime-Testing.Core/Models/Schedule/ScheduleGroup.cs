using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 计划表组配置（对齐 ClassIsland 的 ClassPlanGroup）
    /// 组仅作为归类容器，不持有轮换配置
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
        /// 默认组ID常量
        /// 参考 ClassIsland 的 ClassPlanGroup.DefaultGroupGuid
        /// </summary>
        public const string DefaultGroupId = "default";
    }
}
