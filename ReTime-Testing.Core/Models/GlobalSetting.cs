namespace ReTime_Testing.Models
{
    /// <summary>
    /// 全局配置
    /// </summary>
    public class GlobalSetting
    {
        /// <summary>
        /// 版本号（用于配置迁移）
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// 基本设置域
        /// </summary>
        public BasicSetting Basic { get; set; } = new BasicSetting();
    }
}