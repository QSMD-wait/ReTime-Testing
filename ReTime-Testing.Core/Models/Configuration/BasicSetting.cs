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

        /// <summary>
        /// 首次启动欢迎引导是否已完成
        /// </summary>
        [JsonPropertyName("welcomeShowed")]
        public bool WelcomeShowed { get; set; } = false;

        /// <summary>
        /// Debug：强制显示欢迎引导（无视其他判定）
        /// </summary>
        [JsonPropertyName("forceShowWelcome")]
        public bool ForceShowWelcome { get; set; } = false;

        /// <summary>
        /// 流畅优化：开启后禁用 Loading 状态进度条阴影等流畅度优化
        /// </summary>
        [JsonPropertyName("smoothnessOptimization")]
        public bool SmoothnessOptimization { get; set; } = false;

        /// <summary>
        /// 静默异常处理：启用后应用不在前台时发生异常将自动处理，不弹崩溃窗口
        /// </summary>
        [JsonPropertyName("criticalSafeMode")]
        public bool CriticalSafeMode { get; set; } = false;

        /// <summary>
        /// 静默异常处理方式：0=静默退出, 1=静默重启, 2=完全忽略（仅记日志）
        /// </summary>
        [JsonPropertyName("criticalSafeModeMethod")]
        public int CriticalSafeModeMethod { get; set; } = 0;
    }
}
