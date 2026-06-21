using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 基本设置配置
    /// </summary>
    public class BasicSetting
    {
        /// <summary>
        /// 自启动配置
        /// </summary>
        [JsonPropertyName("autoStart")]
        public AutoStartConfig AutoStart { get; set; } = new();

        /// <summary>
        /// 主题选择: light（浅色）, dark（深色）
        /// </summary>
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "light";

        /// <summary>
        /// 日志配置
        /// </summary>
        [JsonPropertyName("log")]
        public LogConfig Log { get; set; } = new();
    }
}
