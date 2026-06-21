using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 日志等级
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum LogLevel
    {
        /// <summary>
        /// 跟踪
        /// </summary>
        TRC,

        /// <summary>
        /// 调试
        /// </summary>
        DBG,

        /// <summary>
        /// 信息
        /// </summary>
        INF,

        /// <summary>
        /// 警告
        /// </summary>
        WRN,

        /// <summary>
        /// 错误
        /// </summary>
        ERR
    }
}
