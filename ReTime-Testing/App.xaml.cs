using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Sockets;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReTime_Testing.Models;
using ReTime_Testing.Core.Services;
using ReTime_Testing.Core.Models.Theme;
using ReTime_Testing.Services;
using ReTime_Testing.Services.Onboarding;
using ReTime_Testing.Views;
using ReTime_Testing.Views.Settings;
using ReTime_Testing.Views.TimeTopDesktop;
using ReTime_Testing.Helpers;
using ReTime_Testing.Views.CrashWindow;
using Serilog;

namespace ReTime_Testing
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    /// <remarks>
    /// App 仅负责：Host 构建、应用生命周期（启动/退出）、互斥锁与全局异常兜底；
    /// 启动编排见 <see cref="AppBootstrapper"/>，调度编排见 <see cref="IScheduleOrchestrator"/>，
    /// 托盘事件路由见 <see cref="TrayIconController"/>。
    /// </remarks>
    public partial class App : Application
    {
        private readonly ILogger<App> _logger = AppLog.For<App>();
        private readonly CrashReportService _crashService = new();
        private IHost? _host;
        private IMutexManager? _mutexManager;
        private ITrayIconService? _trayIconService;
        private TrayIconController? _trayIconController;

        /// <summary>
        /// 静默异常处理模式是否启用（运行时标志，从设置加载）
        /// </summary>
        internal static bool IsCriticalSafeModeEnabled = false;

        /// <summary>
        /// DI 服务提供者（供非 DI 管理的代码获取服务）
        /// </summary>
        internal IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Host 尚未初始化");

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        /// <summary>
        /// AppDomain 全局未处理异常捕获（最早捕获点）
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                _logger.LogError(ex, "AppDomain 未处理异常: {Message}", ex.Message);
            }
            else
            {
                _logger.LogError("AppDomain 未处理异常（无 Exception 对象）");
            }

            if (e.IsTerminating)
            {
                // 安全模式：终止性异常仍强制退出（进程即将死亡，无法恢复）
                if (IsCriticalSafeModeEnabled)
                {
                    _logger.LogInformation("安全模式：AppDomain 终止性异常，记录日志后退出");
                    Environment.Exit(1);
                    return;
                }

                try
                {
                    var crashInfo = _crashService.BuildCrashReport(ex ?? new Exception("未知错误"));
                    _crashService.SaveCrashLog(crashInfo);
                    _crashService.ShowCrashWindow(crashInfo, isTerminating: true);
                }
                catch
                {
                    // 崩溃窗口显示失败时静默退出
                }

                Environment.Exit(1);
            }
        }

        /// <summary>
        /// WPF Dispatcher UI 线程未处理异常
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 安全模式 + 应用不在前台：静默处理
            if (IsCriticalSafeModeEnabled && !IsAppInForeground())
            {
                HandleSafeModeException(e.Exception);
                e.Handled = true;
                return;
            }

            _logger.LogError(e.Exception, "Dispatcher 未处理异常: {Message}", e.Exception.Message);

            try
            {
                var crashInfo = _crashService.BuildCrashReport(e.Exception);
                _crashService.SaveCrashLog(crashInfo);
                _crashService.ShowCrashWindow(crashInfo, isTerminating: false);
            }
            catch
            {
                // 崩溃窗口显示失败时兜底
            }

            e.Handled = true;
        }

        /// <summary>
        /// TaskScheduler 未观察异常（后台 Task 中未 await 的异常）
        /// </summary>
        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            // 过滤应用退出时的良性异常（如 NTP UDP 接收被中断的 SocketException 995）
            if (IsBenignShutdownException(e.Exception))
            {
                e.SetObserved();
                return;
            }

            // 安全模式 + 应用不在前台：静默处理
            if (IsCriticalSafeModeEnabled && !IsAppInForeground())
            {
                Dispatcher.BeginInvoke(() => HandleSafeModeException(e.Exception));
                e.SetObserved();
                return;
            }

            _logger.LogError(e.Exception, "TaskScheduler 未观察异常: {Message}", e.Exception.Message);

            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var crashInfo = _crashService.BuildCrashReport(e.Exception);
                    _crashService.SaveCrashLog(crashInfo);
                    _crashService.ShowCrashWindow(crashInfo, isTerminating: false);
                }
                catch
                {
                    // 崩溃窗口显示失败时兜底
                }
            });

            e.SetObserved();
        }

        /// <summary>
        /// 判断是否为应用退出时产生的良性异常
        /// </summary>
        private static bool IsBenignShutdownException(Exception exception)
        {
            // TaskScheduler.UnobservedTaskException 包装的 AggregateException
            if (exception is AggregateException aggregate)
            {
                foreach (var inner in aggregate.Flatten().InnerExceptions)
                {
                    if (inner is SocketException { ErrorCode: 995 })
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断应用主窗口是否在前台（活跃状态）
        /// </summary>
        private bool IsAppInForeground()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow;
                return mainWindow != null && mainWindow.IsActive;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 安全模式异常处理：根据设置选择静默退出/重启/忽略
        /// </summary>
        private void HandleSafeModeException(Exception exception)
        {
            var method = 0;
            try
            {
                method = Services.GetService<ISettingsService>()?.GetGlobalSetting().Basic.CriticalSafeModeMethod ?? 0;
            }
            catch
            {
                // 读取设置失败时默认静默退出
            }

            _logger.LogError(exception, "安全模式：处理未捕获异常 (Method={Method})", method);

            switch (method)
            {
                case 1: // 静默重启
                    _logger.LogInformation("安全模式：静默重启");
                    Action? releaseMutex = _mutexManager != null ? () => _mutexManager.Release() : null;
                    _crashService.RestartApplication(releaseMutex);
                    break;
                case 2: // 完全忽略
                    _logger.LogInformation("安全模式：忽略异常，继续运行");
                    break;
                default: // 0 或未知：静默退出
                    _logger.LogInformation("安全模式：静默退出");
                    Environment.Exit(1);
                    break;
            }
        }

        // 公共属性（过渡期保留，供 View code-behind 使用）
        public ITimeService TimeService => Services.GetRequiredService<ITimeService>();
        public ITimeCalibrationService TimeCalibrationService => Services.GetRequiredService<ITimeCalibrationService>();
        public IScheduleManager ScheduleManager => Services.GetRequiredService<IScheduleManager>();
        public IThemeService ThemeService => Services.GetRequiredService<IThemeService>();
        public IAutoStartService AutoStartService => Services.GetRequiredService<IAutoStartService>();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 控制台 UTF-8 编码，解决中文乱码
                Console.OutputEncoding = Encoding.UTF8;

                // 最早阶段：初始化 Serilog 全局日志器（控制台 + 文件 + 内存缓冲）
                InitializeLogging();

                // 构建 DI 容器，使用 Serilog 接管 Host 日志管道
                _host = Host.CreateDefaultBuilder()
                    .UseSerilog(Log.Logger, dispose: false)
                    .ConfigureServices((context, services) =>
                    {
                        services.AddReTimeServices();
                    })
                    .Build();

                // 初始化互斥锁管理器
                _mutexManager = Services.GetRequiredService<IMutexManager>();
                _mutexManager.OnConflictDetected += OnMutexConflictDetected;
                _mutexManager.OnMutexAcquired += OnMutexAcquired;

                bool mutexAcquired = _mutexManager.TryAcquire();

                if (!mutexAcquired)
                {
                    HandleMutexConflict();
                    return;
                }

                // 从设置加载安全模式运行时标志
                try
                {
                    var settings = Services.GetRequiredService<ISettingsService>().GetGlobalSetting();
                    IsCriticalSafeModeEnabled = settings.Basic.CriticalSafeMode;
                }
                catch
                {
                    // 读取设置失败时安全模式默认关闭
                }

                StartupApplication();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用启动失败");
                ShowWarningAndExit($"应用启动失败：\n{ex.Message}");
            }
        }

        /// <summary>
        /// 启动应用程序主窗口
        /// </summary>
        private async void StartupApplication()
        {
            try
            {
                // 执行启动编排（日志/主题/校准/调度/桌面窗口初始化）
                var bootstrapper = Services.GetRequiredService<AppBootstrapper>();
                var result = bootstrapper.RunStartup();

                // 首次启动：进入欢迎引导模式（不启动调度，完成后重启进入正常启动流程）
                if (result.NeedsWelcomeFlow)
                {
                    RunWelcomeFlow();
                    return;
                }

                if (result.ScheduleValidationError != null)
                {
                    ShowValidationErrorDialog(result.ScheduleValidationError);
                }

                var timeTopSetting = result.TimeTopSetting!;

                // 初始化系统托盘图标（含事件路由）
                _trayIconService = Services.GetRequiredService<ITrayIconService>();
                _trayIconController = new TrayIconController(_trayIconService, RestartApplication, ExitApplication, Services.GetRequiredService<ILogger<TrayIconController>>());
                _trayIconController.Initialize();

                // 订阅配置变更事件（热重载）
                var settingsService = Services.GetRequiredService<ISettingsService>();
                settingsService.OnGlobalSettingChanged += OnGlobalSettingChanged;
                settingsService.OnTimeTopSettingChanged += OnTimeTopSettingChanged;

                // 首次校准（非阻塞：UI已就绪，校准在后台执行，不阻塞启动流程）
                if (timeTopSetting.Calibration.Enabled)
                {
                    _ = PerformFirstCalibrationAsync(Services.GetRequiredService<ITimeCalibrationService>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动应用程序时发生异常");
                try
                {
                    var crashInfo = _crashService.BuildCrashReport(ex);
                    _crashService.SaveCrashLog(crashInfo);
                    _crashService.ShowCrashWindow(crashInfo, isTerminating: false);
                }
                catch
                {
                    Shutdown();
                }
            }
        }

        /// <summary>
        /// 欢迎引导模式：最小化启动（不初始化调度/校准），完成后重启进入正常启动流程
        /// </summary>
        private void RunWelcomeFlow()
        {
            try
            {
                var settingsService = Services.GetRequiredService<ISettingsService>();

                // 准备引导环境（主题/进度条窗口位置/流畅优化）
                var bootstrapper = Services.GetRequiredService<AppBootstrapper>();
                bootstrapper.PrepareWelcomeEnvironment();

                // 向导模式：订阅配置变更事件（热重载）
                settingsService.OnTimeTopSettingChanged += OnTimeTopSettingChanged;
                settingsService.OnGlobalSettingChanged += OnGlobalSettingChanged;

                // 引导模式：加载托盘图标（不带右键菜单）
                _trayIconService = Services.GetRequiredService<ITrayIconService>();
                _trayIconController = new TrayIconController(_trayIconService, RestartApplication, ExitApplication, Services.GetRequiredService<ILogger<TrayIconController>>());
                _trayIconController.Initialize(showContextMenu: false);

                _logger.LogInformation("进入欢迎引导模式");

                var welcomeWindow = new Views.Onboarding.WelcomeWindow();
                welcomeWindow.ShowDialog();

                // 取消订阅向导模式的配置变更事件
                settingsService.OnTimeTopSettingChanged -= OnTimeTopSettingChanged;
                settingsService.OnGlobalSettingChanged -= OnGlobalSettingChanged;

                if (!welcomeWindow.IsWizardCompleted)
                {
                    _logger.LogInformation("欢迎引导未完成，退出应用");
                    Shutdown();
                    return;
                }

                _logger.LogInformation("欢迎引导完成，重启进入正常启动流程");
                RestartApplication();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "欢迎引导流程发生异常");
                try
                {
                    var crashInfo = _crashService.BuildCrashReport(ex);
                    _crashService.SaveCrashLog(crashInfo);
                    _crashService.ShowCrashWindow(crashInfo, isTerminating: false);
                }
                catch
                {
                    Shutdown();
                }
            }
        }

        /// <summary>
        /// 首次校准（后台执行，不阻塞UI初始化）
        /// </summary>
        private async Task PerformFirstCalibrationAsync(ITimeCalibrationService timeCalibrationService)
        {
            try
            {
                var success = await timeCalibrationService.CalibrateAsync();
                if (success)
                {
                    _logger.LogInformation("首次校准成功");
                }
                else
                {
                    _logger.LogWarning("首次校准失败，使用系统时间");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "首次校准异常，使用系统时间");
            }
        }

        /// <summary>
        /// 全局配置变更回调（热重载）
        /// </summary>
        private void OnGlobalSettingChanged(GlobalSetting setting)
        {
            // 同步安全模式运行时标志
            IsCriticalSafeModeEnabled = setting.Basic.CriticalSafeMode;

            try
            {
                var themeService = Services.GetRequiredService<IThemeService>();
                themeService.ApplyTheme(setting.Basic.Theme);
                _logger.LogInformation("热重载：主题已刷新");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "热重载主题失败");
            }
        }

        /// <summary>
        /// TimeTop配置变更回调（热重载）
        /// </summary>
        private void OnTimeTopSettingChanged(TimeTopSetting setting)
        {
            try
            {
                var desktopWindowManager = Services.GetRequiredService<IDesktopWindowManager>();
                desktopWindowManager.RefreshPosition();
                desktopWindowManager.RefreshProgressBarScale();
                desktopWindowManager.RefreshShadow();
                desktopWindowManager.RefreshTextOverlay();
                desktopWindowManager.ApplyTopmostModeFromConfig();
                _logger.LogInformation("热重载：窗口位置/缩放/阴影/文字覆盖/层级已刷新");

                // 热重载调度器：重新评估生效计划表并更新执行计划
                Services.GetRequiredService<IScheduleOrchestrator>().ApplyScheduleConfig(setting.Schedule.Enabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "热重载窗口配置失败");
            }
        }

        /// <summary>
        /// 处理互斥锁冲突
        /// </summary>
        private void HandleMutexConflict()
        {
            var config = _mutexManager?.Config;

            if (config == null)
            {
                _logger.LogError("互斥锁管理器配置为 null");
                Shutdown();
                return;
            }

            ShowModernConflictDialog(config);

            _logger.LogWarning("检测到多实例运行，显示冲突弹窗");

            if (config.AutoShutdownOnConflict)
            {
                Shutdown();
            }
        }

        /// <summary>
        /// 显示 Modern UI 风格的冲突对话框
        /// </summary>
        private void ShowModernConflictDialog(MutexConfig config)
        {
            try
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    config.ConflictWindowMessage,
                    config.ConflictWindowTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    MessageBoxResult.OK,
                    config.PlaySound ? SystemSounds.Beep : null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "显示 Modern UI 对话框时发生异常，回退到标准 MessageBox");

                MessageBox.Show(
                    config.ConflictWindowMessage,
                    config.ConflictWindowTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        /// <summary>
        /// 互斥锁冲突事件处理
        /// </summary>
        private void OnMutexConflictDetected(object? sender, MutexConflictEventArgs e)
        {
                _logger.LogWarning("互斥锁冲突事件触发，冲突时间: {ConflictTime}", e.ConflictTime);
        }

        /// <summary>
        /// 显示验证错误提示（启动后弹窗）
        /// </summary>
        private void ShowValidationErrorDialog(string message)
        {
            Task.Delay(1000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        Window? owner = null;
                        if (Application.Current?.MainWindow != null && Application.Current.MainWindow.IsVisible)
                        {
                            owner = Application.Current.MainWindow;
                        }
                        else
                        {
                            var windows = Application.Current?.Windows;
                            if (windows != null)
                            {
                                foreach (Window w in windows)
                                {
                                    if (w.IsActive)
                                    {
                                        owner = w;
                                        break;
                                    }
                                }
                            }
                        }

                        iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                            owner,
                            message,
                            "配置无效",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning,
                            MessageBoxResult.OK
                        );
                    }
                    catch
                    {
                        MessageBox.Show(
                            message,
                            "配置无效",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                });
            });
        }

        /// <summary>
        /// 显示崩溃窗口并退出程序
        /// </summary>
        private void ShowWarningAndExit(string message)
        {
            try
            {
                var crashInfo = _crashService.BuildCrashReport(new Exception(message));
                _crashService.SaveCrashLog(crashInfo);
                _crashService.ShowCrashWindow(crashInfo, isTerminating: true);
            }
            catch
            {
                try
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                        message,
                        "ReTime - Testing 崩溃",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch
                {
                    // 最终兜底
                }
            }

            Environment.Exit(1);
        }

        /// <summary>
        /// 互斥锁获取成功事件处理
        /// </summary>
        private void OnMutexAcquired(object? sender, EventArgs e)
        {
            _logger.LogInformation("互斥锁获取成功事件触发");
        }

        /// <summary>
        /// 重启应用程序
        /// </summary>
        private async void RestartApplication()
        {
            try
            {
                _logger.LogInformation("应用程序重启请求");

                // 先释放互斥锁，避免新进程获取失败
                _mutexManager?.Release();

                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                }

                await Task.Delay(500);
                Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重启应用程序时发生异常");
            }
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            try
            {
                _logger.LogInformation("应用程序退出请求");
                Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "退出应用程序时发生异常");
                Shutdown();
            }
        }

        /// <summary>
        /// 在应用启动最早阶段初始化 Serilog 日志系统
        /// 直接读取 Setting.json 提取日志配置，不依赖 DI 容器
        /// </summary>
        private void InitializeLogging()
        {
            var logConfig = new LogConfig();
            var logsDirectory = Path.Combine(AppContext.BaseDirectory, "data", "Logs");

            try
            {
                var settingPath = Path.Combine(AppContext.BaseDirectory, "data", "Setting.json");
                if (File.Exists(settingPath))
                {
                    var json = File.ReadAllText(settingPath);
                    var node = JsonNode.Parse(json,
                        null,
                        new JsonDocumentOptions { AllowTrailingCommas = true });

                    var logNode = node?["basic"]?["log"];
                    if (logNode != null)
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        var parsed = logNode.Deserialize<LogConfig>(options);
                        if (parsed != null)
                            logConfig = parsed;
                    }
                }

                if (!Directory.Exists(logsDirectory))
                    Directory.CreateDirectory(logsDirectory);
            }
            catch
            {
                // 配置读取失败时使用默认 LogConfig 兜底，确保应用仍可启动
            }

            LoggingSetup.Initialize(logConfig, logsDirectory);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 停止业务服务
            if (_host != null)
            {
                var scheduleManager = _host.Services.GetService<IScheduleManager>();
                var timeCalibrationService = _host.Services.GetService<ITimeCalibrationService>();

                scheduleManager?.Stop();
                timeCalibrationService?.Stop();
            }

            // 释放托盘图标及事件订阅
            _trayIconController?.Dispose();
            _trayIconService?.Dispose();

            // 释放互斥锁
            _mutexManager?.Release();

            // 取消互斥锁事件订阅
            if (_mutexManager != null)
            {
                _mutexManager.OnConflictDetected -= OnMutexConflictDetected;
                _mutexManager.OnMutexAcquired -= OnMutexAcquired;
            }

            // 取消配置变更事件订阅
            if (_host != null)
            {
                var settingsService = _host.Services.GetService<ISettingsService>();
                if (settingsService != null)
                {
                    settingsService.OnGlobalSettingChanged -= OnGlobalSettingChanged;
                    settingsService.OnTimeTopSettingChanged -= OnTimeTopSettingChanged;
                }
            }

            _logger.LogInformation("应用程序退出");
            LoggingSetup.Shutdown();

            _host?.Dispose();

            base.OnExit(e);
        }
    }
}