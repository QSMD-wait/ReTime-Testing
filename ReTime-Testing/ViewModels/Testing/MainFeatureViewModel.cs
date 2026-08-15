using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.ViewModels.Testing
{
    /// <summary>
    /// 主功能测试 ViewModel
    /// 职责：进度条核心功能模拟（状态、进度、样式、可见性等）
    /// </summary>
    public partial class MainFeatureViewModel : ObservableObject
    {
        private readonly IGlobalTimeTopDesktopService _service;

        public string TabTitle => "主功能";

        [ObservableProperty]
        private double _progressValue = 50;

        public MainFeatureViewModel(IGlobalTimeTopDesktopService globalService)
        {
            _service = globalService;
        }

        // ==================== 进度值变更 ====================

        partial void OnProgressValueChanged(double value)
        {
            _service.SetValue(value);
        }

        // ==================== 状态控制命令 ====================

        [RelayCommand]
        private void SetLoading() => _service.SetLoading();

        [RelayCommand]
        private void SetSuccess() => _service.SetSuccess();

        [RelayCommand]
        private void SetError() => _service.SetError();

        [RelayCommand]
        private void SetPaused() => _service.SetPaused();

        [RelayCommand]
        private void SetProgress() => _service.SetProgress(ProgressValue);

        [RelayCommand]
        private void SetHidden() => _service.SetHidden();

        [RelayCommand]
        private void SetDisabled() => _service.SetDisabled();

        // ==================== 可见性控制命令 ====================

        [RelayCommand]
        private void SetVisibilityVisible() => _service.SetVisibility(Visibility.Visible);

        [RelayCommand]
        private void SetVisibilityHidden() => _service.SetVisibility(Visibility.Hidden);

        [RelayCommand]
        private void SetVisibilityCollapsed() => _service.SetVisibility(Visibility.Collapsed);

        // ==================== 启用状态控制命令 ====================

        [RelayCommand]
        private void SetEnabledTrue() => _service.SetEnabled(true);

        [RelayCommand]
        private void SetEnabledFalse() => _service.SetEnabled(false);

        // ==================== 透明度控制命令 ====================

        [RelayCommand]
        private void SetOpacityFull() => _service.SetOpacity(1.0);

        [RelayCommand]
        private void SetOpacityHalf() => _service.SetOpacity(0.5);

        [RelayCommand]
        private void SetOpacityLow() => _service.SetOpacity(0.2);

        // ==================== 前景色控制命令 ====================

        [RelayCommand]
        private void SetForegroundBlue() => _service.SetForeground(ProgressColors.DefaultBlue);

        [RelayCommand]
        private void SetForegroundGreen() => _service.SetForeground(ProgressColors.SuccessGreen);

        [RelayCommand]
        private void SetForegroundRed() => _service.SetForeground(ProgressColors.ErrorRed);

        [RelayCommand]
        private void SetForegroundOrange() => _service.SetForeground(ProgressColors.PauseOrange);

        [RelayCommand]
        private void SetForegroundGray() => _service.SetForeground(ProgressColors.Gray);

        // ==================== 背景色控制命令 ====================

        [RelayCommand]
        private void SetBackgroundTransparent() => _service.SetBackground(Brushes.Transparent);

        [RelayCommand]
        private void SetBackgroundLightGray() => _service.SetBackground(Brushes.LightGray);

        [RelayCommand]
        private void SetBackgroundWhite() => _service.SetBackground(Brushes.White);

        // ==================== 范围控制命令 ====================

        [RelayCommand]
        private void SetRange0100() => _service.SetRange(0, 100);

        [RelayCommand]
        private void SetRange01() => _service.SetRange(0, 1);

        // ==================== 样式优先级测试命令 ====================

        [RelayCommand]
        private void ApplyDefaultStyle()
        {
            _service.SetForeground(ProgressColors.DefaultBlue);
            _service.SetOpacity(1.0);
            _service.SetVisibility(Visibility.Visible);
        }

        [RelayCommand]
        private void ApplyConfigStyle()
        {
            _service.SetForeground(new SolidColorBrush(Color.FromRgb(0x2D, 0x7D, 0x9A)));
            _service.SetOpacity(0.9);
        }

        [RelayCommand]
        private void ApplyScheduleStyle()
        {
            _service.SetForeground(new SolidColorBrush(Color.FromRgb(0xFF, 0x57, 0x33)));
            _service.SetOpacity(1.0);
        }

        // ==================== 重置与批量更新 ====================

        [RelayCommand]
        private void ResetState() => _service.Reset();

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
    }
}