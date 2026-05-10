using System.Windows;
using System.Windows.Threading;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 窗口层级维持服务
    /// 根据配置模式维护进度条窗口的置顶状态
    /// </summary>
    public class TopmostService
    {
        private static readonly Lazy<TopmostService> _instance =
            new Lazy<TopmostService>(() => new TopmostService());

        public static TopmostService Instance => _instance.Value;

        private Window? _targetWindow;
        private TopmostMode _currentMode = TopmostMode.OnDeactivated;
        private DispatcherTimer? _pollingTimer;

        private TopmostService() { }

        /// <summary>
        /// 应用层级维持模式到指定窗口
        /// </summary>
        public void Apply(Window window, TopmostMode mode)
        {
            // 先清理旧模式
            Cleanup();

            _targetWindow = window;
            _currentMode = mode;

            // 初始置顶
            window.Topmost = true;

            switch (mode)
            {
                case TopmostMode.None:
                    // 仅初始化置顶，不维护
                    Logger.Info("ReTime_Testing.Services.TopmostService",
                        "层级维持模式: None（仅初始化）");
                    break;

                case TopmostMode.OnDeactivated:
                    window.Deactivated += OnWindowDeactivated;
                    Logger.Info("ReTime_Testing.Services.TopmostService",
                        "层级维持模式: OnDeactivated（失活时重新置顶）");
                    break;

                case TopmostMode.Polling:
                    _pollingTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };
                    _pollingTimer.Tick += OnPollingTick;
                    _pollingTimer.Start();
                    Logger.Info("ReTime_Testing.Services.TopmostService",
                        "层级维持模式: Polling（500ms 定时轮询）");
                    break;
            }
        }

        /// <summary>
        /// 清理当前模式的资源和事件订阅
        /// </summary>
        public void Cleanup()
        {
            if (_targetWindow != null)
            {
                _targetWindow.Deactivated -= OnWindowDeactivated;
            }

            if (_pollingTimer != null)
            {
                _pollingTimer.Stop();
                _pollingTimer.Tick -= OnPollingTick;
                _pollingTimer = null;
            }

            _targetWindow = null;
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            if (_targetWindow != null && _currentMode == TopmostMode.OnDeactivated)
            {
                _targetWindow.Topmost = false;
                _targetWindow.Topmost = true;
            }
        }

        private void OnPollingTick(object? sender, EventArgs e)
        {
            if (_targetWindow != null && _currentMode == TopmostMode.Polling)
            {
                _targetWindow.Topmost = false;
                _targetWindow.Topmost = true;
            }
        }
    }
}
