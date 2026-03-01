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

        [ObservableProperty]
        private double _progressValue = 0;

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

        public TimeTopDesktopViewModel()
        {
            try
            {
                _service = GlobalTimeTopDesktopService.Instance;
                _service.OnStateChanged = OnStateChanged;
                Logger.Info("ReTime_Testing.ViewModels.TimeTopDesktopViewModel", "ViewModel 初始化完成");
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.ViewModels.TimeTopDesktopViewModel", "ViewModel 初始化失败", ex);
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
                    Logger.Error("ReTime_Testing.ViewModels.TimeTopDesktopViewModel", "OnStateChanged: 配置为 null");
                    return;
                }

                ProgressValue = config.Value;
                IsIndeterminate = config.IsIndeterminate;
                Foreground = config.Foreground ?? ProgressColors.DefaultBlue;
                Background = config.Background ?? Brushes.Transparent;
                Visibility = config.Visibility;
                IsEnabled = config.IsEnabled;
                Opacity = config.Opacity;
                Minimum = config.Minimum;
                Maximum = config.Maximum;
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.ViewModels.TimeTopDesktopViewModel", "OnStateChanged: 更新属性时发生异常", ex);
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
                    _service.OnStateChanged = null;
                    Logger.Info("ReTime_Testing.ViewModels.TimeTopDesktopViewModel", "Service 回调已清理");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.ViewModels.TimeTopDesktopViewModel", "清理资源时发生异常", ex);
            }
        }
    }
}