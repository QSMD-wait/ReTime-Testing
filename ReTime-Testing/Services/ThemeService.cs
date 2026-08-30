using Microsoft.Extensions.Logging;
using iNKORE.UI.WPF.Modern;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 主题服务实现
    /// </summary>
    public class ThemeService : IThemeService
    {
        private readonly ILogger<ThemeService> _logger;
        public string CurrentTheme { get; private set; } = "light";

        public ThemeService(ILogger<ThemeService> logger)
        {
            _logger = logger;
        }

        public void ApplyTheme(string themeName)
        {
            CurrentTheme = themeName.ToLower();

            var appTheme = CurrentTheme switch
            {
                "dark" => ApplicationTheme.Dark,
                _ => ApplicationTheme.Light
            };

            ThemeManager.Current.ApplicationTheme = appTheme;

            _logger.LogInformation("主题已应用: {Theme}", CurrentTheme);
        }
    }
}
