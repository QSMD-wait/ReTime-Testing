using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Models.UI;
using ReTime_Testing.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.ViewModels
{
    /// <summary>
    /// 调试测试窗口 ViewModel
    /// 职责：运行时调试工具（状态控制、互斥锁、位置、API测试、校准、调度、Toast等）
    /// </summary>
    public partial class DebugTestViewModel : ObservableObject
    {
        private static readonly List<int> _hours = Enumerable.Range(0, 24).ToList();
        private static readonly List<int> _minutes = Enumerable.Range(0, 60).ToList();

        private const string LOG_TAG = "DebugTestViewModel";

        private readonly IGlobalTimeTopDesktopService _service;
        private readonly IMutexManager _mutexManager;
        private readonly ISettingsService _settingsService;
        private readonly IDesktopWindowManager _desktopWindowManager;
        private readonly ITimeService? _timeService;
        private readonly IScheduleManager? _scheduleManager;
        private readonly ITimeCalibrationService? _timeCalibrationService;
        private System.Windows.Threading.DispatcherTimer? _refreshTimer;

        // ==================== 状态控制属性 ====================

        [ObservableProperty]
        private double _progressValue = 50;

        [ObservableProperty]
        private int _startHour = 9;

        [ObservableProperty]
        private int _startMinute = 0;

        [ObservableProperty]
        private int _endHour = 17;

        [ObservableProperty]
        private int _endMinute = 0;

        public List<int> Hours => _hours;
        public List<int> Minutes => _minutes;

        [ObservableProperty]
        private double _scheduleProgress = 0;

        [ObservableProperty]
        private string _scheduleStatus = "未开始";

        [ObservableProperty]
        private bool _isStateControlsEnabled = true;

        [ObservableProperty]
        private bool _isScheduleRunning = false;

        // ==================== 互斥锁调试属性 ====================

        [ObservableProperty]
        private bool _isMutexAcquired = false;

        [ObservableProperty]
        private string _mutexId = string.Empty;

        [ObservableProperty]
        private bool _isMutexEnabled = true;

        [ObservableProperty]
        private string _mutexStatus = "未知";

        // ==================== 进度条位置属性 ====================

        [ObservableProperty]
        private ProgressBarPosition _currentPosition = ProgressBarPosition.Top;

        [ObservableProperty]
        private string _positionText = "顶部";

        // ==================== 时间服务调试属性 ====================

        [ObservableProperty]
        private string _currentTime = "未知";

        [ObservableProperty]
        private bool _isCloudSynchronized = false;

        // ==================== 执行计划调试属性 ====================

        [ObservableProperty]
        private string _currentSegmentName = "未知";

        [ObservableProperty]
        private string _currentState = "未知";

        [ObservableProperty]
        private string _nextTimePoint = "无";

        [ObservableProperty]
        private bool _isActiveSegment = false;

        // ==================== 调度管理器调试属性 ====================

        [ObservableProperty]
        private bool _isScheduleManagerRunning = false;

        [ObservableProperty]
        private string _pollingInterval = "1秒";

        [ObservableProperty]
        private int _timePointCount = 0;

        // ==================== 云端校准服务调试属性 ====================

        [ObservableProperty]
        private bool _isCloudCalibrationRunning = false;

        [ObservableProperty]
        private int _calibrationFailureCount = 0;

        [ObservableProperty]
        private string _currentCalibrationInterval = "5分钟";

        [ObservableProperty]
        private string _lastCalibrationTime = "未校准";

        [ObservableProperty]
        private List<string> _ntpServers = NtpServerDefaults.Servers.ToList();

        [ObservableProperty]
        private int _selectedNtpServerIndex = 0;

        [ObservableProperty]
        private string _currentProviderName = "未初始化";

        [ObservableProperty]
        private double _lastRttMs = 0;

        // ==================== Toast 通知测试属性 ====================

        [ObservableProperty]
        private int _selectedToastSeverityIndex = 0;

        [ObservableProperty]
        private string _toastTitle = "测试标题";

        [ObservableProperty]
        private string _toastMessage = "这是一条测试 Toast 通知消息";

        [ObservableProperty]
        private double _toastDurationSeconds = 5;

        [ObservableProperty]
        private bool _toastAutoClose = true;

        [ObservableProperty]
        private bool _toastCanUserClose = true;

        public List<string> ToastSeverityNames { get; } = Enum.GetNames<ToastSeverity>().ToList();

        public event Action<ToastMessage>? ToastRequested;

        // ==================== 构造函数 ====================

        public DebugTestViewModel(
            IGlobalTimeTopDesktopService globalService,
            IMutexManager mutexManager,
            ISettingsService settingsService,
            IDesktopWindowManager desktopWindowManager,
            ITimeService? timeService = null,
            IScheduleManager? scheduleManager = null,
            ITimeCalibrationService? timeCalibrationService = null)
        {
            _service = globalService;
            _mutexManager = mutexManager;
            _settingsService = settingsService;
            _desktopWindowManager = desktopWindowManager;
            _timeService = timeService;
            _scheduleManager = scheduleManager;
            _timeCalibrationService = timeCalibrationService;

            UpdateMutexStatus();
            UpdatePositionStatus();

            _refreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += OnRefreshTimerTick;
            _refreshTimer.Start();

            RefreshDebugInfo();
        }

        // ==================== 调试信息刷新 ====================

        private void OnRefreshTimerTick(object? sender, EventArgs e)
        {
            RefreshDebugInfo();
        }

        private void RefreshDebugInfo()
        {
            try
            {
                if (_timeService != null)
                {
                    CurrentTime = _timeService.GetCurrentTime().ToString("HH:mm:ss");
                    IsCloudSynchronized = _timeService.IsCloudSynchronized;
                }
                else
                {
                    CurrentTime = "未初始化";
                    IsCloudSynchronized = false;
                }

                if (_scheduleManager != null)
                {
                    IsScheduleManagerRunning = _scheduleManager.IsRunning;
                    var plan = _scheduleManager.CurrentPlan;
                    if (plan != null)
                    {
                        TimePointCount = plan.TimePoints.Count;
                        CurrentSegmentName = plan.CurrentSegment?.Name ?? "未知";
                        CurrentState = plan.CurrentSegment?.State.ToString() ?? "未知";
                        NextTimePoint = plan.NextTimePoint?.Name ?? "无";
                        IsActiveSegment = plan.CurrentSegment?.IsActive ?? false;
                    }
                    else
                    {
                        CurrentSegmentName = "未加载";
                        CurrentState = "未加载";
                        NextTimePoint = "未加载";
                        IsActiveSegment = false;
                    }
                }
                else
                {
                    IsScheduleManagerRunning = false;
                    CurrentSegmentName = "未初始化";
                    CurrentState = "未初始化";
                    NextTimePoint = "未初始化";
                    IsActiveSegment = false;
                }

                if (_timeCalibrationService != null)
                {
                    IsCloudCalibrationRunning = _timeCalibrationService.IsRunning;
                    CalibrationFailureCount = _timeCalibrationService.FailureCount;
                    CurrentCalibrationInterval = $"{_timeCalibrationService.CurrentInterval}秒";
                    LastCalibrationTime = _timeCalibrationService.LastCalibrationTime > DateTime.MinValue
                        ? _timeCalibrationService.LastCalibrationTime.ToString("HH:mm:ss")
                        : "未校准";
                    CurrentProviderName = _timeCalibrationService.CurrentProviderName;
                    LastRttMs = _timeCalibrationService.LastRttMs;
                }
                else
                {
                    IsCloudCalibrationRunning = false;
                    CalibrationFailureCount = 0;
                    CurrentCalibrationInterval = "未知";
                    LastCalibrationTime = "未初始化";
                    CurrentProviderName = "未初始化";
                    LastRttMs = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "刷新调试信息时发生异常", ex);
            }
        }

        // ==================== 资源清理 ====================

        public void Cleanup()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= OnRefreshTimerTick;
            }
        }

        // ==================== 进度值变更 ====================

        partial void OnProgressValueChanged(double value)
        {
            _service.SetValue(value);
        }

        // ==================== 状态控制命令 ====================

        [RelayCommand]
        private void SetLoading() => _service.SetLoading();

        [RelayCommand]
        private void SetSuccess() => _service.SetSuccess();

        [RelayCommand]
        private void SetError() => _service.SetError();

        [RelayCommand]
        private void SetPaused() => _service.SetPaused();

        [RelayCommand]
        private void SetProgress() => _service.SetProgress(ProgressValue);

        [RelayCommand]
        private void SetHidden() => _service.SetHidden();

        [RelayCommand]
        private void SetDisabled() => _service.SetDisabled();

        // ==================== 可见性控制命令 ====================

        [RelayCommand]
        private void SetVisibilityVisible() => _service.SetVisibility(Visibility.Visible);

        [RelayCommand]
        private void SetVisibilityHidden() => _service.SetVisibility(Visibility.Hidden);

        [RelayCommand]
        private void SetVisibilityCollapsed() => _service.SetVisibility(Visibility.Collapsed);

        // ==================== 启用状态控制命令 ====================

        [RelayCommand]
        private void SetEnabledTrue() => _service.SetEnabled(true);

        [RelayCommand]
        private void SetEnabledFalse() => _service.SetEnabled(false);

        // ==================== 透明度控制命令 ====================

        [RelayCommand]
        private void SetOpacityFull() => _service.SetOpacity(1.0);

        [RelayCommand]
        private void SetOpacityHalf() => _service.SetOpacity(0.5);

        [RelayCommand]
        private void SetOpacityLow() => _service.SetOpacity(0.2);

        // ==================== 前景色控制命令 ====================

        [RelayCommand]
        private void SetForegroundBlue() => _service.SetForeground(ProgressColors.DefaultBlue);

        [RelayCommand]
        private void SetForegroundGreen() => _service.SetForeground(ProgressColors.SuccessGreen);

        [RelayCommand]
        private void SetForegroundRed() => _service.SetForeground(ProgressColors.ErrorRed);

        [RelayCommand]
        private void SetForegroundOrange() => _service.SetForeground(ProgressColors.PauseOrange);

        [RelayCommand]
        private void SetForegroundGray() => _service.SetForeground(ProgressColors.Gray);

        // ==================== 背景色控制命令 ====================

        [RelayCommand]
        private void SetBackgroundTransparent() => _service.SetBackground(Brushes.Transparent);

        [RelayCommand]
        private void SetBackgroundLightGray() => _service.SetBackground(Brushes.LightGray);

        [RelayCommand]
        private void SetBackgroundWhite() => _service.SetBackground(Brushes.White);

        // ==================== 范围控制命令 ====================

        [RelayCommand]
        private void SetRange0100() => _service.SetRange(0, 100);

        [RelayCommand]
        private void SetRange01() => _service.SetRange(0, 1);

        // ==================== 重置与批量更新 ====================

        [RelayCommand]
        private void ResetState() => _service.Reset();

        [RelayCommand]
        private void BatchUpdateTest()
        {
            _service.BatchUpdate(svc =>
            {
                svc.SetProgress(75);
                svc.SetForeground(ProgressColors.SuccessGreen);
                svc.SetOpacity(0.8);
                svc.SetVisibility(Visibility.Visible);
            });
        }

        // ==================== 互斥锁调试命令 ====================

        private void UpdateMutexStatus()
        {
            IsMutexAcquired = _mutexManager.IsAcquired;
            MutexId = _mutexManager.Config.MutexId;
            IsMutexEnabled = _mutexManager.Config.IsEnabled;
            MutexStatus = IsMutexAcquired ? "已获取" : "未获取";
        }

        [RelayCommand]
        private void ReleaseMutex()
        {
            try
            {
                _mutexManager.Release();
                UpdateMutexStatus();
                Logger.Info(LOG_TAG, "互斥锁已释放");
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "释放互斥锁时发生异常", ex);
            }
        }

        [RelayCommand]
        private void ReacquireMutex()
        {
            try
            {
                bool acquired = _mutexManager.TryAcquire();
                UpdateMutexStatus();
                if (acquired)
                    Logger.Info(LOG_TAG, "互斥锁重新获取成功");
                else
                    Logger.Warn(LOG_TAG, "互斥锁重新获取失败");
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "重新获取互斥锁时发生异常", ex);
            }
        }

        [RelayCommand]
        private void ToggleMutexEnabled()
        {
            try
            {
                var config = _mutexManager.Config;
                config.IsEnabled = !config.IsEnabled;
                IsMutexEnabled = config.IsEnabled;
                Logger.Info(LOG_TAG, $"互斥锁已{(config.IsEnabled ? "启用" : "禁用")}");
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "切换互斥锁启用状态时发生异常", ex);
            }
        }

        // ==================== 进度条位置控制命令 ====================

        private void UpdatePositionStatus()
        {
            CurrentPosition = _desktopWindowManager.CurrentPosition;
            PositionText = GetPositionText(CurrentPosition);
        }

        private static string GetPositionText(ProgressBarPosition position)
        {
            return position switch
            {
                ProgressBarPosition.Top => "顶部",
                ProgressBarPosition.Bottom => "底部",
                ProgressBarPosition.Left => "左侧",
                ProgressBarPosition.Right => "右侧",
                _ => "未知"
            };
        }

        [RelayCommand]
        private void SetPositionTop()
        {
            try
            {
                _desktopWindowManager.SetPosition(ProgressBarPosition.Top);
                SavePositionConfig(ProgressBarPosition.Top);
                UpdatePositionStatus();
                Logger.Info(LOG_TAG, "进度条位置已切换到顶部");
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "切换进度条位置到顶部时发生异常", ex);
            }
        }

        [RelayCommand]
        private void SetPositionBottom()
        {
            try
            {
                _desktopWindowManager.SetPosition(ProgressBarPosition.Bottom);
                SavePositionConfig(ProgressBarPosition.Bottom);
                UpdatePositionStatus();
                Logger.Info(LOG_TAG, "进度条位置已切换到底部");
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "切换进度条位置到底部时发生异常", ex);
            }
        }

        [RelayCommand]
        private void SetPositionLeft()
        {
            try
            {
                _desktopWindowManager.SetPosition(ProgressBarPosition.Left);
                SavePositionConfig(ProgressBarPosition.Left);
                UpdatePositionStatus();
                Logger.Info(LOG_TAG, "进度条位置已切换到左侧");
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "切换进度条位置到左侧时发生异常", ex);
            }
        }

        [RelayCommand]
        private void SetPositionRight()
        {
            try
            {
                _desktopWindowManager.SetPosition(ProgressBarPosition.Right);
                SavePositionConfig(ProgressBarPosition.Right);
                UpdatePositionStatus();
                Logger.Info(LOG_TAG, "进度条位置已切换到右侧");
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "切换进度条位置到右侧时发生异常", ex);
            }
        }

        private void SavePositionConfig(ProgressBarPosition position)
        {
            var setting = _settingsService.GetTimeTopSetting();
            setting.ProgressBar.Position = position switch
            {
                ProgressBarPosition.Bottom => "bottom",
                ProgressBarPosition.Left => "left",
                ProgressBarPosition.Right => "right",
                _ => "top"
            };
            _settingsService.SaveTimeTopSetting(setting);
        }

        // ==================== 时间服务调试命令 ====================

        [RelayCommand]
        private void CalibrateTimeForward()
        {
            if (_timeService != null)
            {
                var newTime = _timeService.GetCurrentTime().AddSeconds(5);
                _timeService.Calibrate(newTime);
                Logger.Info(LOG_TAG, $"时间已校准向前 5 秒: {newTime:HH:mm:ss}");
                RefreshDebugInfo();
            }
        }

        [RelayCommand]
        private void CalibrateTimeBackward()
        {
            if (_timeService != null)
            {
                var newTime = _timeService.GetCurrentTime().AddSeconds(-5);
                _timeService.Calibrate(newTime);
                Logger.Info(LOG_TAG, $"时间已校准向后 5 秒: {newTime:HH:mm:ss}");
                RefreshDebugInfo();
            }
        }

        // ==================== 执行计划调试命令 ====================

        [RelayCommand]
        private void ShowExecutionPlan()
        {
            Logger.Info(LOG_TAG, "=== 执行计划详情 ===");
            Logger.Info(LOG_TAG, $"时间点数量: {TimePointCount}");
            Logger.Info(LOG_TAG, $"当前时间段: {CurrentSegmentName}");
            Logger.Info(LOG_TAG, $"当前状态: {CurrentState}");
            Logger.Info(LOG_TAG, $"下个时间点: {NextTimePoint}");
            Logger.Info(LOG_TAG, "====================");
        }

        [RelayCommand]
        private void ApplyCurrentState()
        {
            if (_scheduleManager != null)
            {
                _scheduleManager.ApplyCurrentState();
                Logger.Info(LOG_TAG, "当前状态已重新应用");
            }
        }

        // ==================== 调度管理器调试命令 ====================

        [RelayCommand]
        private void StopScheduleManager()
        {
            if (_scheduleManager != null)
            {
                _scheduleManager.Stop();
                IsScheduleManagerRunning = false;
                Logger.Info(LOG_TAG, "调度管理器已停止");
            }
        }

        [RelayCommand]
        private void RestartScheduleManager()
        {
            if (_scheduleManager != null)
            {
                try
                {
                    var currentPlan = _scheduleManager.CurrentPlan;
                    _scheduleManager.Stop();

                    if (currentPlan != null)
                    {
                        _scheduleManager.Initialize(currentPlan);
                        Logger.Info(LOG_TAG, "调度管理器已重启，执行计划已重新应用");
                    }
                    else
                    {
                        Logger.Warn(LOG_TAG, "无法重启调度管理器：执行计划为空");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(LOG_TAG, "重启调度管理器时发生异常", ex);
                }
            }
        }

        // ==================== 云端校准服务调试命令 ====================

        [RelayCommand]
        private async Task TriggerManualCalibration()
        {
            try
            {
                if (_timeCalibrationService != null)
                {
                    var success = await _timeCalibrationService.CalibrateAsync();
                    LastCalibrationTime = DateTime.Now.ToString("HH:mm:ss");
                    Logger.Info(LOG_TAG, $"手动校准{(success ? "成功" : "失败")}");
                    RefreshDebugInfo();
                }
                else
                {
                    Logger.Warn(LOG_TAG, "云端校准服务未初始化");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "手动触发校准时发生异常", ex);
            }
        }

        [RelayCommand]
        private void ResetCalibrationFailures()
        {
            if (_timeCalibrationService != null)
            {
                _timeCalibrationService.Reset();
                CalibrationFailureCount = 0;
                Logger.Info(LOG_TAG, "校准失败计数已重置");
                RefreshDebugInfo();
            }
        }

        [RelayCommand]
        private void ApplyTimeSourceConfig()
        {
            try
            {
                if (_timeCalibrationService != null)
                {
                    var selectedServer = NtpServerDefaults.Servers[SelectedNtpServerIndex];
                    var timeTopSetting = _settingsService.GetTimeTopSetting();
                    timeTopSetting.Calibration.Cloud.SelectedServerAddress = selectedServer;
                    _settingsService.SaveTimeTopSetting(timeTopSetting);
                    _timeCalibrationService.ApplyConfig(timeTopSetting.Calibration);
                    Logger.Info(LOG_TAG, $"NTP服务器配置已应用: 服务器={selectedServer}");
                    RefreshDebugInfo();
                }
                else
                {
                    Logger.Warn(LOG_TAG, "时间校准服务未初始化");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, "应用NTP服务器配置时发生异常", ex);
            }
        }

        // ==================== 样式优先级测试命令 ====================

        [RelayCommand]
        private void ApplyDefaultStyle()
        {
            _service.SetForeground(ProgressColors.DefaultBlue);
            _service.SetOpacity(1.0);
            _service.SetVisibility(Visibility.Visible);
            Logger.Info(LOG_TAG, "已应用默认样式");
        }

        [RelayCommand]
        private void ApplyConfigStyle()
        {
            _service.SetForeground(new SolidColorBrush(Color.FromRgb(0x2D, 0x7D, 0x9A)));
            _service.SetOpacity(0.9);
            Logger.Info(LOG_TAG, "已应用配置文件样式");
        }

        [RelayCommand]
        private void ApplyScheduleStyle()
        {
            _service.SetForeground(new SolidColorBrush(Color.FromRgb(0xFF, 0x57, 0x33)));
            _service.SetOpacity(1.0);
            Logger.Info(LOG_TAG, "已应用时间表样式");
        }

        // ==================== 综合测试命令 ====================

        [RelayCommand]
        private void RunComprehensiveTest()
        {
            Logger.Info(LOG_TAG, "=== 开始综合测试 ===");

            if (_timeService != null)
            {
                var time = _timeService.GetCurrentTime();
                Logger.Info(LOG_TAG, $"[测试 1] 时间服务: {time:HH:mm:ss}, 云端同步: {_timeService.IsCloudSynchronized}");
            }

            _service.SetLoading();
            Logger.Info(LOG_TAG, "[测试 2] 状态切换: Loading");

            _service.SetProgress(50);
            Logger.Info(LOG_TAG, "[测试 2] 状态切换: Progress (50%)");

            _service.SetSuccess();
            Logger.Info(LOG_TAG, "[测试 2] 状态切换: Success");

            ApplyDefaultStyle();
            Logger.Info(LOG_TAG, "[测试 3] 默认样式已应用");

            ApplyConfigStyle();
            Logger.Info(LOG_TAG, "[测试 3] 配置文件样式已应用");

            if (_timeService != null)
            {
                var oldTime = _timeService.GetCurrentTime();
                _timeService.Calibrate(oldTime.AddSeconds(10));
                var newTime = _timeService.GetCurrentTime();
                Logger.Info(LOG_TAG, $"[测试 4] 时间跳跃: {oldTime:HH:mm:ss} → {newTime:HH:mm:ss}");
            }

            Logger.Info(LOG_TAG, "=== 综合测试完成 ===");
        }

        [RelayCommand]
        private void LogAllServicesStatus()
        {
            Logger.Info(LOG_TAG, "开始记录所有服务的当前状态...");
            Logger.Info(LOG_TAG, $"主服务: {(_service != null ? "正常" : "未初始化")}");
            Logger.Info(LOG_TAG, $"时间服务: {(_timeService != null ? "正常" : "未初始化")}");
            Logger.Info(LOG_TAG, $"调度管理器: {(_scheduleManager != null ? "正常" : "未初始化")}");
            Logger.Info(LOG_TAG, $"云校准服务: {(_timeCalibrationService != null ? "正常" : "未初始化")}");
            Logger.Info(LOG_TAG, $"当前时间: {CurrentTime}, 云同步: {IsCloudSynchronized}");
            Logger.Info(LOG_TAG, $"当前状态: {CurrentState}, 活动段: {IsActiveSegment}, 下个时间点: {NextTimePoint}");
            Logger.Info(LOG_TAG, "服务状态记录完毕。");
        }

        [RelayCommand]
        private void ShowTestReport()
        {
            Logger.Info(LOG_TAG, "=== 测试报告 ===");
            Logger.Info(LOG_TAG, "时间服务状态:");
            Logger.Info(LOG_TAG, $"  - 当前时间: {CurrentTime}");
            Logger.Info(LOG_TAG, $"  - 云端同步: {(IsCloudSynchronized ? "是" : "否")}");
            Logger.Info(LOG_TAG, "执行计划状态:");
            Logger.Info(LOG_TAG, $"  - 当前状态: {CurrentState}");
            Logger.Info(LOG_TAG, $"  - 是否活跃段: {IsActiveSegment}");
            Logger.Info(LOG_TAG, $"  - 下个时间点: {NextTimePoint}");
            Logger.Info(LOG_TAG, "调度管理器状态:");
            Logger.Info(LOG_TAG, $"  - 状态: {(_scheduleManager?.IsRunning == true ? "运行中" : "已停止")}");
            Logger.Info(LOG_TAG, "云校准服务状态:");
            if (_timeCalibrationService != null)
            {
                Logger.Info(LOG_TAG, $"  - 上次校准: {_timeCalibrationService.LastCalibrationTime:yyyy-MM-dd HH:mm:ss}");
                Logger.Info(LOG_TAG, $"  - 失败次数: {_timeCalibrationService.FailureCount}");
                Logger.Info(LOG_TAG, $"  - 当前间隔: {_timeCalibrationService.CurrentInterval}秒");
            }
            else
            {
                Logger.Info(LOG_TAG, "  - 未初始化");
            }
            Logger.Info(LOG_TAG, "=== 测试报告结束 ===");
        }

        // ==================== Toast 通知测试命令 ====================

        private ToastSeverity GetSelectedSeverity()
        {
            return (ToastSeverity)SelectedToastSeverityIndex;
        }

        [RelayCommand]
        private void ShowCustomToast()
        {
            var message = new ToastMessage(ToastTitle, ToastMessage)
            {
                Severity = GetSelectedSeverity(),
                Duration = TimeSpan.FromSeconds(ToastDurationSeconds),
                AutoClose = ToastAutoClose,
                CanUserClose = ToastCanUserClose
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowInfoToast()
        {
            var message = new ToastMessage("信息通知", "这是一条信息级别的 Toast 通知")
            {
                Severity = ToastSeverity.Informational
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowSuccessToastTest()
        {
            var message = new ToastMessage("操作成功", "任务已成功完成！")
            {
                Severity = ToastSeverity.Success
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowWarningToastTest()
        {
            var message = new ToastMessage("警告", "检测到潜在问题，请注意检查")
            {
                Severity = ToastSeverity.Warning,
                Duration = TimeSpan.FromSeconds(7)
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowErrorToastTest()
        {
            var message = new ToastMessage("错误", "操作执行失败，请重试或联系管理员")
            {
                Severity = ToastSeverity.Error,
                Duration = TimeSpan.FromSeconds(10)
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowNonClosableToast()
        {
            var message = new ToastMessage("不可关闭", "此 Toast 不会自动关闭，只能通过代码关闭")
            {
                Severity = ToastSeverity.Warning,
                AutoClose = false,
                CanUserClose = false,
                Duration = TimeSpan.MaxValue
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowBurstToast()
        {
            var severities = new[] { ToastSeverity.Informational, ToastSeverity.Success, ToastSeverity.Warning, ToastSeverity.Error };
            for (int i = 0; i < 4; i++)
            {
                var message = new ToastMessage($"批量通知 #{i + 1}", $"这是第 {i + 1} 条批量 Toast")
                {
                    Severity = severities[i],
                    Duration = TimeSpan.FromSeconds(3 + i)
                };
                ToastRequested?.Invoke(message);
            }
        }

        [RelayCommand]
        private void ShowActionToast()
        {
            var message = new ToastMessage("更新可用", "新版本 v2.0 已发布，包含多项改进")
            {
                Severity = ToastSeverity.Informational,
                ActionContent = new System.Windows.Controls.Button
                {
                    Content = "查看详情",
                    Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
                    {
                        Logger.Info("ToastTest", "用户点击了 Toast 操作按钮：查看详情");
                    })
                }
            };
            ToastRequested?.Invoke(message);
        }

        [RelayCommand]
        private void ShowErrorActionToast()
        {
            var message = new ToastMessage("保存失败", "文件被占用，无法写入配置")
            {
                Severity = ToastSeverity.Error,
                Duration = TimeSpan.FromSeconds(10),
                ActionContent = new System.Windows.Controls.Button
                {
                    Content = "重试",
                    Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
                    {
                        Logger.Info("ToastTest", "用户点击了重试按钮");
                    })
                }
            };
            ToastRequested?.Invoke(message);
        }
    }
}