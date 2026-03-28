using System;
using System.Configuration;
using System.Data;
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

        // 新增：时间服务字段（改为 internal 便于调试）
        internal ITimeService? _timeService;
        internal ScheduleManager? _scheduleManager;
        internal CloudCalibrationService? _cloudCalibrationService;

        // 公共属性用于访问服务
        public ITimeService? TimeService => _timeService;
        public ScheduleManager? ScheduleManager => _scheduleManager;
        public CloudCalibrationService? CloudCalibrationService => _cloudCalibrationService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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

        /// <summary>
        /// 尝试从云端获取时间
        /// </summary>
        /// <param name="timeout">超时时间</param>
        /// <returns>云端时间（如果成功），否则返回null</returns>
        private async Task<DateTime?> TryGetCloudTimeAsync(TimeSpan timeout)
        {
            try
            {
                // 使用已配置的 _cloudCalibrationService 实例
                if (_cloudCalibrationService == null)
                {
                    Logger.Warn(GetType().FullName ?? "App", "云端校准服务未初始化");
                    return null;
                }

                // 尝试手动触发校准
                var success = await _cloudCalibrationService.CalibrateAsync();

                if (success)
                {
                    // 返回当前时间（已校准）
                    return _timeService?.GetCurrentTime();
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                Logger.Warn(GetType().FullName ?? "App", "云端时间获取超时");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Warn(GetType().FullName ?? "App", $"云端时间获取失败: {ex.Message}");
                return null;
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

                // 初始化时间计划管理器
                var scheduleManager = Services.TimeScheduleManager.Instance;
                scheduleManager.Initialize();

                // ===== 新增：初始化时间服务 =====
                // 1. 初始化绝对时间服务
                _timeService = new AbsoluteTimeService();
                Logger.Info(GetType().FullName ?? "App", "时间服务已初始化");

                // 2. 初始化云端校准服务（统一使用一个实例）
                var timeTopSetting = configManager.LoadTimeTopSetting();
                _cloudCalibrationService = new CloudCalibrationService(_timeService);
                _cloudCalibrationService.Configure(
                    enabled: timeTopSetting.TimeSettings.Calibration.Enabled,
                    interval: timeTopSetting.TimeSettings.Calibration.IntervalSeconds,
                    timeout: timeTopSetting.TimeSettings.Calibration.TimeoutSeconds,
                    maxRetryCount: timeTopSetting.TimeSettings.Calibration.MaxRetryCount,
                    backoffMultiplier: timeTopSetting.TimeSettings.Calibration.BackoffMultiplier,
                    triggerThreshold: timeTopSetting.TimeSettings.Threshold.CalibrationTriggerSeconds
                );
                Logger.Info(GetType().FullName ?? "App", "云端校准服务已配置");

                // 3. 尝试从云端获取时间（3秒超时）
                try
                {
                    var cloudTime = await TryGetCloudTimeAsync(TimeSpan.FromSeconds(3));

                    if (cloudTime.HasValue)
                    {
                        _timeService.Calibrate(cloudTime.Value);
                        Logger.Info(GetType().FullName ?? "App", $"时间已从云端同步: {cloudTime.Value:yyyy-MM-dd HH:mm:ss}");
                    }
                    else
                    {
                        Logger.Warn(GetType().FullName ?? "App", "云端时间获取失败，使用系统时间");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(GetType().FullName ?? "App", $"云端时间获取异常: {ex.Message}，使用系统时间");
                }

                // 4. 初始化执行计划生成器
                var planGenerator = new ExecutionPlanGenerator();
                Logger.Info(GetType().FullName ?? "App", "执行计划生成器已初始化");

                // 5. 生成执行计划
                var selectedSchedule = scheduleManager.LoadSchedule(configManager.LoadTimeTopSetting().SelectedScheduleId);
                if (selectedSchedule == null)
                {
                    selectedSchedule = scheduleManager.LoadSchedule("Default");
                }

                if (selectedSchedule != null)
                {
                    var currentTime = _timeService.GetCurrentTime();
                    var executionPlan = planGenerator.Generate(selectedSchedule, DateTime.Today, currentTime);
                    Logger.Info(GetType().FullName ?? "App", $"执行计划已生成: {executionPlan}");

                    // 6. 初始化调度管理器
                    _scheduleManager = new ScheduleManager(_timeService, GlobalTimeTopDesktopService.Instance.StateManager);
                    _scheduleManager.Initialize(executionPlan);
                    Logger.Info(GetType().FullName ?? "App", "调度管理器已启动");

                    // 7. 启动云端校准服务（长期运行）
                    _cloudCalibrationService.Start();
                    Logger.Info(GetType().FullName ?? "App", "云端校准服务已启动");
                }

                // 注意：不再调用 GlobalTimeTopDesktopService.Instance.InitializeAndApplySchedule()
                // 因为现在使用新的 ScheduleManager 进行调度，避免双重调度冲突
                // GlobalTimeTopDesktopService 保留状态管理功能（SetLoading, SetProgress 等）

                // 初始化全局服务
                var service = GlobalTimeTopDesktopService.Instance;

                // 初始化系统托盘图标服务
                _trayIconService = TrayIconService.Instance;
                _trayIconService.Initialize();

                // 订阅托盘图标事件
                _trayIconService.OpenSettingRequested += OpenSetting;
                _trayIconService.OpenDebugRequested += OpenDebugTest;
                _trayIconService.AboutRequested += OpenMainWindow;
                _trayIconService.ExitRequested += ExitApplication;

                // 使用 WindowManager 打开主窗口和调试测试窗口
                // WindowManager.ShowMainWindow();
                // WindowManager.ShowDebugTest();

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
        /// 退出应用程序
        /// </summary>
        private void ExitApplication()
        {
            try
            {
                Logger.Info(GetType().FullName ?? "App", "应用程序退出请求");

                // 清理新服务
                _scheduleManager?.Stop();
                _cloudCalibrationService?.Stop();

                // 清理托盘图标
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
            _cloudCalibrationService?.Stop();

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

            base.OnExit(e);
        }
    }
}
