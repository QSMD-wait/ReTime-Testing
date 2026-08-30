using System;
using System.Diagnostics;
using System.Windows;
using ReTime_Testing.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ReTime_Testing.Views.Settings.Pages;

public partial class AboutPage : SettingsPageBase
{
        private readonly ILogger<AboutPage> _logger = ((App)App.Current).Services.GetRequiredService<ILogger<AboutPage>>();
    /// <summary>
    /// 项目仓库地址
    /// </summary>
    private const string RepositoryUrl = "https://github.com/QSMD-wait/ReTime-Testing";

    public AboutPage()
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
            _logger.LogWarning(ex, "打开项目仓库失败");
        }
    }
}