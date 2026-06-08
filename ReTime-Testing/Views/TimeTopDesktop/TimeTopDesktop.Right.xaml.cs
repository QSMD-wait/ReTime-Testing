using System;
using System.Windows;
using ReTime_Testing.Helpers;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.TimeTopDesktop
{
    /// <summary>
    /// 右侧进度条窗口
    /// </summary>
    public partial class TimeTopDesktop_Right : Window
    {
        private TimeTopDesktopViewModel? _viewModel;
        private TextOverlayViewModel? _textOverlayViewModel;

        /// <summary>
        /// 窗口位置：右侧
        /// </summary>
        public ProgressBarPosition Position => ProgressBarPosition.Right;

        public TimeTopDesktop_Right()
        {
            InitializeComponent();
            _viewModel = new TimeTopDesktopViewModel();
            DataContext = _viewModel;

            var app = System.Windows.Application.Current as App;
            var configManager = Services.ConfigurationManager.Instance;
            var scheduleManager = app?.ScheduleManager
                ?? throw new InvalidOperationException("ScheduleManager 未初始化");
            var timeService = app?.TimeService
                ?? throw new InvalidOperationException("TimeService 未初始化");

            var resolver = new TextSlotResolver(scheduleManager, timeService);
            _textOverlayViewModel = new TextOverlayViewModel(resolver, configManager);
            _viewModel.TextOverlay = _textOverlayViewModel;

            DesktopWindowHelper.ApplyStandardStyles(this);
            DesktopWindowHelper.SetWindowPosition(this, Position);

            Closing += TimeTopDesktop_Right_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            DesktopWindowHelper.SetToolWindowStyle(this);
        }

        private void TimeTopDesktop_Right_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel?.Cleanup();
            _textOverlayViewModel?.Dispose();
        }
    }
}