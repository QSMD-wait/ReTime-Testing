using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 日志服务接口
    /// </summary>
    public interface ILogService
    {
        /// <summary>
        /// 记录跟踪日志
        /// </summary>
        void Trace(string module, string message);

        /// <summary>
        /// 记录调试日志
        /// </summary>
        void Debug(string module, string message);

        /// <summary>
        /// 记录信息日志
        /// </summary>
        void Info(string module, string message);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        void Warn(string module, string message);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        void Error(string module, string message);

        /// <summary>
        /// 记录错误日志（带异常）
        /// </summary>
        void Error(string module, string message, Exception exception);
    }
}
