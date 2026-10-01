using System;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 计划表简化信息（用于列表显示）
    /// 天→表映射、轮转、组归属已移至 ScheduleGroup 层
    /// </summary>
    public class ScheduleInfo
    {
        /// <summary>
        /// 计划表ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 计划表名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 计划表描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
