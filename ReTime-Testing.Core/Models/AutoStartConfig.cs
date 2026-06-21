namespace ReTime_Testing.Models
{
    /// <summary>
    /// 自启动配置
    /// </summary>
    public class AutoStartConfig
    {
        /// <summary>
        /// 是否启用自启动
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// 自启动方式: registry（注册表）, startupFolder（启动文件夹）
        /// </summary>
        public string Method { get; set; } = "registry";
    }
}
