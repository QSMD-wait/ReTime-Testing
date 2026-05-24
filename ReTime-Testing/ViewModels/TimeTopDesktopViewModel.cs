using CommunityToolkit.Mvvm.ComponentModel;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.ViewModels
{
    public partial class TimeTopDesktopViewModel : ObservableObject
    {
        private readonly GlobalTimeTopDesktopService _service;
        private readonly IConfigurationManager? _configManager;

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

        public TimeTopDesktopViewModel(IConfigurationManager? configManager = null)
        {
            try
            {
                _configManager = configManager ?? ConfigurationManager.Instance;
                _service = GlobalTimeTopDesktopService.Instance;
                _service.OnStateChanged = OnStateChanged;
                
                // 从配置中加载阴影设置
                var setting = _configManager?.LoadTimeTopSetting();
                if (setting != null)
                {
                    // 默认使用progressBar域中的设置
                    EnableShadow = setting.ProgressBar.EnableShadow;
                }
                
                Logger.Info("ReTime_Testing.ViewModels.TimeTopDesktopViewModel" ?? "TimeTopDesktopViewModel", "ViewModel 初始化完成");
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.ViewModels.TimeTopDesktopViewModel" ?? "TimeTopDesktopViewModel", "ViewModel 初始化失败", ex);
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
                    Logger.Error("ReTime_Testing.ViewModels.TimeTopDesktopViewModel" ?? "TimeTopDesktopViewModel", "OnStateChanged: 配置为 null");
                    return;
                }

                Logger.Info("TimeTopDesktopViewModel", $"UI更新: State={config.StateType}, Foreground={config.Foreground}, Background={config.Background}");

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
                Logger.Error("ReTime_Testing.ViewModels.TimeTopDesktopViewModel" ?? "TimeTopDesktopViewModel", "OnStateChanged: 更新属性时发生异常", ex);
            }
        }

        /// <summary>
        /// 根据当前状态更新阴影设置
        /// </summary>
        private void UpdateShadowBasedOnState(ProgressStateType stateType)
        {
            // 首先获取全局配置作为基础值
            var globalSetting = _configManager?.LoadTimeTopSetting();
            var baseEnableShadow = globalSetting?.ProgressBar.EnableShadow ?? true;
            
            // 检查状态特定配置是否覆盖
            if (_configManager != null && globalSetting != null && globalSetting.StateStyles.Enabled)
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
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            try
            {
                if (_service != null)
                {
                    _service.OnStateChanged = null;
                    Logger.Info("ReTime_Testing.ViewModels.TimeTopDesktopViewModel" ?? "TimeTopDesktopViewModel", "Service 回调已清理");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.ViewModels.TimeTopDesktopViewModel" ?? "TimeTopDesktopViewModel", "清理资源时发生异常", ex);
            }
        }
    }
}