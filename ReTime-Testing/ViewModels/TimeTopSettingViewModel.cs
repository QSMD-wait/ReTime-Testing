using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
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
        private readonly IConfigurationManager _configManager;
        private readonly IThemeService _themeService;
        private readonly IAutoStartService _autoStartService;
        private GlobalSetting _setting;

        [ObservableProperty]
        private string _selectedTheme = "light";

        [ObservableProperty]
        private bool _isAutoStartEnabled;

        [ObservableProperty]
        private string _selectedAutoStartMethod = "registry";

        public BasicPageViewModel(IThemeService themeService, IAutoStartService autoStartService,
            IConfigurationManager? configManager = null)
        {
            _configManager = configManager ?? ConfigurationManager.Instance;
            _themeService = themeService;
            _autoStartService = autoStartService;
            _setting = _configManager.LoadGlobalSetting();

            SelectedTheme = _setting.Basic.Theme;
            IsAutoStartEnabled = _setting.Basic.AutoStart.Enabled;
            SelectedAutoStartMethod = _setting.Basic.AutoStart.Method;
        }

        partial void OnSelectedThemeChanged(string value)
        {
            _setting.Basic.Theme = value;
            _themeService.ApplyTheme(value);
            SaveSetting();
        }

        partial void OnIsAutoStartEnabledChanged(bool value)
        {
            _setting.Basic.AutoStart.Enabled = value;

            if (value)
                _autoStartService.Enable(SelectedAutoStartMethod);
            else
                _autoStartService.Disable();

            SaveSetting();
        }

        partial void OnSelectedAutoStartMethodChanged(string value)
        {
            _setting.Basic.AutoStart.Method = value;

            if (IsAutoStartEnabled)
                _autoStartService.Enable(value);

            SaveSetting();
        }

        private void SaveSetting()
        {
            _configManager.SaveGlobalSetting(_setting);
        }
    }

    /// <summary>
    /// 外观页面 ViewModel
    /// </summary>
    public partial class AppearancePageViewModel : ObservableObject
    {
        private readonly IConfigurationManager _configManager;
        private TimeTopSetting _setting;

        [ObservableProperty]
        private bool _enableShadow = true;

        public AppearancePageViewModel(IConfigurationManager? configManager = null)
        {
            _configManager = configManager ?? ConfigurationManager.Instance;
            _setting = _configManager.LoadTimeTopSetting();

            EnableShadow = _setting.ProgressBar.EnableShadow;
        }

        partial void OnEnableShadowChanged(bool value)
        {
            _setting.ProgressBar.EnableShadow = value;
            SaveSetting();
        }

        private void SaveSetting()
        {
            _configManager.SaveTimeTopSetting(_setting);
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
        private readonly IConfigurationManager _configManager;
        private TimeTopSetting _setting;

        [ObservableProperty]
        private string _selectedTopmostMode = "OnDeactivated";

        [ObservableProperty]
        private string _selectedPosition = "Top";

        [ObservableProperty]
        private bool _useFullScreen = false;

        public WindowPageViewModel(IConfigurationManager? configManager = null)
        {
            _configManager = configManager ?? ConfigurationManager.Instance;
            _setting = _configManager.LoadTimeTopSetting();

            SelectedTopmostMode = _setting.Window.TopmostMode.ToString();
            SelectedPosition = PositionToString(ParsePosition(_setting.ProgressBar.Position));
            UseFullScreen = _setting.Window.UseFullScreen;
        }

        partial void OnSelectedTopmostModeChanged(string value)
        {
            if (Enum.TryParse<TopmostMode>(value, out var mode))
            {
                _setting.Window.TopmostMode = mode;
                SaveSetting();
            }
        }

        partial void OnSelectedPositionChanged(string value)
        {
            var position = ParsePosition(value);
            _setting.ProgressBar.Position = PositionToConfigString(position);
            SaveSetting();

            DesktopWindowManager.Instance.SetPosition(position);
        }

        partial void OnUseFullScreenChanged(bool value)
        {
            _setting.Window.UseFullScreen = value;
            SaveSetting();

            DesktopWindowManager.Instance.RefreshPosition();
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

        private static string PositionToString(ProgressBarPosition position)
        {
            return position switch
            {
                ProgressBarPosition.Bottom => "Bottom",
                ProgressBarPosition.Left => "Left",
                ProgressBarPosition.Right => "Right",
                _ => "Top"
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

        private void SaveSetting()
        {
            _configManager.SaveTimeTopSetting(_setting);
        }
    }

    public partial class TimeTopSettingViewModel : ObservableObject
    {
        private static readonly List<int> _hours = Enumerable.Range(0, 24).ToList();
        private static readonly List<int> _minutes = Enumerable.Range(0, 60).ToList();

        // 导航标签常量
        private const string TAG_BASIC = "Basic";
        private const string TAG_APPEARANCE = "Appearance";
        private const string TAG_TIME = "Time";
        private const string TAG_WINDOW = "Window";
        private const string TAG_ABOUT = "About";

        private readonly GlobalTimeTopDesktopService _service;
        private readonly MutexManager _mutexManager;
        private ITimeService? _timeService;
        private ScheduleManager? _scheduleManager;
        private ITimeCalibrationService? _timeCalibrationService;
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

        public TimeTopSettingViewModel()
        {
            _service = GlobalTimeTopDesktopService.Instance;
            _mutexManager = MutexManager.Instance;

            // 订阅 Service 的调度状态变更事件
            _service.OnScheduleStateChanged += OnScheduleStateChanged;

            // 初始化互斥锁状态
            UpdateMutexStatus();

            // 初始化进度条位置
            UpdatePositionStatus();

            // 延迟初始化新服务引用（在窗口加载后）
            _refreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += OnRefreshTimerTick;
            _refreshTimer.Start();

            // 立即执行一次刷新
            RefreshDebugInfo();
        }

        /// <summary>
        /// 初始化新服务
        /// </summary>
        private void InitializeNewServices()
        {
            // 从 App.xaml.cs 获取服务实例
            var app = System.Windows.Application.Current as App;
            if (app != null)
            {
                _timeService = app.TimeService;
                _scheduleManager = app.ScheduleManager;
                _timeCalibrationService = app.TimeCalibrationService;

                Logger.Info("TimeTopSettingViewModel", $"新服务引用已初始化: TimeService={_timeService != null}, ScheduleManager={_scheduleManager != null}, TimeCalibrationService={_timeCalibrationService != null}");
            }
            else
            {
                Logger.Warn("TimeTopSettingViewModel", "无法获取 App 实例");
            }
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
                // 如果服务尚未初始化，尝试初始化
                if (_timeService == null || _scheduleManager == null || _timeCalibrationService == null)
                {
                    InitializeNewServices();
                }

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
            // 默认选中全局设置
             NavigateTo(TAG_BASIC);
        }

        /// <summary>
        /// 导航到指定页面
        /// </summary>
         public void NavigateTo(string tag)
        {
            var app = Application.Current as App;
            var themeService = app?.ThemeService;
            var autoStartService = app?.AutoStartService;

            CurrentPage = tag switch
            {
                TAG_BASIC => new BasicPageViewModel(themeService!, autoStartService!),
                TAG_APPEARANCE => new AppearancePageViewModel(),
                TAG_TIME => new TimePageViewModel(timeService: _timeService, timeCalibrationService: _timeCalibrationService),
                TAG_WINDOW => new WindowPageViewModel(),
                TAG_ABOUT => new AboutPageViewModel(),
                _ => new BasicPageViewModel(themeService!, autoStartService!)
            };
        }

        /// <summary>
        /// 调度状态变更回调
        /// </summary>
        private void OnScheduleStateChanged(double progress, string status)
        {
            ScheduleProgress = progress;
            ScheduleStatus = status;
            IsScheduleRunning = _service.IsScheduleRunning;
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
        /// 开始调度（调试功能，已废弃）
        /// </summary>
        [Obsolete("请使用 ScheduleManager 替代", false)]
        [RelayCommand]
        private void StartSchedule()
        {
            bool started = _service.StartSchedule(StartHour, StartMinute, 0, EndHour, EndMinute, 0);

            if (started)
            {
                IsStateControlsEnabled = false;
                IsScheduleRunning = true;
            }
            else
            {
                ScheduleStatus = "错误：启动失败";
            }
        }

        /// <summary>
        /// 停止调度（调试功能，已废弃）
        /// </summary>
        [Obsolete("请使用 ScheduleManager 替代", false)]
        [RelayCommand]
        private void StopSchedule()
        {
            _service.StopSchedule();
            IsStateControlsEnabled = true;
            IsScheduleRunning = false;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            // 停止刷新定时器
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= OnRefreshTimerTick;
            }

            // 取消订阅 Service 事件
            if (_service != null)
            {
                _service.OnScheduleStateChanged -= OnScheduleStateChanged;
            }

            // 清理服务引用
            _timeService = null;
            _scheduleManager = null;
            _timeCalibrationService = null;
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
            var manager = DesktopWindowManager.Instance;
            CurrentPosition = manager.CurrentPosition;
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
                DesktopWindowManager.Instance.SetPosition(ProgressBarPosition.Top);
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
                DesktopWindowManager.Instance.SetPosition(ProgressBarPosition.Bottom);
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
                DesktopWindowManager.Instance.SetPosition(ProgressBarPosition.Left);
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
                DesktopWindowManager.Instance.SetPosition(ProgressBarPosition.Right);
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
        private static void SavePositionConfig(ProgressBarPosition position)
        {
            var configManager = Services.ConfigurationManager.Instance;
            var setting = configManager.LoadTimeTopSetting();
            setting.ProgressBar.Position = position switch
            {
                ProgressBarPosition.Bottom => "bottom",
                ProgressBarPosition.Left => "left",
                ProgressBarPosition.Right => "right",
                _ => "top"
            };
            configManager.SaveTimeTopSetting(setting);
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
                    var configManager = ConfigurationManager.Instance;
                    var timeTopSetting = configManager.LoadTimeTopSetting();
                    timeTopSetting.Calibration.Cloud.SelectedServerAddress = selectedServer;
                    configManager.SaveTimeTopSetting(timeTopSetting);

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
            Logger.Info("TimeTopSettingViewModel", $"  - 状态: {(IsScheduleRunning ? "运行中" : "已停止")}");

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
    }
}