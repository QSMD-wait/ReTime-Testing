using System;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 互斥锁冲突事件参数
    /// </summary>
    public class MutexConflictEventArgs : EventArgs
    {
        /// <summary>
        /// 互斥锁唯一标识符
        /// </summary>
        public string MutexId { get; set; } = string.Empty;

        /// <summary>
        /// 自动关闭时间（毫秒）
        /// </summary>
        public int AutoCloseTime { get; set; }

        /// <summary>
        /// 冲突发生时间
        /// </summary>
        public DateTime ConflictTime { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="mutexId">互斥锁唯一标识符</param>
        /// <param name="autoCloseTime">自动关闭时间（毫秒）</param>
        public MutexConflictEventArgs(string mutexId, int autoCloseTime)
        {
            MutexId = mutexId ?? string.Empty;
            AutoCloseTime = autoCloseTime;
            ConflictTime = DateTime.Now;
        }
    }
}