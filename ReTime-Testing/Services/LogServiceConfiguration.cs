
using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 日志服务运行时配置
    /// 由 LogConfig（JSON 配置模型）+ 运行时路径构建
    /// </summary>
    public class LogServiceConfiguration
    {
        /// <summary>
        /// 是否允许文件输出
        /// </summary>
        public bool EnableFileOutput { get; }

        /// <summary>
        /// 输出的最低等级
        /// </summary>
        public LogLevel MinimumLevel { get; }

        /// <summary>
        /// 日志文件目录路径
        /// </summary>
        public string LogDirectory { get; }

        /// <summary>
        /// 日志文件保留天数
        /// </summary>
        public int RetainedFileCountLimit { get; }

        /// <summary>
        /// 单个日志文件大小上限（字节）
        /// </summary>
        public long FileSizeLimitBytes { get; }

        /// <summary>
        /// 默认构造函数（使用默认值）
        /// </summary>
        public LogServiceConfiguration()
        {
            EnableFileOutput = true;
            MinimumLevel = LogLevel.INF;
            LogDirectory = "data/Logs";
            RetainedFileCountLimit = 30;
            FileSizeLimitBytes = 10 * 1024L * 1024L;
        }

        /// <summary>
        /// 从 LogConfig（JSON 配置模型）+ 日志目录路径构建运行时配置
        /// </summary>
        public LogServiceConfiguration(LogConfig logConfig, string logDirectory)
        {
            ArgumentNullException.ThrowIfNull(logConfig);
            ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);

            EnableFileOutput = logConfig.EnableFileOutput;
            MinimumLevel = logConfig.MinimumLevel;
            LogDirectory = logDirectory;
            RetainedFileCountLimit = Math.Max(1, logConfig.RetainedDays);
            FileSizeLimitBytes = Math.Max(1, logConfig.FileSizeLimitMB) * 1024L * 1024L;
        }
    }
}
