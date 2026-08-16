using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels
{
    /// <summary>
    /// 首次启动欢迎引导 ViewModel
    /// 职责：引导步骤导航 + 引导临时值管理 + 完成时统一落盘
    /// </summary>
    public partial class WelcomeViewModel : ObservableObject
    {
        /// <summary>
        /// 引导步骤定义
        /// </summary>
        public enum WelcomeStep
        {
            Welcome = 0,
            Theme = 1,
            Position = 2,
            AutoStart = 3,
            Calibration = 4,
            Finish = 5
        }

        private const int StepCount = 6;

        private readonly ISettingsService _settingsService;
        private readonly IThemeService _themeService;
        private readonly IDesktopWindowManager _desktopWindowManager;
        private readonly IAutoStartService _autoStartService;

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
        /// 是否启用时间校准
        /// </summary>
        [ObservableProperty]
        private bool _enableCalibration = true;

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
        /// 时间校准显示文本
        /// </summary>
        public string CalibrationText => EnableCalibration ? "开启" : "关闭";

        /// <summary>
        /// 步骤指示文本，如 "步骤 3 / 6"
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
        /// 是否可以下一步
        /// </summary>
        public bool CanGoNext => CurrentIndex < StepCount - 1;

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

                var timeTopSetting = _settingsService.GetTimeTopSetting();
                SelectedPosition = timeTopSetting.ProgressBar.Position ?? "top";
                EnableCalibration = timeTopSetting.Calibration.Enabled;

                Logger.Info("WelcomeViewModel", "欢迎引导初始化完成");
            }
            catch (Exception ex)
            {
                Logger.Error("WelcomeViewModel", $"欢迎引导初始化失败: {ex.Message}", ex);
            }
        }

        partial void OnSelectedThemeChanged(string value)
        {
            if (IsCompleted) return;
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
            if (IsCompleted) return;
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
        if (IsCompleted) return;
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
            if (CurrentIndex < StepCount - 1)
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
                globalSetting.Basic.WelcomeShowed = true;
                globalSetting.Basic.ForceShowWelcome = false;
                _settingsService.SaveGlobalSetting(globalSetting);

                var timeTopSetting = _settingsService.GetTimeTopSetting();
                timeTopSetting.ProgressBar.Position = PositionToConfigString(ParsePosition(SelectedPosition));
                timeTopSetting.Calibration.Enabled = EnableCalibration;
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