using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using ReTime_Testing.Services;

namespace ReTime_Testing.Views.Onboarding.WelcomePages
{
    /// <summary>
    /// 引导页：许可协议（开源说明 + 版权 + 许可证，同意后可继续）
    /// </summary>
    public partial class LicensePage : UserControl
    {
        /// <summary>
        /// 项目仓库地址
        /// </summary>
        private const string RepositoryUrl = "https://github.com/QSMD-wait/ReTime-Testing";

        public LicensePage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 打开项目仓库链接（默认浏览器）
        /// </summary>
        private void OnOpenRepositoryClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(RepositoryUrl)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Warn("LicensePage", $"打开项目仓库失败: {ex.Message}");
            }
        }
    }
}
