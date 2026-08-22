using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels
{
    /// <summary>
    /// 首次启动欢迎引导 ViewModel
    /// 职责：引导步骤导航 + 引导临时值管理 + 配置项即时应用与落盘
    /// </summary>
    public partial class WelcomeViewModel : ObservableObject
    {
        /// <summary>
        /// 引导步骤定义
        /// </summary>
        public enum WelcomeStep
        {
            Welcome = 0,
            License = 1,
            Theme = 2,
            Basic = 3,
            Appearance = 4,
            TextOverlay = 5,
            Finish = 6
        }

        private const int StepCount = 7;

        /// <summary>
        /// 进度条缩放范围（与配置校验一致）
        /// </summary>
        public const double ScaleMinimum = 0.5;
        public const double ScaleMaximum = 3.0;

        /// <summary>
        /// 文字大小范围（与设置页文字栏一致）
        /// </summary>
        public const double FontSizeMinimum = 6;
        public const double FontSizeMaximum = 48;

        private readonly ISettingsService _settingsService;
        private readonly IThemeService _themeService;
        private readonly IDesktopWindowManager _desktopWindowManager;
        private readonly IAutoStartService _autoStartService;

        /// <summary>
        /// 构造初始化期间抑制即时应用（避免构造时触发保存/服务调用）
        /// </summary>
        private bool _isInitializing = true;

        /// <summary>
        /// 是否已完整走完引导（完成命令触发）
        /// </summary>
        [ObservableProperty]
        private bool _isCompleted;

        /// <summary>
        /// 当前步骤索引
        /// </summary>
        [ObservableProperty]
        private int _currentIndex;

        /// <summary>
        /// 是否已同意许可协议（许可页门控）
        /// </summary>
        [ObservableProperty]
        private bool _hasAcceptedLicense;

        /// <summary>
        /// 主题选择: light（浅色）, dark（深色）
        /// </summary>
        [ObservableProperty]
        private string _selectedTheme = "light";

        /// <summary>
        /// 进度条位置: top / bottom / left / right
        /// </summary>
        [ObservableProperty]
        private string _selectedPosition = "top";

        /// <summary>
        /// 是否启用开机自启
        /// </summary>
        [ObservableProperty]
        private bool _enableAutoStart;

        /// <summary>
        /// 是否启用云端时间校准
        /// </summary>
        [ObservableProperty]
        private bool _enableCalibration = true;

        /// <summary>
        /// 进度条缩放（0.5 ~ 3.0）
        /// </summary>
        [ObservableProperty]
        private double _progressScale = 1.0;

        /// <summary>
        /// 是否启用进度条阴影
        /// </summary>
        [ObservableProperty]
        private bool _enableProgressShadow = true;

        /// <summary>
        /// 是否启用流畅优化
        /// </summary>
        [ObservableProperty]
        private bool _enableSmoothness;

        /// <summary>
        /// 是否启用文字栏
        /// </summary>
        [ObservableProperty]
        private bool _enableTextOverlay = true;

        /// <summary>
        /// 文字大小（6 ~ 48）
        /// </summary>
        [ObservableProperty]
        private double _textFontSize = 12;

        /// <summary>
        /// 文字效果: none=无效果, shadow=阴影, outline=描边
        /// </summary>
        [ObservableProperty]
        private string _selectedTextEffect = "shadow";

        /// <summary>
        /// 主题显示文本
        /// </summary>
        public string SelectedThemeText => SelectedTheme == "dark" ? "暗黑" : "明亮";

        /// <summary>
        /// 位置显示文本
        /// </summary>
        public string SelectedPositionText => SelectedPosition switch
        {
            "bottom" => "底部",
            "left" => "左侧",
            "right" => "右侧",
            _ => "顶部"
        };

        /// <summary>
        /// 开机自启显示文本
        /// </summary>
        public string AutoStartText => EnableAutoStart ? "开启" : "关闭";

        /// <summary>
        /// 云端校准显示文本
        /// </summary>
        public string CalibrationText => EnableCalibration ? "开启" : "关闭";

        /// <summary>
        /// 文字效果显示文本
        /// </summary>
        public string SelectedTextEffectText => SelectedTextEffect switch
        {
            "outline" => "描边",
            "none" => "无效果",
            _ => "阴影"
        };

        /// <summary>
        /// 步骤指示文本，如 "步骤 3 / 7"
        /// </summary>
        public string StepText => $"步骤 {CurrentIndex + 1} / {StepCount}";

        /// <summary>
        /// 当前步骤是否为最后一页
        /// </summary>
        public bool IsLastPage => CurrentIndex >= StepCount - 1;

        /// <summary>
        /// 是否可以上一步
        /// </summary>
        public bool CanGoBack => CurrentIndex > 0;

        /// <summary>
        /// 是否可以下一步（许可页需先同意协议）
        /// </summary>
        public bool CanGoNext =>
            CurrentIndex < StepCount - 1 &&
            !(CurrentIndex == (int)WelcomeStep.License && !HasAcceptedLicense);

        public WelcomeViewModel(
            ISettingsService settingsService,
            IThemeService themeService,
            IDesktopWindowManager desktopWindowManager,
            IAutoStartService autoStartService)
        {
            _settingsService = settingsService;
            _themeService = themeService;
            _desktopWindowManager = desktopWindowManager;
            _autoStartService = autoStartService;

            // 从现有配置初始化引导默认值
            try
            {
                var globalSetting = _settingsService.GetGlobalSetting();
                SelectedTheme = string.Equals(globalSetting.Basic.Theme, "dark", StringComparison.OrdinalIgnoreCase)
                    ? "dark" : "light";
                EnableAutoStart = globalSetting.Basic.AutoStart.Enabled;
                EnableSmoothness = globalSetting.Basic.SmoothnessOptimization;

                var timeTopSetting = _settingsService.GetTimeTopSetting();
                SelectedPosition = timeTopSetting.ProgressBar.Position ?? "top";
                EnableCalibration = timeTopSetting.Calibration.Enabled;
                ProgressScale = Math.Clamp(timeTopSetting.ProgressBar.Scale, ScaleMinimum, ScaleMaximum);
                EnableProgressShadow = timeTopSetting.ProgressBar.EnableShadow;
                EnableTextOverlay = timeTopSetting.TextOverlay.Enabled;
                TextFontSize = Math.Clamp(timeTopSetting.TextOverlay.Style.FontSize, FontSizeMinimum, FontSizeMaximum);
                SelectedTextEffect = NormalizeTextEffect(timeTopSetting.TextOverlay.Style.TextEffect);

                Logger.Info("WelcomeViewModel", "欢迎引导初始化完成");
            }
            catch (Exception ex)
            {
                Logger.Error("WelcomeViewModel", $"欢迎引导初始化失败: {ex.Message}", ex);
            }
            finally
            {
                _isInitializing = false;
            }
        }

        partial void OnHasAcceptedLicenseChanged(bool value)
        {
            OnPropertyChanged(nameof(CanGoNext));
        }

        partial void OnSelectedThemeChanged(string value)
        {
            if (_isInitializing || IsCompleted) return;
            try
            {
                _themeService.ApplyTheme(value);
            }
            catch (Exception ex)
            {
                Logger.Warn("WelcomeViewModel", $"主题即时预览失败: {ex.Message}");
            }

            OnPropertyChanged(nameof(SelectedThemeText));
        }

        partial void OnSelectedPositionChanged(string value)
        {
            if (_isInitializing || IsCompleted) return;
            try
            {
                var position = ParsePosition(value);
                _desktopWindowManager.SetPosition(position);
            }
            catch (Exception ex)
            {
                Logger.Warn("WelcomeViewModel", $"位置即时预览失败: {ex.Message}");
            }

            OnPropertyChanged(nameof(SelectedPositionText));
        }

        partial void OnEnableAutoStartChanged(bool value)
        {
            if (_isInitializing || IsCompleted) return;
            try
            {
                if (value)
                    _autoStartService.Enable("registry");
                else
                    _autoStartService.Disable();
            }
            catch (Exception ex)
            {
                Logger.Warn("WelcomeViewModel", $"自启动即时应用失败: {ex.Message}");
            }

            OnPropertyChanged(nameof(AutoStartText));
        }

        partial void OnEnableCalibrationChanged(bool value)
        {
            OnPropertyChanged(nameof(CalibrationText));
            if (_isInitializing || IsCompleted) return;
            SaveTimeTop(t => t.Calibration.Enabled = value, "云端校准");
        }

        partial void OnProgressScaleChanged(double value)
        {
            if (_isInitializing || IsCompleted) return;
            var clamped = Math.Clamp(value, ScaleMinimum, ScaleMaximum);
            SaveTimeTop(t => t.ProgressBar.Scale = clamped, "进度条缩放");
        }

        partial void OnEnableProgressShadowChanged(bool value)
        {
            if (_isInitializing || IsCompleted) return;
            SaveTimeTop(t => t.ProgressBar.EnableShadow = value, "进度条阴影");
        }

        partial void OnEnableSmoothnessChanged(bool value)
        {
            if (_isInitializing || IsCompleted) return;
            try
            {
                var globalSetting = _settingsService.GetGlobalSetting();
                globalSetting.Basic.SmoothnessOptimization = value;
                _settingsService.SaveGlobalSetting(globalSetting);
            }
            catch (Exception ex)
            {
                Logger.Warn("WelcomeViewModel", $"流畅优化保存失败: {ex.Message}");
            }
        }

        partial void OnEnableTextOverlayChanged(bool value)
        {
            if (_isInitializing || IsCompleted) return;
            SaveTimeTop(t => t.TextOverlay.Enabled = value, "启用文字栏");
        }

        partial void OnTextFontSizeChanged(double value)
        {
            if (_isInitializing || IsCompleted) return;
            var clamped = Math.Clamp(value, FontSizeMinimum, FontSizeMaximum);
            SaveTimeTop(t => t.TextOverlay.Style.FontSize = clamped, "文字大小");
        }

        partial void OnSelectedTextEffectChanged(string value)
        {
            OnPropertyChanged(nameof(SelectedTextEffectText));
            if (_isInitializing || IsCompleted) return;
            SaveTimeTop(t => t.TextOverlay.Style.TextEffect = NormalizeTextEffect(value), "文字效果");
        }

        partial void OnCurrentIndexChanged(int value)
        {
            OnPropertyChanged(nameof(StepText));
            OnPropertyChanged(nameof(IsLastPage));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
        }

        /// <summary>
        /// 下一步
        /// </summary>
        [RelayCommand]
        private void Next()
        {
            if (!CanGoNext)
                return;

            CurrentIndex++;
        }

        /// <summary>
        /// 上一步
        /// </summary>
        [RelayCommand]
        private void Back()
        {
            if (CurrentIndex > 0)
                CurrentIndex--;
        }

        /// <summary>
        /// 完成引导：统一落盘全部选择
        /// </summary>
        [RelayCommand]
        private void Finish()
        {
            try
            {
                var globalSetting = _settingsService.GetGlobalSetting();
                globalSetting.Basic.Theme = SelectedTheme;
                globalSetting.Basic.AutoStart.Enabled = EnableAutoStart;
                globalSetting.Basic.SmoothnessOptimization = EnableSmoothness;
                globalSetting.Basic.WelcomeShowed = true;
                globalSetting.Basic.ForceShowWelcome = false;
                _settingsService.SaveGlobalSetting(globalSetting);

                var timeTopSetting = _settingsService.GetTimeTopSetting();
                timeTopSetting.ProgressBar.Position = PositionToConfigString(ParsePosition(SelectedPosition));
                timeTopSetting.ProgressBar.Scale = Math.Clamp(ProgressScale, ScaleMinimum, ScaleMaximum);
                timeTopSetting.ProgressBar.EnableShadow = EnableProgressShadow;
                timeTopSetting.Calibration.Enabled = EnableCalibration;
                timeTopSetting.TextOverlay.Enabled = EnableTextOverlay;
                timeTopSetting.TextOverlay.Style.FontSize = Math.Clamp(TextFontSize, FontSizeMinimum, FontSizeMaximum);
                timeTopSetting.TextOverlay.Style.TextEffect = NormalizeTextEffect(SelectedTextEffect);
                _settingsService.SaveTimeTopSetting(timeTopSetting);

                IsCompleted = true;

                Logger.Info("WelcomeViewModel", "欢迎引导完成，设置已保存");
            }
            catch (Exception ex)
            {
                Logger.Error("WelcomeViewModel", $"完成引导保存设置失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 修改 TimeTop 配置并立即保存（触发热更新，实现引导内实时预览）
        /// </summary>
        private void SaveTimeTop(Action<Models.TimeTopSetting> apply, string label)
        {
            try
            {
                var setting = _settingsService.GetTimeTopSetting();
                apply(setting);
                _settingsService.SaveTimeTopSetting(setting);
            }
            catch (Exception ex)
            {
                Logger.Warn("WelcomeViewModel", $"{label}保存失败: {ex.Message}");
            }
        }

        private static string NormalizeTextEffect(string? value)
        {
            return value switch
            {
                "none" => "none",
                "outline" => "outline",
                "shadow" => "shadow",
                _ => "shadow"
            };
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
}
