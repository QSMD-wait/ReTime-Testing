using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.Onboarding
{
    /// <summary>
    /// 首次启动欢迎引导窗口
    /// 职责：引导步骤容器 + 底部导航栏 + 关闭守卫（未完成禁止关闭）
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        private WelcomeViewModel? _viewModel;
        private bool _allowClose;
        private bool _isDialogShowing;

        /// <summary>
        /// 引导是否已完整完成
        /// </summary>
        public bool IsWizardCompleted => _viewModel?.IsCompleted ?? false;

        public WelcomeWindow()
        {
            InitializeComponent();

            var app = System.Windows.Application.Current as App;
            var services = app?.Services ?? throw new InvalidOperationException("DI 容器未初始化");

            _viewModel = services.GetRequiredService<WelcomeViewModel>();
            DataContext = _viewModel;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Closing += WelcomeWindow_Closing;
        }

        /// <summary>
        /// 引导完成后自动关闭窗口
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(WelcomeViewModel.IsCompleted) && _viewModel?.IsCompleted == true)
            {
                _allowClose = true;
                Logger.Info("WelcomeWindow", "引导完成，关闭引导窗口");
                Close();
            }
        }

        /// <summary>
        /// 关闭守卫：引导未完成时禁止关闭，弹出确认对话框
        /// </summary>
        private async void WelcomeWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_allowClose || IsWizardCompleted)
                return;

            e.Cancel = true;

            if (_isDialogShowing)
                return;

            _isDialogShowing = true;

            var dialog = new iNKORE.UI.WPF.Modern.Controls.ContentDialog
            {
                Title = "尚未完成设置",
                Content = "您需要完成设置才能开始使用本应用。\n关闭此窗口将直接退出应用。",
                PrimaryButtonText = "退出应用",
                CloseButtonText = "继续设置",
                DefaultButton = iNKORE.UI.WPF.Modern.Controls.ContentDialogButton.Close,
                IsShadowEnabled = false
            };

            try
            {
                var result = await dialog.ShowAsync();
                if (result == iNKORE.UI.WPF.Modern.Controls.ContentDialogResult.Primary)
                {
                    Logger.Info("WelcomeWindow", "引导未完成，用户选择退出应用");
                    _allowClose = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("WelcomeWindow", $"关闭确认对话框异常: {ex.Message}");
            }
            finally
            {
                _isDialogShowing = false;
            }
        }
    }
}