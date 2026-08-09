using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Media;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReTime_Testing.Models;
using ReTime_Testing.Core.Services;
using ReTime_Testing.Core.Models.Theme;
using ReTime_Testing.Services;
using ReTime_Testing.Views;
using ReTime_Testing.Views.Settings;
using ReTime_Testing.Views.TimeTopDesktop;
using ReTime_Testing.Helpers;

namespace ReTime_Testing
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private IMutexManager? _mutexManager;
        private ITrayIconService? _trayIconService;

        /// <summary>
        /// DI 服务提供者（供非 DI 管理的代码获取服务）
        /// </summary>
        internal IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Host 尚未初始化");

        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// 全局未处理异常捕获
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            var message = ex?.Message ?? "未知错误";
            if (ex != null)
            {
                Logger.Error(GetType().FullName ?? "App", $"全局未处理异常: {message}", ex);
            }
            else
            {
                Logger.Error(GetType().FullName ?? "App", $"全局未处理异常: {message}");
            }

            if (e.IsTerminating)
            {
                ShowWarningAndExit($"应用程序发生未处理异常：\n{message}\n\n程序将退出。");
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
                // 构建 DI 容器（必须在其他操作之前）
                _host = Host.CreateDefaultBuilder()
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

                StartupApplication();
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", $"应用启动失败: {ex.Message}", ex);
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
                // 通过 DI 获取服务
                var configManager = Services.GetRequiredService<IConfigurationManager>();
                var settingsService = Services.GetRequiredService<ISettingsService>();
                var globalSetting = settingsService.GetGlobalSetting();

                // 初始化目录结构
                configManager.InitializeDirectories();

                // 初始化 Serilog 日志服务
                var logConfig = new LogServiceConfiguration(globalSetting.Basic.Log, configManager.LogsDirectory);
                SerilogLogService.Initialize(logConfig);
                Logger.OnSerilogReady();
                Logger.Info(GetType().FullName ?? "App", "Serilog 日志服务已初始化");

                // 应用主题
                var themeService = Services.GetRequiredService<IThemeService>();
                themeService.ApplyTheme(globalSetting.Basic.Theme);

                // 初始化进度条主题服务
                var progressBarThemeService = Services.GetRequiredService<IProgressBarThemeService>();
                progressBarThemeService.LoadAllThemes();
                progressBarThemeService.ApplyTheme(ProgressBarThemeManifest.DefaultId);
                Logger.Info(GetType().FullName ?? "App", "进度条主题服务已初始化");

                // 应用自启动配置
                var autoStartService = Services.GetRequiredService<IAutoStartService>();
                autoStartService.InitializeFromConfig(globalSetting.Basic.AutoStart);

                // 初始化时间计划管理器
                var timeScheduleManager = Services.GetRequiredService<ITimeScheduleManager>();
                timeScheduleManager.Initialize();

                // 初始化时间服务
                var timeService = Services.GetRequiredService<ITimeService>();
                Logger.Info(GetType().FullName ?? "App", "单调时钟服务已初始化");

                // 初始化时间校准服务
                var timeTopSetting = settingsService.GetTimeTopSetting();
                var timeCalibrationService = Services.GetRequiredService<ITimeCalibrationService>();
                timeCalibrationService.ApplyConfig(timeTopSetting.Calibration);
                Logger.Info(GetType().FullName ?? "App", "时间校准服务已初始化");

                // 恢复用户时间偏移
                var userOffsetSeconds = timeTopSetting.Calibration.UserOffsetSeconds;
                if (double.IsNaN(userOffsetSeconds) || double.IsInfinity(userOffsetSeconds))
                    userOffsetSeconds = 0;
                if (userOffsetSeconds != 0)
                {
                    timeService.ApplyUserOffset(TimeSpan.FromSeconds(userOffsetSeconds));
                }

                // 初始化调度管理器
                var scheduleManager = Services.GetRequiredService<IScheduleManager>();
                var planGenerator = new ExecutionPlanGenerator();
                Logger.Info(GetType().FullName ?? "App", "执行计划生成器已初始化");

                if (!timeTopSetting.Schedule.Enabled)
                {
                    Logger.Info(GetType().FullName ?? "App", "时间计划控制已禁用，跳过调度初始化");
                }
                else
                {
                    var scheduleGroupManager = Services.GetRequiredService<IScheduleGroupManager>();
                    var effectiveScheduleId = scheduleGroupManager.GetEffectiveScheduleId();

                    if (effectiveScheduleId == null)
                    {
                        Logger.Info(GetType().FullName ?? "App", "今日无生效计划表，保持空闲状态");
                    }
                    else
                    {
                        var selectedSchedule = timeScheduleManager.LoadSchedule(effectiveScheduleId);

                        if (selectedSchedule == null)
                        {
                            Logger.Error(GetType().FullName ?? "App",
                                $"生效计划表无效或不存在: {effectiveScheduleId}，保持空闲状态");
                            ShowValidationErrorDialog(
                                $"计划表 \"{effectiveScheduleId}\" 无效或不存在。\n\n请检查计划表组配置或计划表文件是否完整。");
                        }
                        else
                        {
                            var currentTime = timeService.GetCurrentTime();
                            var executionPlan = planGenerator.GenerateSafe(selectedSchedule, DateTime.Today, currentTime);

                            if (executionPlan == null)
                            {
                                Logger.Warn(GetType().FullName ?? "App", "时间计划验证失败，保持空闲状态");
                                ShowValidationErrorDialog("时间计划配置无效，已保持空闲状态。\n\n请检查时间计划表配置是否正确。");
                            }
                            else
                            {
                                Logger.Info(GetType().FullName ?? "App", $"执行计划已生成: {executionPlan}");
                                scheduleManager.Initialize(executionPlan);
                                Logger.Info(GetType().FullName ?? "App", "调度管理器已启动");
                            }
                        }
                    }
                }

                // 启动时间校准服务
                timeCalibrationService.Start();
                Logger.Info(GetType().FullName ?? "App", "时间校准服务已启动");

                // 初始化全局服务
                var globalService = Services.GetRequiredService<IGlobalTimeTopDesktopService>();

                // 初始化系统托盘图标服务
                _trayIconService = Services.GetRequiredService<ITrayIconService>();
                _trayIconService.Initialize(new TrayIconService.TrayIconConfig
                {
                    Title = "ReTime - Testing",
                    IconResource = "ReTime-Testing;component/Resources/app.ico"
                });

                _trayIconService.OpenSettingRequested += OpenSetting;
                _trayIconService.OpenDebugRequested += OpenDebugTest;
                _trayIconService.OpenTimeScheduleEditorRequested += OpenTimeScheduleEditor;
                _trayIconService.AboutRequested += OpenMainWindow;
                _trayIconService.RestartRequested += RestartApplication;
                _trayIconService.ExitRequested += ExitApplication;

                // 订阅配置变更事件（热重载）
                settingsService.OnGlobalSettingChanged += OnGlobalSettingChanged;
                settingsService.OnTimeTopSettingChanged += OnTimeTopSettingChanged;

                // 打开进度条窗口
                var desktopWindowManager = Services.GetRequiredService<IDesktopWindowManager>();
                var initialPosition = ParsePosition(timeTopSetting.ProgressBar.Position);
                desktopWindowManager.SetPosition(initialPosition);

                Logger.Info(GetType().FullName ?? "App", "应用程序启动成功");

                // 首次校准（非阻塞：UI已就绪，校准在后台执行，不阻塞启动流程）
                if (timeTopSetting.Calibration.Enabled)
                {
                    _ = PerformFirstCalibrationAsync(timeCalibrationService);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", "启动应用程序时发生异常", ex);
                Shutdown();
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
                    Logger.Info(GetType().FullName ?? "App", "首次校准成功");
                }
                else
                {
                    Logger.Warn(GetType().FullName ?? "App", "首次校准失败，使用系统时间");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(GetType().FullName ?? "App", $"首次校准异常: {ex.Message}，使用系统时间");
            }
        }

        /// <summary>
        /// 全局配置变更回调（热重载）
        /// </summary>
        private void OnGlobalSettingChanged(GlobalSetting setting)
        {
            try
            {
                var themeService = Services.GetRequiredService<IThemeService>();
                themeService.ApplyTheme(setting.Basic.Theme);
                Logger.Info(GetType().FullName ?? "App", "热重载：主题已刷新");
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", $"热重载主题失败: {ex.Message}", ex);
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
                desktopWindowManager.RefreshTextOverlay();
                desktopWindowManager.ApplyTopmostModeFromConfig();
                Logger.Info(GetType().FullName ?? "App", "热重载：窗口位置/文字覆盖/层级已刷新");
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", $"热重载窗口配置失败: {ex.Message}", ex);
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
                Logger.Error(GetType().FullName ?? "App", "互斥锁管理器配置为 null");
                Shutdown();
                return;
            }

            ShowModernConflictDialog(config);

            Logger.Warn(GetType().FullName ?? "App", "检测到多实例运行，显示冲突弹窗");

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
                Logger.Error(GetType().FullName ?? "App", "显示 Modern UI 对话框时发生异常，回退到标准 MessageBox", ex);

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
            Logger.Warn(GetType().FullName ?? "App", $"互斥锁冲突事件触发，冲突时间: {e.ConflictTime}");
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
        /// 显示警告并退出程序
        /// </summary>
        private void ShowWarningAndExit(string message)
        {
            try
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show(
                    message,
                    "配置无效",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning,
                    MessageBoxResult.OK
                );
            }
            catch
            {
                MessageBox.Show(message, "配置无效", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            Environment.Exit(1);
        }

        /// <summary>
        /// 互斥锁获取成功事件处理
        /// </summary>
        private void OnMutexAcquired(object? sender, EventArgs e)
        {
            Logger.Info(GetType().FullName ?? "App", "互斥锁获取成功事件触发");
        }

        /// <summary>
        /// 打开设置窗口
        /// </summary>
        private void OpenSetting()
        {
            try
            {
                WindowManager.ShowTimeTopSetting();
                Logger.Info(GetType().FullName ?? "App", "设置窗口已打开");
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", "打开设置窗口时发生异常", ex);
            }
        }

        /// <summary>
        /// 打开主窗口（关于窗口）
        /// </summary>
        private void OpenMainWindow()
        {
            try
            {
                WindowManager.ShowMainWindow();
                Logger.Info(GetType().FullName ?? "App", "主窗口已打开");
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", "打开主窗口时发生异常", ex);
            }
        }

        /// <summary>
        /// 打开调试测试窗口
        /// </summary>
        private void OpenDebugTest()
        {
            try
            {
                WindowManager.ShowDebugTest();
                Logger.Info(GetType().FullName ?? "App", "调试测试窗口已打开");
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", "打开调试测试窗口时发生异常", ex);
            }
        }

        /// <summary>
        /// 打开时间计划编辑器
        /// </summary>
        private void OpenTimeScheduleEditor()
        {
            try
            {
                WindowManager.ShowTimeScheduleEditor();
                Logger.Info(GetType().FullName ?? "App", "时间计划编辑器已打开");
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", "打开时间计划编辑器时发生异常", ex);
            }
        }

        /// <summary>
        /// 重启应用程序
        /// </summary>
        private async void RestartApplication()
        {
            try
            {
                Logger.Info(GetType().FullName ?? "App", "应用程序重启请求");

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
                Logger.Error(GetType().FullName ?? "App", "重启应用程序时发生异常", ex);
            }
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            try
            {
                Logger.Info(GetType().FullName ?? "App", "应用程序退出请求");
                Shutdown();
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", "退出应用程序时发生异常", ex);
                Shutdown();
            }
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

            // 释放托盘图标
            _trayIconService?.Dispose();

            // 释放互斥锁
            _mutexManager?.Release();

            // 取消事件订阅
            if (_trayIconService != null)
            {
                _trayIconService.OpenSettingRequested -= OpenSetting;
                _trayIconService.OpenDebugRequested -= OpenDebugTest;
                _trayIconService.OpenTimeScheduleEditorRequested -= OpenTimeScheduleEditor;
                _trayIconService.AboutRequested -= OpenMainWindow;
                _trayIconService.RestartRequested -= RestartApplication;
                _trayIconService.ExitRequested -= ExitApplication;
            }

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

            Logger.Info(GetType().FullName ?? "App", "应用程序退出");
            SerilogLogService.Instance?.Dispose();

            _host?.Dispose();

            base.OnExit(e);
        }

        /// <summary>
        /// 将配置字符串解析为 ProgressBarPosition 枚举
        /// </summary>
        private static ProgressBarPosition ParsePosition(string position)
        {
            return position?.ToLowerInvariant() switch
            {
                "bottom" => ProgressBarPosition.Bottom,
                "left" => ProgressBarPosition.Left,
                "right" => ProgressBarPosition.Right,
                _ => ProgressBarPosition.Top
            };
        }
    }
}