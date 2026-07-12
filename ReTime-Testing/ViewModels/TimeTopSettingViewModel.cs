using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Models.UI;
using ReTime_Testing.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.ViewModels
{
    /// <summary>
    /// 基本设置页面 ViewModel
    /// </summary>
    public partial class BasicPageViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private GlobalSetting _setting;
        private bool _isInitializing = true;

        [ObservableProperty]
        private string _selectedTheme = "light";

        [ObservableProperty]
        private bool _isAutoStartEnabled;

        [ObservableProperty]
        private string _selectedAutoStartMethod = "registry";

        public BasicPageViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _setting = _settingsService.GetGlobalSetting();

            SelectedTheme = _setting.Basic.Theme;
            IsAutoStartEnabled = _setting.Basic.AutoStart.Enabled;
            SelectedAutoStartMethod = _setting.Basic.AutoStart.Method;

            _isInitializing = false;
        }

        partial void OnSelectedThemeChanged(string value)
        {
            if (_isInitializing) return;
            _setting.Basic.Theme = value;
            _settingsService.SaveGlobalSetting(_setting);
        }

        partial void OnIsAutoStartEnabledChanged(bool value)
        {
            if (_isInitializing) return;
            _setting.Basic.AutoStart.Enabled = value;
            _settingsService.SaveGlobalSetting(_setting);
        }

        partial void OnSelectedAutoStartMethodChanged(string value)
        {
            if (_isInitializing) return;
            _setting.Basic.AutoStart.Method = value;
            _settingsService.SaveGlobalSetting(_setting);
        }
    }

    /// <summary>
    /// 外观页面 ViewModel
    /// </summary>
    public partial class AppearancePageViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IDesktopWindowManager _desktopWindowManager;
        private TimeTopSetting _setting;
        private bool _isInitializing = true;

        [ObservableProperty]
        private bool _enableShadow = true;

        [ObservableProperty]
        private string _selectedTextEffect = "none";

        public AppearancePageViewModel(ISettingsService settingsService, IDesktopWindowManager desktopWindowManager)
        {
            _settingsService = settingsService;
            _desktopWindowManager = desktopWindowManager;
            _setting = _settingsService.GetTimeTopSetting();

            EnableShadow = _setting.ProgressBar.EnableShadow;
            SelectedTextEffect = _setting.TextOverlay.Style.TextEffect ?? "shadow";

            _isInitializing = false;
        }

        partial void OnEnableShadowChanged(bool value)
        {
            if (_isInitializing) return;
            _setting.ProgressBar.EnableShadow = value;
            _settingsService.SaveTimeTopSetting(_setting);
        }

        partial void OnSelectedTextEffectChanged(string value)
        {
            if (_isInitializing) return;
            _setting.TextOverlay.Style.TextEffect = value;
            _settingsService.SaveTimeTopSetting(_setting);
            _desktopWindowManager.RefreshTextOverlay();
        }
    }

    /// <summary>
    /// 关于页面 ViewModel
    /// </summary>
    public partial class AboutPageViewModel : ObservableObject
    {
        public AboutPageViewModel()
        {
        }
    }

    /// <summary>
    /// 窗口页面 ViewModel
    /// </summary>
    public partial class WindowPageViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IDesktopWindowManager _desktopWindowManager;
        private TimeTopSetting _setting;
        private bool _isInitializing = true;

        [ObservableProperty]
        private string _selectedTopmostMode = "OnDeactivated";

        [ObservableProperty]
        private string _selectedPosition = "top";

        [ObservableProperty]
        private bool _useFullScreen = false;

        public WindowPageViewModel(ISettingsService settingsService, IDesktopWindowManager desktopWindowManager)
        {
            _settingsService = settingsService;
            _desktopWindowManager = desktopWindowManager;
            _setting = _settingsService.GetTimeTopSetting();

            SelectedTopmostMode = _setting.Window.TopmostMode.ToString();
            SelectedPosition = _setting.ProgressBar.Position ?? "top";
            UseFullScreen = _setting.Window.UseFullScreen;

            _isInitializing = false;
        }

        partial void OnSelectedTopmostModeChanged(string value)
        {
            if (_isInitializing) return;
            if (Enum.TryParse<TopmostMode>(value, out var mode))
            {
                _setting.Window.TopmostMode = mode;
                _settingsService.SaveTimeTopSetting(_setting);
            }
        }

        partial void OnSelectedPositionChanged(string value)
        {
            if (_isInitializing) return;
            var position = ParsePosition(value);
            _setting.ProgressBar.Position = PositionToConfigString(position);
            _settingsService.SaveTimeTopSetting(_setting);

            _desktopWindowManager.SetPosition(position);
        }

        partial void OnUseFullScreenChanged(bool value)
        {
            if (_isInitializing) return;
            _setting.Window.UseFullScreen = value;
            _settingsService.SaveTimeTopSetting(_setting);

            _desktopWindowManager.RefreshPosition();
        }

        private static ProgressBarPosition ParsePosition(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "bottom" => ProgressBarPosition.Bottom,
                "left" => ProgressBarPosition.Left,
                "right" => ProgressBarPosition.Right,
                _ => ProgressBarPosition.Top
            };
        }

        private static string PositionToConfigString(ProgressBarPosition position)
        {
            return position switch
            {
                ProgressBarPosition.Bottom => "bottom",
                ProgressBarPosition.Left => "left",
                ProgressBarPosition.Right => "right",
                _ => "top"
            };
        }
    }

    public partial class TimeTopSettingViewModel : ObservableObject
    {
        private static readonly List<int> _hours = Enumerable.Range(0, 24).ToList();
        private static readonly List<int> _minutes = Enumerable.Range(0, 60).ToList();

        private const string TAG_BASIC = "Basic";
        private const string TAG_APPEARANCE = "Appearance";
        private const string TAG_TIME = "Time";
        private const string TAG_WINDOW = "Window";
        private const string TAG_ABOUT = "About";

        private readonly IGlobalTimeTopDesktopService _service;
        private readonly IMutexManager _mutexManager;
        private readonly ISettingsService _settingsService;
        private readonly IDesktopWindowManager _desktopWindowManager;
        private readonly ITimeService? _timeService;
        private readonly IScheduleManager? _scheduleManager;
        private readonly ITimeCalibrationService? _timeCalibrationService;
        private System.Windows.Threading.DispatcherTimer? _refreshTimer;

        // 导航属性
        [ObservableProperty]
        private object? _currentPage;

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

        [ObservableProperty]
        private bool _isMutexAcquired = false;

        [ObservableProperty]
        private string _mutexId = string.Empty;

        [ObservableProperty]
        private bool _isMutexEnabled = true;

        [ObservableProperty]
        private string _mutexStatus = "未知";

        [ObservableProperty]
        private ProgressBarPosition _currentPosition = ProgressBarPosition.Top;

        [ObservableProperty]
        private string _positionText = "顶部";

        // 新增：时间服务调试属性
        [ObservableProperty]
        private string _currentTime = "未知";

        [ObservableProperty]
        private bool _isCloudSynchronized = false;

        // 新增：执行计划调试属性
        [ObservableProperty]
        private string _currentSegmentName = "未知";

        [ObservableProperty]
        private string _currentState = "未知";

        [ObservableProperty]
        private string _nextTimePoint = "无";

        [ObservableProperty]
        private bool _isActiveSegment = false;

        // 新增：调度管理器调试属性
        [ObservableProperty]
        private bool _isScheduleManagerRunning = false;

        [ObservableProperty]
        private string _pollingInterval = "1秒";

        [ObservableProperty]
        private int _timePointCount = 0;

        // 新增：云端校准服务调试属性
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

        // Toast 通知测试属性
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

        public TimeTopSettingViewModel(
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

            // 初始化互斥锁状态
            UpdateMutexStatus();

            // 初始化进度条位置
            UpdatePositionStatus();

            _refreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += OnRefreshTimerTick;
            _refreshTimer.Start();

            RefreshDebugInfo();
        }

        /// <summary>
        /// 刷新定时器回调
        /// </summary>
        private void OnRefreshTimerTick(object? sender, EventArgs e)
        {
            RefreshDebugInfo();
        }

        /// <summary>
        /// 刷新调试信息
        /// </summary>
        private void RefreshDebugInfo()
        {
            try
            {
                // 时间服务信息
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

                // 执行计划信息
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

                // 云端校准服务信息
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
                Logger.Error("TimeTopSettingViewModel", "刷新调试信息时发生异常", ex);
            }
        }

        /// <summary>
        /// 初始化导航
        /// </summary>
        public void InitializeNavigation()
        {
            NavigateTo(TAG_BASIC);
        }

        private BasicPageViewModel? _basicPage;
        private AppearancePageViewModel? _appearancePage;
        private TimePageViewModel? _timePage;
        private WindowPageViewModel? _windowPage;
        private AboutPageViewModel? _aboutPage;

        /// <summary>
        /// 导航到指定页面（缓存 ViewModel 实例，避免重复加载配置）
        /// </summary>
        public void NavigateTo(string tag)
        {
            CurrentPage = tag switch
            {
                TAG_BASIC => _basicPage ??= new BasicPageViewModel(_settingsService),
                TAG_APPEARANCE => _appearancePage ??= new AppearancePageViewModel(_settingsService, _desktopWindowManager),
                TAG_TIME => _timePage ??= new TimePageViewModel(_settingsService, _timeService, _timeCalibrationService),
                TAG_WINDOW => _windowPage ??= new WindowPageViewModel(_settingsService, _desktopWindowManager),
                TAG_ABOUT => _aboutPage ??= new AboutPageViewModel(),
                _ => _basicPage ??= new BasicPageViewModel(_settingsService)
            };
        }

        partial void OnProgressValueChanged(double value)
        {
            _service.SetValue(value);
        }

        [RelayCommand]
        private void SetLoading()
        {
            _service.SetLoading();
        }

        [RelayCommand]
        private void SetSuccess()
        {
            _service.SetSuccess();
        }

        [RelayCommand]
        private void SetError()
        {
            _service.SetError();
        }

        [RelayCommand]
        private void SetPaused()
        {
            _service.SetPaused();
        }

        [RelayCommand]
        private void SetProgress()
        {
            _service.SetProgress(ProgressValue);
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= OnRefreshTimerTick;
            }

            _timePage?.Dispose();
            _timePage = null;
            _basicPage = null;
            _appearancePage = null;
            _windowPage = null;
            _aboutPage = null;
        }

        /// <summary>
        /// 更新互斥锁状态
        /// </summary>
        private void UpdateMutexStatus()
        {
            IsMutexAcquired = _mutexManager.IsAcquired;
            MutexId = _mutexManager.Config.MutexId;
            IsMutexEnabled = _mutexManager.Config.IsEnabled;
            MutexStatus = IsMutexAcquired ? "已获取" : "未获取";
        }

        /// <summary>
        /// 释放互斥锁
        /// </summary>
        [RelayCommand]
        private void ReleaseMutex()
        {
            try
            {
                _mutexManager.Release();
                UpdateMutexStatus();
                Logger.Info("TimeTopSettingViewModel", "互斥锁已释放");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "释放互斥锁时发生异常", ex);
            }
        }

        /// <summary>
        /// 重新获取互斥锁
        /// </summary>
        [RelayCommand]
        private void ReacquireMutex()
        {
            try
            {
                bool acquired = _mutexManager.TryAcquire();
                UpdateMutexStatus();

                if (acquired)
                {
                    Logger.Info("TimeTopSettingViewModel", "互斥锁重新获取成功");
                }
                else
                {
                    Logger.Warn("TimeTopSettingViewModel", "互斥锁重新获取失败");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "重新获取互斥锁时发生异常", ex);
            }
        }

        /// <summary>
        /// 切换互斥锁启用状态
        /// </summary>
        [RelayCommand]
        private void ToggleMutexEnabled()
        {
            try
            {
                var config = _mutexManager.Config;
                config.IsEnabled = !config.IsEnabled;
                IsMutexEnabled = config.IsEnabled;

                Logger.Info("TimeTopSettingViewModel", $"互斥锁已{(config.IsEnabled ? "启用" : "禁用")}");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "切换互斥锁启用状态时发生异常", ex);
            }
        }

        // ==================== GlobalTimeTopDesktopService API 调试命令 ====================

        /// <summary>
        /// 设置为隐藏状态
        /// </summary>
        [RelayCommand]
        private void SetHidden()
        {
            _service.SetHidden();
        }

        /// <summary>
        /// 设置为禁用状态
        /// </summary>
        [RelayCommand]
        private void SetDisabled()
        {
            _service.SetDisabled();
        }

        /// <summary>
        /// 设置可见性为 Visible
        /// </summary>
        [RelayCommand]
        private void SetVisibilityVisible()
        {
            _service.SetVisibility(Visibility.Visible);
        }

        /// <summary>
        /// 设置可见性为 Hidden
        /// </summary>
        [RelayCommand]
        private void SetVisibilityHidden()
        {
            _service.SetVisibility(Visibility.Hidden);
        }

        /// <summary>
        /// 设置可见性为 Collapsed
        /// </summary>
        [RelayCommand]
        private void SetVisibilityCollapsed()
        {
            _service.SetVisibility(Visibility.Collapsed);
        }

        /// <summary>
        /// 设置启用状态为 True
        /// </summary>
        [RelayCommand]
        private void SetEnabledTrue()
        {
            _service.SetEnabled(true);
        }

        /// <summary>
        /// 设置启用状态为 False
        /// </summary>
        [RelayCommand]
        private void SetEnabledFalse()
        {
            _service.SetEnabled(false);
        }

        /// <summary>
        /// 设置透明度为 1.0
        /// </summary>
        [RelayCommand]
        private void SetOpacityFull()
        {
            _service.SetOpacity(1.0);
        }

        /// <summary>
        /// 设置透明度为 0.5
        /// </summary>
        [RelayCommand]
        private void SetOpacityHalf()
        {
            _service.SetOpacity(0.5);
        }

        /// <summary>
        /// 设置透明度为 0.2
        /// </summary>
        [RelayCommand]
        private void SetOpacityLow()
        {
            _service.SetOpacity(0.2);
        }

        /// <summary>
        /// 设置前景色为蓝色
        /// </summary>
        [RelayCommand]
        private void SetForegroundBlue()
        {
            _service.SetForeground(ProgressColors.DefaultBlue);
        }

        /// <summary>
        /// 设置前景色为绿色
        /// </summary>
        [RelayCommand]
        private void SetForegroundGreen()
        {
            _service.SetForeground(ProgressColors.SuccessGreen);
        }

        /// <summary>
        /// 设置前景色为红色
        /// </summary>
        [RelayCommand]
        private void SetForegroundRed()
        {
            _service.SetForeground(ProgressColors.ErrorRed);
        }

        /// <summary>
        /// 设置前景色为橙色
        /// </summary>
        [RelayCommand]
        private void SetForegroundOrange()
        {
            _service.SetForeground(ProgressColors.PauseOrange);
        }

        /// <summary>
        /// 设置前景色为灰色
        /// </summary>
        [RelayCommand]
        private void SetForegroundGray()
        {
            _service.SetForeground(ProgressColors.Gray);
        }

        /// <summary>
        /// 设置背景色为透明
        /// </summary>
        [RelayCommand]
        private void SetBackgroundTransparent()
        {
            _service.SetBackground(Brushes.Transparent);
        }

        /// <summary>
        /// 设置背景色为浅灰色
        /// </summary>
        [RelayCommand]
        private void SetBackgroundLightGray()
        {
            _service.SetBackground(Brushes.LightGray);
        }

        /// <summary>
        /// 设置背景色为白色
        /// </summary>
        [RelayCommand]
        private void SetBackgroundWhite()
        {
            _service.SetBackground(Brushes.White);
        }

        /// <summary>
        /// 设置范围为 0-100
        /// </summary>
        [RelayCommand]
        private void SetRange0100()
        {
            _service.SetRange(0, 100);
        }

        /// <summary>
        /// 设置范围为 0-1
        /// </summary>
        [RelayCommand]
        private void SetRange01()
        {
            _service.SetRange(0, 1);
        }

        /// <summary>
        /// 重置为默认状态
        /// </summary>
        [RelayCommand]
        private void ResetState()
        {
            _service.Reset();
        }

        /// <summary>
        /// 批量更新测试
        /// </summary>
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

        // ==================== 进度条位置控制 ====================

        /// <summary>
        /// 更新位置状态
        /// </summary>
        private void UpdatePositionStatus()
        {
            CurrentPosition = _desktopWindowManager.CurrentPosition;
            PositionText = GetPositionText(CurrentPosition);
        }

        /// <summary>
        /// 获取位置文本
        /// </summary>
        private string GetPositionText(ProgressBarPosition position)
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

        /// <summary>
        /// 切换到顶部位置
        /// </summary>
        [RelayCommand]
        private void SetPositionTop()
        {
            try
            {
                _desktopWindowManager.SetPosition(ProgressBarPosition.Top);
                SavePositionConfig(ProgressBarPosition.Top);
                UpdatePositionStatus();
                Logger.Info("TimeTopSettingViewModel", "进度条位置已切换到顶部");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "切换进度条位置到顶部时发生异常", ex);
            }
        }

        /// <summary>
        /// 切换到底部位置
        /// </summary>
        [RelayCommand]
        private void SetPositionBottom()
        {
            try
            {
                _desktopWindowManager.SetPosition(ProgressBarPosition.Bottom);
                SavePositionConfig(ProgressBarPosition.Bottom);
                UpdatePositionStatus();
                Logger.Info("TimeTopSettingViewModel", "进度条位置已切换到底部");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "切换进度条位置到底部时发生异常", ex);
            }
        }

        /// <summary>
        /// 切换到左侧位置
        /// </summary>
        [RelayCommand]
        private void SetPositionLeft()
        {
            try
            {
                _desktopWindowManager.SetPosition(ProgressBarPosition.Left);
                SavePositionConfig(ProgressBarPosition.Left);
                UpdatePositionStatus();
                Logger.Info("TimeTopSettingViewModel", "进度条位置已切换到左侧");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "切换进度条位置到左侧时发生异常", ex);
            }
        }

        /// <summary>
        /// 切换到右侧位置
        /// </summary>
        [RelayCommand]
        private void SetPositionRight()
        {
            try
            {
                _desktopWindowManager.SetPosition(ProgressBarPosition.Right);
                SavePositionConfig(ProgressBarPosition.Right);
                UpdatePositionStatus();
                Logger.Info("TimeTopSettingViewModel", "进度条位置已切换到右侧");
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "切换进度条位置到右侧时发生异常", ex);
            }
        }

        /// <summary>
        /// 保存位置配置到文件
        /// </summary>
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

        /// <summary>
        /// 手动校准时间向前
        /// </summary>
        [RelayCommand]
        private void CalibrateTimeForward()
        {
            if (_timeService != null)
            {
                var newTime = _timeService.GetCurrentTime().AddSeconds(5);
                _timeService.Calibrate(newTime);
                Logger.Info("TimeTopSettingViewModel", $"时间已校准向前 5 秒: {newTime:HH:mm:ss}");
                RefreshDebugInfo();
            }
        }

        /// <summary>
        /// 手动校准时间向后
        /// </summary>
        [RelayCommand]
        private void CalibrateTimeBackward()
        {
            if (_timeService != null)
            {
                var newTime = _timeService.GetCurrentTime().AddSeconds(-5);
                _timeService.Calibrate(newTime);
                Logger.Info("TimeTopSettingViewModel", $"时间已校准向后 5 秒: {newTime:HH:mm:ss}");
                RefreshDebugInfo();
            }
        }

        // ==================== 执行计划调试命令 ====================

        /// <summary>
        /// 显示执行计划
        /// </summary>
        [RelayCommand]
        private void ShowExecutionPlan()
        {
            Logger.Info("TimeTopSettingViewModel", "=== 执行计划详情 ===");
            Logger.Info("TimeTopSettingViewModel", $"时间点数量: {TimePointCount}");
            Logger.Info("TimeTopSettingViewModel", $"当前时间段: {CurrentSegmentName}");
            Logger.Info("TimeTopSettingViewModel", $"当前状态: {CurrentState}");
            Logger.Info("TimeTopSettingViewModel", $"下个时间点: {NextTimePoint}");
            Logger.Info("TimeTopSettingViewModel", "====================");
        }

        /// <summary>
        /// 应用当前状态
        /// </summary>
        [RelayCommand]
        private void ApplyCurrentState()
        {
            if (_scheduleManager != null)
            {
                _scheduleManager.ApplyCurrentState();
                Logger.Info("TimeTopSettingViewModel", "当前状态已重新应用");
            }
        }

        // ==================== 调度管理器调试命令 ====================

        /// <summary>
        /// 停止调度管理器
        /// </summary>
        [RelayCommand]
        private void StopScheduleManager()
        {
            if (_scheduleManager != null)
            {
                _scheduleManager.Stop();
                IsScheduleManagerRunning = false;
                Logger.Info("TimeTopSettingViewModel", "调度管理器已停止");
            }
        }

        /// <summary>
        /// 重启调度管理器
        /// </summary>
        [RelayCommand]
        private void RestartScheduleManager()
        {
            if (_scheduleManager != null)
            {
                try
                {
                    // 保存当前执行计划
                    var currentPlan = _scheduleManager.CurrentPlan;

                    // 停止调度管理器
                    _scheduleManager.Stop();

                    // 重新初始化执行计划
                    if (currentPlan != null)
                    {
                        _scheduleManager.Initialize(currentPlan);
                        Logger.Info("TimeTopSettingViewModel", "调度管理器已重启，执行计划已重新应用");
                    }
                    else
                    {
                        Logger.Warn("TimeTopSettingViewModel", "无法重启调度管理器：执行计划为空");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("TimeTopSettingViewModel", "重启调度管理器时发生异常", ex);
                }
            }
        }

        // ==================== 云端校准服务调试命令 ====================

        /// <summary>
        /// 手动触发校准
        /// </summary>
        [RelayCommand]
        private async Task TriggerManualCalibration()
        {
            try
            {
                if (_timeCalibrationService != null)
                {
                    var success = await _timeCalibrationService.CalibrateAsync();
                    LastCalibrationTime = DateTime.Now.ToString("HH:mm:ss");
                    Logger.Info("TimeTopSettingViewModel", $"手动校准{(success ? "成功" : "失败")}");
                    RefreshDebugInfo();
                }
                else
                {
                    Logger.Warn("TimeTopSettingViewModel", "云端校准服务未初始化");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "手动触发校准时发生异常", ex);
            }
        }

        /// <summary>
        /// 重置校准失败计数
        /// </summary>
        [RelayCommand]
        private void ResetCalibrationFailures()
        {
            if (_timeCalibrationService != null)
            {
                _timeCalibrationService.Reset();
                CalibrationFailureCount = 0;
                Logger.Info("TimeTopSettingViewModel", "校准失败计数已重置");
                RefreshDebugInfo();
            }
        }

        /// <summary>
        /// 应用NTP服务器配置
        /// </summary>
        [RelayCommand]
        private void ApplyTimeSourceConfig()
        {
            try
            {
                if (_timeCalibrationService != null)
                {
                    var selectedServer = NtpServerDefaults.Servers[SelectedNtpServerIndex];

                    // 更新配置并应用到校准服务
                    var timeTopSetting = _settingsService.GetTimeTopSetting();
                    timeTopSetting.Calibration.Cloud.SelectedServerAddress = selectedServer;
                    _settingsService.SaveTimeTopSetting(timeTopSetting);

                    // 通过 ApplyConfig 统一应用配置（内部会配置NTP服务器）
                    _timeCalibrationService.ApplyConfig(timeTopSetting.Calibration);

                    Logger.Info("TimeTopSettingViewModel", $"NTP服务器配置已应用: 服务器={selectedServer}");
                    RefreshDebugInfo();
                }
                else
                {
                    Logger.Warn("TimeTopSettingViewModel", "时间校准服务未初始化");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("TimeTopSettingViewModel", "应用NTP服务器配置时发生异常", ex);
            }
        }

        // ==================== 样式优先级测试命令 ====================

        /// <summary>
        /// 应用默认样式
        /// </summary>
        [RelayCommand]
        private void ApplyDefaultStyle()
        {
            _service.SetForeground(ProgressColors.DefaultBlue);
            _service.SetOpacity(1.0);
            _service.SetVisibility(Visibility.Visible);
            Logger.Info("TimeTopSettingViewModel", "已应用默认样式");
        }

        /// <summary>
        /// 应用配置文件样式
        /// </summary>
        [RelayCommand]
        private void ApplyConfigStyle()
        {
            _service.SetForeground(new SolidColorBrush(Color.FromRgb(0x2D, 0x7D, 0x9A))); // 自定义蓝色
            _service.SetOpacity(0.9);
            Logger.Info("TimeTopSettingViewModel", "已应用配置文件样式");
        }

        /// <summary>
        /// 应用时间表样式
        /// </summary>
        [RelayCommand]
        private void ApplyScheduleStyle()
        {
            _service.SetForeground(new SolidColorBrush(Color.FromRgb(0xFF, 0x57, 0x33))); // 橙红色
            _service.SetOpacity(1.0);
            Logger.Info("TimeTopSettingViewModel", "已应用时间表样式");
        }

        // ==================== 综合测试命令 ====================

        /// <summary>
        /// 运行综合测试
        /// </summary>
        [RelayCommand]
        private void RunComprehensiveTest()
        {
            Logger.Info("TimeTopSettingViewModel", "=== 开始综合测试 ===");

            // 测试 1: 时间服务
            if (_timeService != null)
            {
                var time = _timeService.GetCurrentTime();
                Logger.Info("TimeTopSettingViewModel", $"[测试 1] 时间服务: {time:HH:mm:ss}, 云端同步: {_timeService.IsCloudSynchronized}");
            }

            // 测试 2: 状态切换
            _service.SetLoading();
            Logger.Info("TimeTopSettingViewModel", "[测试 2] 状态切换: Loading");

            _service.SetProgress(50);
            Logger.Info("TimeTopSettingViewModel", "[测试 2] 状态切换: Progress (50%)");

            _service.SetSuccess();
            Logger.Info("TimeTopSettingViewModel", "[测试 2] 状态切换: Success");

            // 测试 3: 样式应用
            ApplyDefaultStyle();
            Logger.Info("TimeTopSettingViewModel", "[测试 3] 默认样式已应用");

            ApplyConfigStyle();
            Logger.Info("TimeTopSettingViewModel", "[测试 3] 配置文件样式已应用");

            // 测试 4: 时间跳跃
            if (_timeService != null)
            {
                var oldTime = _timeService.GetCurrentTime();
                _timeService.Calibrate(oldTime.AddSeconds(10));
                var newTime = _timeService.GetCurrentTime();
                Logger.Info("TimeTopSettingViewModel", $"[测试 4] 时间跳跃: {oldTime:HH:mm:ss} → {newTime:HH:mm:ss}");
            }

            Logger.Info("TimeTopSettingViewModel", "=== 综合测试完成 ===");
        }

        /// <summary>
        /// 记录所有服务的状态
        /// </summary>
        [RelayCommand]
        private void LogAllServicesStatus()
        {
            Logger.Info("TimeTopSettingViewModel", "开始记录所有服务的当前状态...");
            Logger.Info("TimeTopSettingViewModel", $"主服务: {(_service != null ? "正常" : "未初始化")}");
            Logger.Info("TimeTopSettingViewModel", $"时间服务: {(_timeService != null ? "正常" : "未初始化")}");
            Logger.Info("TimeTopSettingViewModel", $"调度管理器: {(_scheduleManager != null ? "正常" : "未初始化")}");
            Logger.Info("TimeTopSettingViewModel", $"云校准服务: {(_timeCalibrationService != null ? "正常" : "未初始化")}");

            // 记录详细状态
            Logger.Info("TimeTopSettingViewModel", $"当前时间: {CurrentTime}, 云同步: {IsCloudSynchronized}");
            Logger.Info("TimeTopSettingViewModel", $"当前状态: {CurrentState}, 活动段: {IsActiveSegment}, 下个时间点: {NextTimePoint}");
            Logger.Info("TimeTopSettingViewModel", "服务状态记录完毕。");
        }

        /// <summary>
        /// 查看测试报告
        /// </summary>
        [RelayCommand]
        private void ShowTestReport()
        {
            Logger.Info("TimeTopSettingViewModel", "=== 测试报告 ===");
            Logger.Info("TimeTopSettingViewModel", "时间服务状态:");
            Logger.Info("TimeTopSettingViewModel", $"  - 当前时间: {CurrentTime}");
            Logger.Info("TimeTopSettingViewModel", $"  - 云端同步: {(IsCloudSynchronized ? "是" : "否")}");

            Logger.Info("TimeTopSettingViewModel", "执行计划状态:");
            Logger.Info("TimeTopSettingViewModel", $"  - 当前状态: {CurrentState}");
            Logger.Info("TimeTopSettingViewModel", $"  - 是否活跃段: {IsActiveSegment}");
            Logger.Info("TimeTopSettingViewModel", $"  - 下个时间点: {NextTimePoint}");

            Logger.Info("TimeTopSettingViewModel", "调度管理器状态:");
            Logger.Info("TimeTopSettingViewModel", $"  - 状态: {(_scheduleManager?.IsRunning == true ? "运行中" : "已停止")}");

            Logger.Info("TimeTopSettingViewModel", "云校准服务状态:");
            if (_timeCalibrationService != null)
            {
                Logger.Info("TimeTopSettingViewModel", $"  - 上次校准: {_timeCalibrationService.LastCalibrationTime:yyyy-MM-dd HH:mm:ss}");
                Logger.Info("TimeTopSettingViewModel", $"  - 失败次数: {_timeCalibrationService.FailureCount}");
                Logger.Info("TimeTopSettingViewModel", $"  - 当前间隔: {_timeCalibrationService.CurrentInterval}秒");
            }
            else
            {
                Logger.Info("TimeTopSettingViewModel", "  - 未初始化");
            }

            Logger.Info("TimeTopSettingViewModel", "=== 测试报告结束 ===");
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