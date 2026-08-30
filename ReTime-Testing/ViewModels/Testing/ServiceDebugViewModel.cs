using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using System;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.ViewModels.Testing
{
    /// <summary>
    /// 服务接口调试 ViewModel
    /// 职责：互斥锁、窗口位置、时间校准、执行计划、调度等接口调试
    /// </summary>
    public partial class ServiceDebugViewModel : ObservableObject
    {
        private readonly ILogger<ServiceDebugViewModel> _logger;
        private readonly IMutexManager _mutexManager;
        private readonly ISettingsService _settingsService;
        private readonly IDesktopWindowManager _desktopWindowManager;
        private readonly ITimeService? _timeService;
        private readonly IScheduleManager? _scheduleManager;
        private DispatcherTimer? _refreshTimer;

        public string TabTitle => "接口";

        // ==================== 互斥锁 ====================

        [ObservableProperty]
        private bool _isMutexAcquired;

        [ObservableProperty]
        private string _mutexId = string.Empty;

        [ObservableProperty]
        private bool _isMutexEnabled = true;

        [ObservableProperty]
        private string _mutexStatus = "未知";

        // ==================== 进度条位置 ====================

        [ObservableProperty]
        private ProgressBarPosition _currentPosition = ProgressBarPosition.Top;

        [ObservableProperty]
        private string _positionText = "顶部";

        // ==================== 时间服务 ====================

        [ObservableProperty]
        private string _currentTime = "未知";

        // ==================== 执行计划 ====================

        [ObservableProperty]
        private string _currentSegmentName = "未知";

        [ObservableProperty]
        private string _currentState = "未知";

        [ObservableProperty]
        private string _nextTimePoint = "无";

        [ObservableProperty]
        private bool _isActiveSegment;

        // ==================== 调度管理器 ====================

        [ObservableProperty]
        private bool _isScheduleManagerRunning;

        [ObservableProperty]
        private int _timePointCount;

        public ServiceDebugViewModel(
            ILogger<ServiceDebugViewModel> logger,
            IMutexManager mutexManager,
            ISettingsService settingsService,
            IDesktopWindowManager desktopWindowManager,
            ITimeService? timeService = null,
            IScheduleManager? scheduleManager = null)
        {
            _logger = logger;
            _mutexManager = mutexManager;
            _settingsService = settingsService;
            _desktopWindowManager = desktopWindowManager;
            _timeService = timeService;
            _scheduleManager = scheduleManager;

            UpdateMutexStatus();
            UpdatePositionStatus();

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += OnRefreshTimerTick;
            _refreshTimer.Start();

            RefreshStatus();
        }

        // ==================== 状态刷新 ====================

        private void OnRefreshTimerTick(object? sender, EventArgs e)
        {
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            try
            {
                if (_timeService != null)
                {
                    CurrentTime = _timeService.GetCurrentTime().ToString("HH:mm:ss");
                }
                else
                {
                    CurrentTime = "未初始化";
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新调试信息时发生异常");
            }
        }

        /// <summary>
        /// 资源清理
        /// </summary>
        public void Cleanup()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= OnRefreshTimerTick;
            }
        }

        // ==================== 互斥锁调试 ====================

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
                _logger.LogInformation("互斥锁已释放");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "释放互斥锁时发生异常");
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
                    _logger.LogInformation("互斥锁重新获取成功");
                else
                    _logger.LogWarning("互斥锁重新获取失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重新获取互斥锁时发生异常");
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
                _logger.LogInformation("互斥锁已{Status}", config.IsEnabled ? "启用" : "禁用");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换互斥锁启用状态时发生异常");
            }
        }

        // ==================== 进度条位置控制 ====================

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
            SetPosition(ProgressBarPosition.Top, "顶部");
        }

        [RelayCommand]
        private void SetPositionBottom()
        {
            SetPosition(ProgressBarPosition.Bottom, "底部");
        }

        [RelayCommand]
        private void SetPositionLeft()
        {
            SetPosition(ProgressBarPosition.Left, "左侧");
        }

        [RelayCommand]
        private void SetPositionRight()
        {
            SetPosition(ProgressBarPosition.Right, "右侧");
        }

        private void SetPosition(ProgressBarPosition position, string name)
        {
            try
            {
                _desktopWindowManager.SetPosition(position);
                SavePositionConfig(position);
                UpdatePositionStatus();
                _logger.LogInformation("进度条位置已切换到{Name}", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换进度条位置到{Name}时发生异常", name);
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

        // ==================== 时间服务调试 ====================

        [RelayCommand]
        private void CalibrateTimeForward()
        {
            CalibrateTime(5);
        }

        [RelayCommand]
        private void CalibrateTimeBackward()
        {
            CalibrateTime(-5);
        }

        private void CalibrateTime(int seconds)
        {
            if (_timeService != null)
            {
                var newTime = _timeService.GetCurrentTime().AddSeconds(seconds);
                _timeService.Calibrate(newTime);
                _logger.LogInformation("时间已校准{Seconds} 秒: {NewTime:HH:mm:ss}", seconds, newTime);
                RefreshStatus();
            }
            else
            {
                _logger.LogWarning("时间服务未初始化");
            }
        }

        // ==================== 执行计划调试 ====================

        [RelayCommand]
        private void ShowExecutionPlan()
        {
            _logger.LogInformation("=== 执行计划详情 ===");
            _logger.LogInformation("时间点数量: {TimePointCount}", TimePointCount);
            _logger.LogInformation("当前时间段: {SegmentName}", CurrentSegmentName);
            _logger.LogInformation("当前状态: {State}", CurrentState);
            _logger.LogInformation("下个时间点: {NextTimePoint}", NextTimePoint);
            _logger.LogInformation("====================");
        }

        [RelayCommand]
        private void ApplyCurrentState()
        {
            if (_scheduleManager != null)
            {
                _scheduleManager.ApplyCurrentState();
                _logger.LogInformation("当前状态已重新应用");
            }
        }

        // ==================== 调度管理器调试 ====================

        [RelayCommand]
        private void StopScheduleManager()
        {
            if (_scheduleManager != null)
            {
                _scheduleManager.Stop();
                IsScheduleManagerRunning = false;
                _logger.LogInformation("调度管理器已停止");
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
                        _logger.LogInformation("调度管理器已重启，执行计划已重新应用");
                    }
                    else
                    {
                        _logger.LogWarning("无法重启调度管理器：执行计划为空");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "重启调度管理器时发生异常");
                }
            }
        }
    }
}