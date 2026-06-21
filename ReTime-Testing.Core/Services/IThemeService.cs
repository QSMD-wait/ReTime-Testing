namespace ReTime_Testing.Services
{
    /// <summary>
    /// 主题服务接口
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// 获取当前主题名称
        /// </summary>
        string CurrentTheme { get; }

        /// <summary>
        /// 应用指定主题
        /// </summary>
        /// <param name="themeName">主题名称: light, dark</param>
        void ApplyTheme(string themeName);
    }
}
