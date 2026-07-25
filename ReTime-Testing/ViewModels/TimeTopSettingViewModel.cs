using CommunityToolkit.Mvvm.ComponentModel;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels
{
    /// <summary>
    /// 基本设置页面 ViewModel
    /// </summary>
    public partial class BasicPageViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private Models.GlobalSetting _setting;
        private bool _isInitializing = true;

        [ObservableProperty]
        private string _selectedTheme = "light";

        [ObservableProperty]
        private bool _isAutoStartEnabled;

        [ObservableProperty]
        private string _selectedAutoStartMethod = "registry";

        [ObservableProperty]
        private bool _isFileLogEnabled = true;

        [ObservableProperty]
        private int _selectedLogLevelIndex = 2;

        [ObservableProperty]
        private int _retainedDays = 30;

        [ObservableProperty]
        private int _fileSizeLimitMB = 10;

        public List<string> LogLevelNames { get; } = new() { "错误 (ERR)", "警告 (WRN)", "信息 (INF)", "调试 (DBG)", "跟踪 (TRC)" };

        public BasicPageViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _setting = _settingsService.GetGlobalSetting();

            SelectedTheme = _setting.Basic.Theme;
            IsAutoStartEnabled = _setting.Basic.AutoStart.Enabled;
            SelectedAutoStartMethod = _setting.Basic.AutoStart.Method;

            IsFileLogEnabled = _setting.Basic.Log.EnableFileOutput;
            SelectedLogLevelIndex = 4 - (int)_setting.Basic.Log.MinimumLevel;
            RetainedDays = _setting.Basic.Log.RetainedDays;
            FileSizeLimitMB = _setting.Basic.Log.FileSizeLimitMB;

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

        partial void OnIsFileLogEnabledChanged(bool value)
        {
            if (_isInitializing) return;
            _setting.Basic.Log.EnableFileOutput = value;
            _settingsService.SaveGlobalSetting(_setting);
        }

        partial void OnSelectedLogLevelIndexChanged(int value)
        {
            if (_isInitializing) return;
            _setting.Basic.Log.MinimumLevel = (Models.LogLevel)(4 - value);
            _settingsService.SaveGlobalSetting(_setting);
        }

        partial void OnRetainedDaysChanged(int value)
        {
            if (_isInitializing) return;
            _setting.Basic.Log.RetainedDays = value;
            _settingsService.SaveGlobalSetting(_setting);
        }

        partial void OnFileSizeLimitMBChanged(int value)
        {
            if (_isInitializing) return;
            _setting.Basic.Log.FileSizeLimitMB = value;
            _settingsService.SaveGlobalSetting(_setting);
        }
    }

    /// <summary>
    /// 单个状态样式项 ViewModel
    /// </summary>
    public partial class StateStyleItemViewModel : ObservableObject
    {
        private readonly Action _saveCallback;
        private readonly Models.StateStyleEntry _entry;
        private bool _isInitializing = true;

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private string _foregroundColor = "";

        [ObservableProperty]
        private string _backgroundColor = "";

        [ObservableProperty]
        private double _opacity = 1.0;

        [ObservableProperty]
        private bool _enableShadow;

        [ObservableProperty]
        private bool _hasEnableShadowOverride;

        public string StateName { get; }
        public string DisplayName { get; }

        private static readonly Dictionary<string, string> StateDisplayNames = new()
        {
            ["Loading"] = "加载中",
            ["Progress"] = "进行中",
            ["Success"] = "成功",
            ["Error"] = "错误",
            ["Paused"] = "暂停",
            ["Hidden"] = "隐藏",
            ["Disabled"] = "禁用"
        };

        public StateStyleItemViewModel(string stateName, Models.StateStyleEntry entry, Action saveCallback)
        {
            StateName = stateName;
            DisplayName = StateDisplayNames.GetValueOrDefault(stateName, stateName);
            _entry = entry;
            _saveCallback = saveCallback;

            IsEnabled = entry.Enabled;
            ForegroundColor = entry.ForegroundColor ?? "";
            BackgroundColor = entry.BackgroundColor ?? "";
            Opacity = entry.Opacity ?? 1.0;
            HasEnableShadowOverride = entry.EnableShadow.HasValue;
            EnableShadow = entry.EnableShadow ?? false;

            _isInitializing = false;
        }

        partial void OnIsEnabledChanged(bool value)
        {
            if (_isInitializing) return;
            _entry.Enabled = value;
            _saveCallback();
        }

        partial void OnForegroundColorChanged(string value)
        {
            if (_isInitializing) return;
            _entry.ForegroundColor = string.IsNullOrWhiteSpace(value) ? null : value;
            _saveCallback();
        }

        partial void OnBackgroundColorChanged(string value)
        {
            if (_isInitializing) return;
            _entry.BackgroundColor = string.IsNullOrWhiteSpace(value) ? null : value;
            _saveCallback();
        }

        partial void OnOpacityChanged(double value)
        {
            if (_isInitializing) return;
            _entry.Opacity = value;
            _saveCallback();
        }

        partial void OnEnableShadowChanged(bool value)
        {
            if (_isInitializing) return;
            if (HasEnableShadowOverride)
            {
                _entry.EnableShadow = value;
                _saveCallback();
            }
        }

        partial void OnHasEnableShadowOverrideChanged(bool value)
        {
            if (_isInitializing) return;
            _entry.EnableShadow = value ? EnableShadow : null;
            _saveCallback();
        }
    }

    /// <summary>
    /// 外观页面 ViewModel
    /// </summary>
    public partial class AppearancePageViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        private readonly IDesktopWindowManager _desktopWindowManager;
        private Models.TimeTopSetting _setting;
        private bool _isInitializing = true;

        [ObservableProperty]
        private bool _enableShadow = true;

        [ObservableProperty]
        private string _selectedTextEffect = "none";

        [ObservableProperty]
        private int _progressBarHeight = 5;

        [ObservableProperty]
        private int _cornerRadius;

        [ObservableProperty]
        private bool _glowEnabled = true;

        [ObservableProperty]
        private string _glowColor = "";

        [ObservableProperty]
        private bool _autoHide;

        [ObservableProperty]
        private double _idleOpacity = 0.3;

        [ObservableProperty]
        private bool _stateStylesEnabled = true;

        public List<StateStyleItemViewModel> StateStyleItems { get; } = [];

        private void SaveAndRefresh()
        {
            _settingsService.SaveTimeTopSetting(_setting);
            _desktopWindowManager.RefreshPosition();
        }

        public AppearancePageViewModel(ISettingsService settingsService, IDesktopWindowManager desktopWindowManager)
        {
            _settingsService = settingsService;
            _desktopWindowManager = desktopWindowManager;
            _setting = _settingsService.GetTimeTopSetting();

            EnableShadow = _setting.ProgressBar.EnableShadow;
            SelectedTextEffect = _setting.TextOverlay.Style.TextEffect ?? "shadow";

            ProgressBarHeight = _setting.ProgressBar.Height;
            CornerRadius = _setting.ProgressBar.CornerRadius;
            GlowEnabled = _setting.ProgressBar.GlowEnabled;
            GlowColor = _setting.ProgressBar.GlowColor ?? "";

            AutoHide = _setting.Behavior.AutoHide;
            IdleOpacity = _setting.Behavior.IdleOpacity;

            StateStylesEnabled = _setting.StateStyles.Enabled;
            foreach (var kvp in _setting.StateStyles.Styles)
            {
                StateStyleItems.Add(new StateStyleItemViewModel(kvp.Key, kvp.Value, SaveAndRefresh));
            }

            _isInitializing = false;
        }

        partial void OnEnableShadowChanged(bool value)
        {
            if (_isInitializing) return;
            _setting.ProgressBar.EnableShadow = value;
            SaveAndRefresh();
        }

        partial void OnSelectedTextEffectChanged(string value)
        {
            if (_isInitializing) return;
            _setting.TextOverlay.Style.TextEffect = value;
            _settingsService.SaveTimeTopSetting(_setting);
            _desktopWindowManager.RefreshTextOverlay();
        }

        partial void OnProgressBarHeightChanged(int value)
        {
            if (_isInitializing) return;
            _setting.ProgressBar.Height = value;
            SaveAndRefresh();
        }

        partial void OnCornerRadiusChanged(int value)
        {
            if (_isInitializing) return;
            _setting.ProgressBar.CornerRadius = value;
            SaveAndRefresh();
        }

        partial void OnGlowEnabledChanged(bool value)
        {
            if (_isInitializing) return;
            _setting.ProgressBar.GlowEnabled = value;
            SaveAndRefresh();
        }

        partial void OnGlowColorChanged(string value)
        {
            if (_isInitializing) return;
            _setting.ProgressBar.GlowColor = string.IsNullOrWhiteSpace(value) ? null : value;
            SaveAndRefresh();
        }

        partial void OnAutoHideChanged(bool value)
        {
            if (_isInitializing) return;
            _setting.Behavior.AutoHide = value;
            _settingsService.SaveTimeTopSetting(_setting);
        }

        partial void OnIdleOpacityChanged(double value)
        {
            if (_isInitializing) return;
            _setting.Behavior.IdleOpacity = value;
            _settingsService.SaveTimeTopSetting(_setting);
        }

        partial void OnStateStylesEnabledChanged(bool value)
        {
            if (_isInitializing) return;
            _setting.StateStyles.Enabled = value;
            SaveAndRefresh();
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
        private Models.TimeTopSetting _setting;
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
            if (Enum.TryParse<Models.TopmostMode>(value, out var mode))
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

        private static Models.ProgressBarPosition ParsePosition(string value)
        {
            return value?.ToLowerInvariant() switch
            {
                "bottom" => Models.ProgressBarPosition.Bottom,
                "left" => Models.ProgressBarPosition.Left,
                "right" => Models.ProgressBarPosition.Right,
                _ => Models.ProgressBarPosition.Top
            };
        }

        private static string PositionToConfigString(Models.ProgressBarPosition position)
        {
            return position switch
            {
                Models.ProgressBarPosition.Bottom => "bottom",
                Models.ProgressBarPosition.Left => "left",
                Models.ProgressBarPosition.Right => "right",
                _ => "top"
            };
        }
    }

    /// <summary>
    /// 设置窗口主 ViewModel
    /// 职责：设置页面导航 + 子页面 ViewModel 缓存
    /// </summary>
    public partial class TimeTopSettingViewModel : ObservableObject
    {
        private const string TAG_BASIC = "Basic";
        private const string TAG_APPEARANCE = "Appearance";
        private const string TAG_TIME = "Time";
        private const string TAG_WINDOW = "Window";
        private const string TAG_ABOUT = "About";

        private readonly ISettingsService _settingsService;
        private readonly IDesktopWindowManager _desktopWindowManager;
        private readonly ITimeService? _timeService;
        private readonly ITimeCalibrationService? _timeCalibrationService;

        [ObservableProperty]
        private object? _currentPage;

        private BasicPageViewModel? _basicPage;
        private AppearancePageViewModel? _appearancePage;
        private TimePageViewModel? _timePage;
        private WindowPageViewModel? _windowPage;
        private AboutPageViewModel? _aboutPage;

        public TimeTopSettingViewModel(
            ISettingsService settingsService,
            IDesktopWindowManager desktopWindowManager,
            ITimeService? timeService = null,
            ITimeCalibrationService? timeCalibrationService = null)
        {
            _settingsService = settingsService;
            _desktopWindowManager = desktopWindowManager;
            _timeService = timeService;
            _timeCalibrationService = timeCalibrationService;
        }

        /// <summary>
        /// 初始化导航（首次进入基本页面）
        /// </summary>
        public void InitializeNavigation()
        {
            NavigateTo(TAG_BASIC);
        }

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

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _timePage?.Dispose();
            _timePage = null;
            _basicPage = null;
            _appearancePage = null;
            _windowPage = null;
            _aboutPage = null;
        }
    }
}