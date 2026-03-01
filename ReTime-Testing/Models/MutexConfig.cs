using System;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 互斥锁配置
    /// </summary>
    public class MutexConfig
    {
        /// <summary>
        /// 互斥锁唯一标识符（建议使用应用程序名称或GUID）
        /// </summary>
        public string MutexId { get; set; } = string.Empty;

        /// <summary>
        /// 冲突弹窗自动关闭时间（毫秒），0 表示不自动关闭
        /// </summary>
        public int ConflictWindowAutoCloseTime { get; set; }

        /// <summary>
        /// 冲突弹窗标题
        /// </summary>
        public string ConflictWindowTitle { get; set; } = string.Empty;

        /// <summary>
        /// 冲突弹窗消息
        /// </summary>
        public string ConflictWindowMessage { get; set; } = string.Empty;

        /// <summary>
        /// 是否启用互斥锁
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 冲突时是否自动关闭应用程序
        /// </summary>
        public bool AutoShutdownOnConflict { get; set; }

        /// <summary>
        /// 是否播放提示音
        /// </summary>
        public bool PlaySound { get; set; }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        public static MutexConfig GetDefault()
        {
            return new MutexConfig
            {
                MutexId = "ReTime-Testing-Application-Mutex",
                ConflictWindowAutoCloseTime = 0, // 不自动关闭
                ConflictWindowTitle = "程序已在运行",
                ConflictWindowMessage = "检测到该程序已在运行中，请关闭此窗口。",
                IsEnabled = true,
                AutoShutdownOnConflict = true, // 显示弹窗后自动关闭应用
                PlaySound = true
            };
        }
    }
}