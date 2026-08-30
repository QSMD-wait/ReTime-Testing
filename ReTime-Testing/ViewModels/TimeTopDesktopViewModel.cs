using CommunityToolkit.Mvvm.ComponentModel;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.ViewModels
{
    public partial class TimeTopDesktopViewModel : ObservableObject
    {
        private readonly ILogger<TimeTopDesktopViewModel> _logger;
        private readonly IGlobalTimeTopDesktopService _service;
        private readonly ISettingsService? _settingsService;

        [ObservableProperty]
        private double _progressValue = 0;

        [ObservableProperty]
        private TextOverlayViewModel? _textOverlay;

        [ObservableProperty]
        private bool _isIndeterminate = true;

        [ObservableProperty]
        private Brush? _foreground = ProgressColors.DefaultBlue;

        [ObservableProperty]
        private Brush? _background = Brushes.Transparent;

        [ObservableProperty]
        private Visibility _visibility = Visibility.Visible;

        [ObservableProperty]
        private bool _isEnabled = true;

        [ObservableProperty]
        private double _opacity = 1.0;

        [ObservableProperty]
        private double _minimum = 0;

        [ObservableProperty]
        private double _maximum = 100;

        [ObservableProperty]
        private bool _enableShadow = true;

        [ObservableProperty]
        private double _progressBarScale = 1.0;

        public TimeTopDesktopViewModel(
            ILogger<TimeTopDesktopViewModel> logger,
            IGlobalTimeTopDesktopService globalService,
            ISettingsService? settingsService = null)
        {
            // 赋值在 try 之外，确保 catch 块中日志器可用
            _logger = logger;
            try
            {
                _service = globalService;
                _settingsService = settingsService;
                _service.OnStateChanged += OnStateChanged;
                
                var setting = _settingsService?.GetTimeTopSetting();
                if (setting != null)
                {
                    ProgressBarScale = Math.Clamp(setting.ProgressBar.Scale, 0.5, 3.0);
                }

                // 初始阴影按当前状态判定（避免窗口创建瞬间闪过阴影）
                var initialState = _service.GetCurrentConfig()?.StateType ?? ProgressStateType.Loading;
                UpdateShadowBasedOnState(initialState);

                // 订阅全局配置变更（流畅优化热生效）
                if (_settingsService != null)
                {
                    _settingsService.OnGlobalSettingChanged += OnGlobalSettingChanged;
                }

                _logger.LogInformation("ViewModel 初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ViewModel 初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 状态变更回调
        /// </summary>
        private void OnStateChanged(ProgressStateConfig config)
        {
            try
            {
                if (config == null)
                {
                    _logger.LogError("OnStateChanged: 配置为 null");
                    return;
                }

                _logger.LogTrace("UI更新: State={StateType}, Foreground={Foreground}, Background={Background}", config.StateType, config.Foreground, config.Background);

                ProgressValue = config.Value;
                IsIndeterminate = config.IsIndeterminate;
                Foreground = config.Foreground ?? ProgressColors.DefaultBlue;
                Background = config.Background ?? Brushes.Transparent;
                Visibility = config.Visibility;
                IsEnabled = config.IsEnabled;
                Opacity = config.Opacity;
                Minimum = config.Minimum;
                Maximum = config.Maximum;
                
                // 根据当前状态更新阴影设置
                UpdateShadowBasedOnState(config.StateType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnStateChanged: 更新属性时发生异常");
            }
        }

        /// <summary>
        /// 根据当前状态更新阴影设置
        /// </summary>
        private void UpdateShadowBasedOnState(ProgressStateType stateType)
        {
            // 流畅优化：Loading 状态强制关闭阴影（引导时由运行时标志强制，正常模式读配置）
            if (stateType == ProgressStateType.Loading && IsSmoothnessOptimizationActive)
            {
                EnableShadow = false;
                return;
            }

            // 首先获取全局配置作为基础值
            var globalSetting = _settingsService?.GetTimeTopSetting();
            var baseEnableShadow = globalSetting?.ProgressBar.EnableShadow ?? true;
            
            if (_settingsService != null && globalSetting != null && globalSetting.StateStyles.Enabled)
            {
                var stateName = stateType.ToString();
                if (globalSetting.StateStyles.Styles.ContainsKey(stateName))
                {
                    var stateStyle = globalSetting.StateStyles.Styles[stateName];
                    if (stateStyle.Enabled && stateStyle.EnableShadow.HasValue)
                    {
                        // 使用状态特定配置覆盖全局配置
                        EnableShadow = stateStyle.EnableShadow.Value;
                        return;
                    }
                }
            }
            
            // 使用全局配置作为默认值
            EnableShadow = baseEnableShadow;
        }

        /// <summary>
        /// 流畅优化是否激活：运行时强制标志（引导模式）或配置文件开关
        /// </summary>
        private bool IsSmoothnessOptimizationActive
        {
            get
            {
                if (_service.ForceSmoothnessOptimization)
                    return true;

                try
                {
                    return _settingsService?.GetGlobalSetting()?.Basic.SmoothnessOptimization == true;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 全局配置变更回调（流畅优化切换即时生效）
        /// </summary>
        private void OnGlobalSettingChanged(Models.GlobalSetting setting)
        {
            try
            {
                var stateType = _service.GetCurrentConfig()?.StateType ?? ProgressStateType.Loading;
                UpdateShadowBasedOnState(stateType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("全局配置变更重算阴影失败: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// 重新计算阴影状态（供热重载调用）
        /// </summary>
        public void RefreshShadow()
        {
            try
            {
                var stateType = _service.GetCurrentConfig()?.StateType ?? ProgressStateType.Loading;
                UpdateShadowBasedOnState(stateType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("刷新阴影失败: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            try
            {
                if (_service != null)
                {
                    _service.OnStateChanged -= OnStateChanged;
                    _logger.LogInformation("Service 回调已清理");
                }

                if (_settingsService != null)
                {
                    _settingsService.OnGlobalSettingChanged -= OnGlobalSettingChanged;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理资源时发生异常");
            }
        }
    }
}