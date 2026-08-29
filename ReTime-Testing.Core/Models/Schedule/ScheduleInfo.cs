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
        /// 计划表描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 所属计划表组ID
        /// </summary>
        public string AssociatedGroupId { get; set; } = "default";

        /// <summary>
        /// 是否自动启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 星期几（0=周日, 1=周一, ..., 6=周六）
        /// </summary>
        public int DayOfWeek { get; set; }

        /// <summary>
        /// 轮换周数（1=每周, 2=双周, ..., 4=四周）
        /// </summary>
        public int RotationCycleCount { get; set; } = 1;

        /// <summary>
        /// 轮换周索引（0=每周, 1~N=第N轮换周）
        /// </summary>
        public int RotationWeekIndex { get; set; } = 0;

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
