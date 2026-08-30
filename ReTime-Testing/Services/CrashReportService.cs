using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using ReTime_Testing.Views.CrashWindow;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 崩溃报告服务：集中处理未捕获异常，构建崩溃信息，显示崩溃窗口，持久化日志
    /// </summary>
    public class CrashReportService
    {
        /// <summary>
        /// 构建完整的崩溃报告文本
        /// </summary>
        public string BuildCrashReport(Exception exception)
        {
            var assembly = Assembly.GetEntryAssembly();
            var version = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly?.GetName().Version?.ToString()
                ?? "未知";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var sb = new StringBuilder();
            sb.AppendLine("ReTime - Testing 崩溃报告");
            sb.AppendLine("========================================");
            sb.AppendLine($"时间: {timestamp}");
            sb.AppendLine($"版本: {version}");
            sb.AppendLine($"异常类型: {exception.GetType().FullName}");
            sb.AppendLine($"消息: {exception.Message}");
            sb.AppendLine();

            if (exception.InnerException != null)
            {
                sb.AppendLine("内部异常:");
                sb.AppendLine(exception.InnerException.ToString());
                sb.AppendLine();
            }

            sb.AppendLine("堆栈跟踪:");
            sb.AppendLine(exception.StackTrace ?? "（无堆栈跟踪信息）");

            return sb.ToString();
        }

        /// <summary>
        /// 将崩溃日志保存到文件
        /// </summary>
        public string SaveCrashLog(string crashInfo)
        {
            var logsDir = Path.Combine(AppContext.BaseDirectory, "data", "Logs");
            if (!Directory.Exists(logsDir))
                Directory.CreateDirectory(logsDir);

            var fileName = $"RTT_crash-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.log";
            var filePath = Path.Combine(logsDir, fileName);
            File.WriteAllText(filePath, crashInfo, Encoding.UTF8);
            return filePath;
        }

        /// <summary>
        /// 显示崩溃窗口（作为模态对话框）
        /// </summary>
        /// <param name="crashInfo">崩溃信息文本</param>
        /// <param name="isTerminating">是否为终止性异常（不允许忽略）</param>
        public void ShowCrashWindow(string crashInfo, bool isTerminating)
        {
            try
            {
                var window = new CrashReportWindow
                {
                    CrashInfo = crashInfo,
                    AllowIgnore = !isTerminating
                };

                // 尝试找到父窗口作为 Owner
                Window? owner = null;
                if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
                {
                    owner = Application.Current.MainWindow;
                }
                else if (Application.Current?.Windows != null)
                {
                    foreach (Window w in Application.Current.Windows)
                    {
                        if (w.IsActive && w is not CrashReportWindow)
                        {
                            owner = w;
                            break;
                        }
                    }
                }

                if (owner != null)
                    window.Owner = owner;

                window.ShowDialog();
            }
            catch
            {
                // 崩溃窗口本身创建失败时，回退到 Modern MessageBox
                try
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                        crashInfo,
                        "ReTime - Testing 崩溃",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch
                {
                    // 最终兜底：什么都不做，进程即将退出
                }
            }
        }

        /// <summary>
        /// 重启应用程序（释放互斥锁后启动新进程）
        /// </summary>
        public void RestartApplication(Action? releaseMutex = null)
        {
            try
            {
                releaseMutex?.Invoke();

                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                }

                Environment.Exit(0);
            }
            catch
            {
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        public void ExitApplication(int exitCode = 1)
        {
            Environment.Exit(exitCode);
        }
    }
}
