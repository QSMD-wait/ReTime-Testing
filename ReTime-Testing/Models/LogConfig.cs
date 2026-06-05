using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 日志配置（BasicSetting.log 域）
    /// </summary>
    public class LogConfig
    {
        /// <summary>
        /// 是否启用文件日志输出
        /// </summary>
        [JsonPropertyName("enableFileOutput")]
        public bool EnableFileOutput { get; set; } = true;

        /// <summary>
        /// 输出的最低日志等级
        /// </summary>
        [JsonPropertyName("minimumLevel")]
        public LogLevel MinimumLevel { get; set; } = LogLevel.INF;

        /// <summary>
        /// 日志文件保留天数
        /// </summary>
        [JsonPropertyName("retainedDays")]
        public int RetainedDays { get; set; } = 30;

        /// <summary>
        /// 单个日志文件大小上限（MB），默认 10MB
        /// </summary>
        [JsonPropertyName("fileSizeLimitMB")]
        public int FileSizeLimitMB { get; set; } = 10;
    }
}
