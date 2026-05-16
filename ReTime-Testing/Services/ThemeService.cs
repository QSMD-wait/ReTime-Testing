using iNKORE.UI.WPF.Modern;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 主题服务实现
    /// </summary>
    public class ThemeService : IThemeService
    {
        public string CurrentTheme { get; private set; } = "light";

        public void ApplyTheme(string themeName)
        {
            CurrentTheme = themeName.ToLower();

            var appTheme = CurrentTheme switch
            {
                "dark" => ApplicationTheme.Dark,
                _ => ApplicationTheme.Light
            };

            ThemeManager.Current.ApplicationTheme = appTheme;

            Logger.Info(nameof(ThemeService), $"主题已应用: {CurrentTheme}");
        }
    }
}
