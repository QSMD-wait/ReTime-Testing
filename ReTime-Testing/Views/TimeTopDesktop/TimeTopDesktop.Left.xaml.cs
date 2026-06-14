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
    /// 左侧进度条窗口
    /// </summary>
    public partial class TimeTopDesktop_Left : Window
    {
        private TimeTopDesktopViewModel? _viewModel;
        private TextOverlayViewModel? _textOverlayViewModel;

        /// <summary>
        /// 窗口位置：左侧
        /// </summary>
        public ProgressBarPosition Position => ProgressBarPosition.Left;

        public TimeTopDesktop_Left()
        {
            InitializeComponent();

            var app = System.Windows.Application.Current as App;
            var services = app?.Services ?? throw new InvalidOperationException("DI 容器未初始化");

            _viewModel = services.GetRequiredService<TimeTopDesktopViewModel>();
            DataContext = _viewModel;

            var settingsService = services.GetRequiredService<ISettingsService>();
            var scheduleManager = services.GetRequiredService<IScheduleManager>();
            var timeService = services.GetRequiredService<ITimeService>();

            var resolver = new TextSlotResolver(scheduleManager, timeService);
            _textOverlayViewModel = new TextOverlayViewModel(resolver, settingsService);
            _viewModel.TextOverlay = _textOverlayViewModel;

            DesktopWindowHelper.ApplyStandardStyles(this);
            DesktopWindowHelper.SetWindowPosition(this, Position);

            Closing += TimeTopDesktop_Left_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            DesktopWindowHelper.SetToolWindowStyle(this);
        }

        private void TimeTopDesktop_Left_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel?.Cleanup();
            _textOverlayViewModel?.Dispose();
        }
    }
}