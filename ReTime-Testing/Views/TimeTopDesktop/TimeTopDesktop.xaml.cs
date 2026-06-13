using System;
using System.Windows;
using System.Windows.Threading;
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

            // 创建主 ViewModel
            _viewModel = new TimeTopDesktopViewModel();
            DataContext = _viewModel;

            // 创建文字覆盖 ViewModel（通过 App 属性获取服务实例）
            var app = System.Windows.Application.Current as App;
            var configManager = Services.SettingsService.Instance;
            var scheduleManager = app?.ScheduleManager
                ?? throw new InvalidOperationException("ScheduleManager 未初始化");
            var timeService = app?.TimeService
                ?? throw new InvalidOperationException("TimeService 未初始化");

            var resolver = new TextSlotResolver(scheduleManager, timeService);
            _textOverlayViewModel = new TextOverlayViewModel(resolver, configManager);
            _viewModel.TextOverlay = _textOverlayViewModel;

            // 应用标准样式
            DesktopWindowHelper.ApplyStandardStyles(this);

            // 设置窗口位置
            DesktopWindowHelper.SetWindowPosition(this, Position);

            // 注册窗口关闭事件
            Closing += TimeTopDesktop_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 设置工具窗口样式（必须在 OnSourceInitialized 之后调用）
            DesktopWindowHelper.SetToolWindowStyle(this);

            // 设置点击穿透（所有点击直接穿透到下层窗口）
            WindowHelper.SetClickThrough(this);
        }

        private void TimeTopDesktop_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 清理 ViewModel 资源
            _viewModel?.Cleanup();
            _textOverlayViewModel?.Dispose();
        }
    }
}