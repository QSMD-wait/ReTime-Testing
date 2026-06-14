using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Helpers;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.TimeTopDesktop
{
    /// <summary>
    /// 顶部进度条窗口
    /// </summary>
    public partial class TimeTopDesktop : Window
    {
        private TimeTopDesktopViewModel? _viewModel;
        private TextOverlayViewModel? _textOverlayViewModel;

        /// <summary>
        /// 窗口位置：顶部
        /// </summary>
        public ProgressBarPosition Position => ProgressBarPosition.Top;

        public TimeTopDesktop()
        {
            InitializeComponent();

            var app = System.Windows.Application.Current as App;
            var services = app?.Services ?? throw new InvalidOperationException("DI 容器未初始化");

            // 通过 DI 创建 ViewModel
            _viewModel = services.GetRequiredService<TimeTopDesktopViewModel>();
            DataContext = _viewModel;

            // 创建文字覆盖 ViewModel
            var settingsService = services.GetRequiredService<ISettingsService>();
            var scheduleManager = services.GetRequiredService<IScheduleManager>();
            var timeService = services.GetRequiredService<ITimeService>();

            var resolver = new TextSlotResolver(scheduleManager, timeService);
            _textOverlayViewModel = new TextOverlayViewModel(resolver, settingsService);
            _viewModel.TextOverlay = _textOverlayViewModel;

            DesktopWindowHelper.ApplyStandardStyles(this);
            DesktopWindowHelper.SetWindowPosition(this, Position);

            Closing += TimeTopDesktop_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            DesktopWindowHelper.SetToolWindowStyle(this);
            WindowHelper.SetClickThrough(this);
        }

        private void TimeTopDesktop_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel?.Cleanup();
            _textOverlayViewModel?.Dispose();
        }
    }
}