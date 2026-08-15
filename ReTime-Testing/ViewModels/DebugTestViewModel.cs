using CommunityToolkit.Mvvm.ComponentModel;
using ReTime_Testing.Models.UI;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels.Testing;
using System;
using System.Collections.Generic;

namespace ReTime_Testing.ViewModels
{
    /// <summary>
    /// 调试测试窗口 ViewModel（窗口外壳）
    /// 职责：组织各功能 Tab 页面，转发 Toast 事件
    /// </summary>
    public partial class DebugTestViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _selectedTabIndex;
        /// <summary>
        /// 首页：服务状态总览 + 快捷操作
        /// </summary>
        public HomePageViewModel HomePage { get; }

        /// <summary>
        /// 主功能测试：进度条核心功能模拟
        /// </summary>
        public MainFeatureViewModel MainFeature { get; }

        /// <summary>
        /// 接口调试：互斥锁、位置、时间校准、执行计划、调度
        /// </summary>
        public ServiceDebugViewModel ServiceDebug { get; }

        /// <summary>
        /// 控件测试：Toast 等
        /// </summary>
        public ControlsViewModel Controls { get; }

        /// <summary>
        /// Tab 集合（供 TabControl 绑定，Header 使用各页 TabTitle）
        /// </summary>
        public IReadOnlyList<object> Tabs { get; }

        /// <summary>
        /// Toast 显示请求事件（由窗口代码后置处理）
        /// </summary>
        public event Action<ToastMessage>? ToastRequested;

        public DebugTestViewModel(
            IGlobalTimeTopDesktopService globalService,
            IMutexManager mutexManager,
            ISettingsService settingsService,
            IDesktopWindowManager desktopWindowManager,
            IConfigurationManager configurationManager,
            ITimeService? timeService = null,
            IScheduleManager? scheduleManager = null)
        {
            HomePage = new HomePageViewModel(mutexManager, desktopWindowManager, configurationManager,
                timeService, scheduleManager);
            MainFeature = new MainFeatureViewModel(globalService);
            ServiceDebug = new ServiceDebugViewModel(mutexManager, settingsService, desktopWindowManager,
                timeService, scheduleManager);
            Controls = new ControlsViewModel();

            Controls.ToastRequested += OnControlsToastRequested;

            Tabs = new object[] { HomePage, MainFeature, ServiceDebug, Controls };
        }

        private void OnControlsToastRequested(ToastMessage message)
        {
            ToastRequested?.Invoke(message);
        }

        /// <summary>
        /// 资源清理
        /// </summary>
        public void Cleanup()
        {
            Controls.ToastRequested -= OnControlsToastRequested;
            HomePage.Cleanup();
            ServiceDebug.Cleanup();
        }
    }
}