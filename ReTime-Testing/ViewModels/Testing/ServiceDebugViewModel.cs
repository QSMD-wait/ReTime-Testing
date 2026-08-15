using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using System;
using System.Windows.Threading;

namespace ReTime_Testing.ViewModels.Testing
{
    /// <summary>
    /// 服务接口调试 ViewModel
    /// 职责：互斥锁、窗口位置、时间校准、执行计划、调度等接口调试
    /// </summary>
    public partial class ServiceDebugViewModel : ObservableObject
    {
        private const string LOG_TAG = "ServiceDebugViewModel";

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
            IMutexManager mutexManager,
            ISettingsService settingsService,
            IDesktopWindowManager desktopWindowManager,
            ITimeService? timeService = null,
            IScheduleManager? scheduleManager = null)
        {
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
                Logger.Error(LOG_TAG, "刷新调试信息时发生异常", ex);
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
                Logger.Info(LOG_TAG, $"进度条位置已切换到{name}");
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_TAG, $"切换进度条位置到{name}时发生异常", ex);
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
                Logger.Info(LOG_TAG, $"时间已校准{seconds} 秒: {newTime:HH:mm:ss}");
                RefreshStatus();
            }
            else
            {
                Logger.Warn(LOG_TAG, "时间服务未初始化");
            }
        }

        // ==================== 执行计划调试 ====================

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

        // ==================== 调度管理器调试 ====================

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
    }
}