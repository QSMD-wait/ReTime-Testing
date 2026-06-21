using System;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 计划表简化信息（用于列表显示）
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
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
