using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Media;
using System.Windows;
using ReTime_Testing.Models;
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
        private MutexManager? _mutexManager;
        private TrayIconService? _trayIconService;

        // 服务字段（改为 internal 便于调试）
        internal ITimeService? _timeService;
        internal ICloudCalibrationService? _cloudCalibrationService;
        internal ITimeCalibrationService? _timeCalibrationService;
        internal ScheduleManager? _scheduleManager;
        internal IThemeService? _themeService;
        internal IAutoStartService? _autoStartService;

        public App()
        {
            // 注册全局异常处理
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

        // 公共属性用于访问服务
        public ITimeService? TimeService => _timeService;
        public ICloudCalibrationService? CloudCalibrationService => _cloudCalibrationService;
        public ITimeCalibrationService? TimeCalibrationService => _timeCalibrationService;
        public ScheduleManager? ScheduleManager => _scheduleManager;
        public IThemeService? ThemeService => _themeService;
        public IAutoStartService? AutoStartService => _autoStartService;

protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 初始化互斥锁管理器
                _mutexManager = MutexManager.Instance;

                // 订阅互斥锁事件
                _mutexManager.OnConflictDetected += OnMutexConflictDetected;
                _mutexManager.OnMutexAcquired += OnMutexAcquired;

                // 尝试获取互斥锁
                bool mutexAcquired = _mutexManager.TryAcquire();

                // 如果未获取到互斥锁，则启动冲突处理流程
                if (!mutexAcquired)
                {
                    HandleMutexConflict();
                    return;
                }

                // 互斥锁获取成功，正常启动应用
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
                // 初始化配置管理器（必须在其他服务之前初始化）
                var configManager = Services.ConfigurationManager.Instance;
                configManager.InitializeDirectories();

                // 加载全局配置（确保空文件/缺失字段被正确初始化）
                var globalSetting = configManager.LoadGlobalSetting();

                // 初始化 Serilog 日志服务（必须在其他服务之前，确保 Logger 走 Serilog 管道）
                var logConfig = new LogServiceConfiguration(globalSetting.Basic.Log, configManager.LogsDirectory);
                SerilogLogService.Initialize(logConfig);
                Logger.OnSerilogReady(); // 回放缓存的早期日志到同一文件
                Logger.Info(GetType().FullName ?? "App", "Serilog 日志服务已初始化");

                // 初始化主题服务并应用配置
                _themeService = new ThemeService();
                _themeService.ApplyTheme(globalSetting.Basic.Theme);

                // 初始化自启动服务并应用配置
                _autoStartService = new AutoStartService();
                _autoStartService.InitializeFromConfig(globalSetting.Basic.AutoStart);

                // 初始化时间计划管理器
                var scheduleManager = Services.TimeScheduleManager.Instance;
                scheduleManager.Initialize();

                // ===== 初始化时间服务 =====
                // 1. 初始化绝对时间服务（纯单调时钟）
                _timeService = new AbsoluteTimeService();
                Logger.Info(GetType().FullName ?? "App", "单调时钟服务已初始化");

                // 2. 初始化云端校准数据源（纯NTP数据源）
                var timeTopSetting = configManager.LoadTimeTopSetting();
                _cloudCalibrationService = new CloudCalibrationService();

                // 3. 初始化时间校准服务（校准协调器）
                _timeCalibrationService = new TimeCalibrationService(_timeService, _cloudCalibrationService);
                _timeCalibrationService.ApplyConfig(timeTopSetting.Calibration);
                Logger.Info(GetType().FullName ?? "App", "时间校准服务已初始化");

                // 5. 初始化执行计划生成器
                var planGenerator = new ExecutionPlanGenerator();
                Logger.Info(GetType().FullName ?? "App", "执行计划生成器已初始化");

                // 6. 生成执行计划
                if (!timeTopSetting.Schedule.Enabled)
                {
                    Logger.Info(GetType().FullName ?? "App", "时间计划控制已禁用，跳过调度初始化");
                }
                else
                {
                    var selectedSchedule = scheduleManager.LoadSchedule(timeTopSetting.Schedule.SelectedId);
                    if (selectedSchedule == null)
                    {
                        selectedSchedule = scheduleManager.LoadSchedule("Default");
                    }

                    if (selectedSchedule != null)
                    {
                        var currentTime = _timeService.GetCurrentTime();
                        var executionPlan = planGenerator.GenerateSafe(selectedSchedule, DateTime.Today, currentTime);

                        // 始终创建 ScheduleManager 实例（窗口构造需要非空实例）
                        _scheduleManager = new ScheduleManager(_timeService, GlobalTimeTopDesktopService.Instance.StateManager);

                        if (executionPlan == null)
                        {
                            // 验证失败，保持空闲状态，记录警告
                            Logger.Warn(GetType().FullName ?? "App", "时间计划验证失败，保持空闲状态");
                            ShowValidationErrorDialog("时间计划配置无效，已保持空闲状态。\n\n请检查时间计划表配置是否正确。");
                        }
                        else
                        {
                            Logger.Info(GetType().FullName ?? "App", $"执行计划已生成: {executionPlan}");

                            // 7. 初始化调度管理器
                            _scheduleManager.Initialize(executionPlan);
                            Logger.Info(GetType().FullName ?? "App", "调度管理器已启动");
                        }
                    }
                }

                // 8. 启动时间校准服务（长期运行）
                _timeCalibrationService.Start();
                Logger.Info(GetType().FullName ?? "App", "时间校准服务已启动");

                // 9. 启动后执行首次校准
                if (timeTopSetting.Calibration.Enabled)
                {
                    try
                    {
                        var success = await _timeCalibrationService.CalibrateAsync();
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

                // 注意：不再调用 GlobalTimeTopDesktopService.Instance.InitializeAndApplySchedule()
                // 因为现在使用新的 ScheduleManager 进行调度，避免双重调度冲突
                // GlobalTimeTopDesktopService 保留状态管理功能（SetLoading, SetProgress 等）

                // 初始化全局服务
                var service = GlobalTimeTopDesktopService.Instance;

// 初始化系统托盘图标服务
                _trayIconService = TrayIconService.Instance;
                _trayIconService.Initialize(new TrayIconService.TrayIconConfig
                {
                    Title = "ReTime - Testing",
                    IconResource = "ReTime-Testing;component/Resources/app.ico"
                });

                // 订阅托盘图标事件
                _trayIconService.OpenSettingRequested += OpenSetting;
                _trayIconService.OpenDebugRequested += OpenDebugTest;
                _trayIconService.OpenTimeScheduleEditorRequested += OpenTimeScheduleEditor; // 订阅新事件
                _trayIconService.AboutRequested += OpenMainWindow;
                _trayIconService.RestartRequested += RestartApplication;
                _trayIconService.ExitRequested += ExitApplication;

                // 使用 WindowManager 打开主窗口和调试测试窗口
                // WindowManager.ShowMainWindow();
                // WindowManager.ShowDebugTest();
                // WindowManager.ShowTimeScheduleEditor();

                // 使用 DesktopWindowManager 打开进度条窗口（默认顶部）
                DesktopWindowManager.Instance.SetPosition(ProgressBarPosition.Top);

                Logger.Info(GetType().FullName ?? "App", "应用程序启动成功");
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "App", "启动应用程序时发生异常", ex);
                Shutdown();
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

            // 显示冲突警告弹窗（使用 Modern UI 风格）
            ShowModernConflictDialog(config);

            Logger.Warn(GetType().FullName ?? "App", "检测到多实例运行，显示冲突弹窗");

            // 根据配置决定是否自动关闭应用程序
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
                // 使用 iNKORE.Modern 的 MessageBox.Show API
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

                // 回退到标准 MessageBox
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
            // 延迟 1 秒后显示，确保主窗口已创建
            Task.Delay(1000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // 获取当前活动窗口作为 Owner
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
                        // 回退到标准 MessageBox
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
        /// 打开调试测试窗口（DebugTest）
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

                // 1. 停止所有服务
                _scheduleManager?.Stop();
                _timeCalibrationService?.Stop();

                // 2. 清理托盘图标
                _trayIconService?.Dispose();

                // 3. 释放互斥锁
                _mutexManager?.Release();

                // 4. 启动新进程
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                }

                // 5. 延迟后退出
                await Task.Delay(1500);
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

                // 清理新服务
                _scheduleManager?.Stop();
                _timeCalibrationService?.Stop();

                // 清理托盘图标
                _trayIconService?.Dispose();

                // 释放互斥锁
                _mutexManager?.Release();

                // 释放日志服务（确保缓冲区刷新）
                SerilogLogService.Instance?.Dispose();

                // 取消事件订阅
                if (_trayIconService != null)
                {
                    _trayIconService.OpenSettingRequested -= OpenSetting;
                    _trayIconService.OpenDebugRequested -= OpenDebugTest;
                    _trayIconService.AboutRequested -= OpenMainWindow;
                    _trayIconService.ExitRequested -= ExitApplication;
                }

                if (_mutexManager != null)
                {
                    _mutexManager.OnConflictDetected -= OnMutexConflictDetected;
                    _mutexManager.OnMutexAcquired -= OnMutexAcquired;
                }

                // 退出应用
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
            // 清理新服务
            _scheduleManager?.Stop();
            _timeCalibrationService?.Stop();

            // 释放托盘图标
            _trayIconService?.Dispose();

            // 释放互斥锁
            _mutexManager?.Release();

            // 取消事件订阅
            if (_trayIconService != null)
            {
                _trayIconService.OpenSettingRequested -= OpenSetting;
                _trayIconService.OpenDebugRequested -= OpenDebugTest;
                _trayIconService.AboutRequested -= OpenMainWindow;
                _trayIconService.ExitRequested -= ExitApplication;
            }

            if (_mutexManager != null)
            {
                _mutexManager.OnConflictDetected -= OnMutexConflictDetected;
                _mutexManager.OnMutexAcquired -= OnMutexAcquired;
            }

            Logger.Info(GetType().FullName ?? "App", "应用程序退出");

            // 释放日志服务（确保缓冲区刷新）
            SerilogLogService.Instance?.Dispose();

            base.OnExit(e);
        }
    }
}