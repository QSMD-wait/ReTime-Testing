using System;
using System.Reflection;
using System.Windows.Controls;

namespace ReTime_Testing.Views.Onboarding.WelcomePages
{
    /// <summary>
    /// 引导第 1 步：欢迎页
    /// </summary>
    public partial class WelcomePage : UserControl
    {
        public WelcomePage()
        {
            InitializeComponent();
            VersionText.Text = $"版本 {GetAppVersion()}";
        }

        /// <summary>
        /// 读取完整版本号（含 alpha 等尾缀），例如 0.1.0-alpha.1
        /// </summary>
        private static string GetAppVersion()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;
                if (string.IsNullOrWhiteSpace(version))
                    version = "0.0.0";
                return version.Split('+')[0].Trim();
            }
            catch
            {
                return "0.0.0";
            }
        }
    }
}