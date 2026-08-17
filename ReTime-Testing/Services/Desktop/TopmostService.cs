using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 窗口层级维持服务
    /// 根据配置模式维护进度条窗口的置顶状态
    /// </summary>
    public class TopmostService : ITopmostService
    {

        private Window? _targetWindow;
        private TopmostMode _currentMode = TopmostMode.OnDeactivated;
        private DispatcherTimer? _pollingTimer;
        private HwndSource? _hwndSource;
        private DateTime _lastForceTopmostTime = DateTime.MinValue;
        private const int DEBOUNCE_INTERVAL_MS = 50;
        private IntPtr _winEventHook = IntPtr.Zero;
        private WinEventDelegate? _winEventDelegate;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOOWNERZORDER = 0x0200;

        private const uint EVENT_OBJECT_REORDER = 0x8004;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

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
                    window.Activated += OnWindowActivated;
                    window.LocationChanged += OnWindowLocationChanged;
                    window.SizeChanged += OnWindowSizeChanged;
                    window.IsVisibleChanged += OnWindowIsVisibleChanged;
                    window.SourceInitialized += OnWindowSourceInitialized;
                    
                    SubscribeWindowMessages();
                    SetupZOrderHook();
                    
                    Logger.Info("ReTime_Testing.Services.TopmostService",
                        "层级维持模式: OnDeactivated（窗口层级变化时）");
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
                _targetWindow.Activated -= OnWindowActivated;
                _targetWindow.LocationChanged -= OnWindowLocationChanged;
                _targetWindow.SizeChanged -= OnWindowSizeChanged;
                _targetWindow.IsVisibleChanged -= OnWindowIsVisibleChanged;
                _targetWindow.SourceInitialized -= OnWindowSourceInitialized;
            }

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }

            if (_pollingTimer != null)
            {
                _pollingTimer.Stop();
                _pollingTimer.Tick -= OnPollingTick;
                _pollingTimer = null;
            }

            if (_winEventHook != IntPtr.Zero)
            {
                UnhookWinEvent(_winEventHook);
                _winEventHook = IntPtr.Zero;
                _winEventDelegate = null;
                Logger.Info("ReTime_Testing.Services.TopmostService", "Z-order 钩子已清理");
            }

            _targetWindow = null;
            _lastForceTopmostTime = DateTime.MinValue;
        }

        /// <summary>
        /// 订阅 Windows 消息
        /// </summary>
        private void SubscribeWindowMessages()
        {
            if (_targetWindow == null) return;

            try
            {
                var helper = new WindowInteropHelper(_targetWindow);
                Logger.Info("ReTime_Testing.Services.TopmostService", 
                    $"尝试订阅 Windows 消息，窗口句柄: {helper.Handle}");
                
                if (helper.Handle != IntPtr.Zero)
                {
                    _hwndSource = HwndSource.FromHwnd(helper.Handle);
                    if (_hwndSource != null)
                    {
                        _hwndSource.AddHook(WndProc);
                        Logger.Info("ReTime_Testing.Services.TopmostService", 
                            "Windows 消息钩子订阅成功");
                    }
                    else
                    {
                        Logger.Warn("ReTime_Testing.Services.TopmostService", 
                            "HwndSource.FromHwnd 返回 null");
                    }
                }
                else
                {
                    Logger.Warn("ReTime_Testing.Services.TopmostService", 
                        "窗口句柄为 0，无法订阅消息");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TopmostService", "订阅 Windows 消息失败", ex);
            }
        }

        /// <summary>
        /// 设置 Z-order 监听钩子
        /// </summary>
        private void SetupZOrderHook()
        {
            try
            {
                _winEventDelegate = new WinEventDelegate(OnWinEvent);
                _winEventHook = SetWinEventHook(
                    EVENT_OBJECT_REORDER,
                    EVENT_OBJECT_REORDER,
                    IntPtr.Zero,
                    _winEventDelegate,
                    0,
                    0,
                    WINEVENT_OUTOFCONTEXT);

                if (_winEventHook != IntPtr.Zero)
                {
                    Logger.Info("ReTime_Testing.Services.TopmostService", "Z-order 监听钩子设置成功");
                }
                else
                {
                    Logger.Error("ReTime_Testing.Services.TopmostService", "Z-order 监听钩子设置失败");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TopmostService", "设置 Z-order 监听钩子失败", ex);
            }
        }

        /// <summary>
        /// Windows 事件回调（Z-order 改变）
        /// </summary>
        private void OnWinEvent(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (_currentMode != TopmostMode.OnDeactivated || _targetWindow == null)
            {
                return;
            }

            if (eventType == EVENT_OBJECT_REORDER)
            {
                ForceTopmostAsync();
            }
        }

        /// <summary>
        /// Windows 消息钩子
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_currentMode != TopmostMode.OnDeactivated || _targetWindow == null)
            {
                return IntPtr.Zero;
            }

            const int WM_ACTIVATEAPP = 0x001C;
            const int WM_WINDOWPOSCHANGED = 0x0047;
            const int WM_ACTIVATE = 0x0006;
            const int WM_KILLFOCUS = 0x0008;
            const int WM_SETFOCUS = 0x0007;
            const int WM_SHOWWINDOW = 0x0018;
            const int WM_SIZE = 0x0005;
            const int WM_MOVE = 0x0003;

            bool shouldForceTopmost = false;
            string messageName = string.Empty;

            switch (msg)
            {
                case WM_ACTIVATEAPP:
                    messageName = "WM_ACTIVATEAPP";
                    shouldForceTopmost = true;
                    break;
                case WM_WINDOWPOSCHANGED:
                    messageName = "WM_WINDOWPOSCHANGED";
                    shouldForceTopmost = true;
                    break;
                case WM_ACTIVATE:
                    messageName = "WM_ACTIVATE";
                    shouldForceTopmost = true;
                    break;
                case WM_KILLFOCUS:
                    messageName = "WM_KILLFOCUS";
                    shouldForceTopmost = true;
                    break;
                case WM_SETFOCUS:
                    messageName = "WM_SETFOCUS";
                    shouldForceTopmost = true;
                    break;
                case WM_SHOWWINDOW:
                    messageName = "WM_SHOWWINDOW";
                    shouldForceTopmost = true;
                    break;
                case WM_SIZE:
                    messageName = "WM_SIZE";
                    shouldForceTopmost = true;
                    break;
                case WM_MOVE:
                    messageName = "WM_MOVE";
                    shouldForceTopmost = true;
                    break;
            }

            if (shouldForceTopmost)
            {
                ForceTopmostAsync();
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// 异步强制置顶窗口（带防抖）
        /// </summary>
        private void ForceTopmostAsync()
        {
            if (_targetWindow == null) return;

            var now = DateTime.Now;
            var elapsed = (now - _lastForceTopmostTime).TotalMilliseconds;

            if (elapsed < DEBOUNCE_INTERVAL_MS)
            {
                return;
            }

            _lastForceTopmostTime = now;

            _targetWindow.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    ForceTopmostUsingWin32();
                }
                catch (Exception ex)
                {
                    Logger.Error("ReTime_Testing.Services.TopmostService", "异步强制置顶失败", ex);
                }
            }, DispatcherPriority.Normal);
        }

        /// <summary>
        /// 强制置顶窗口（同步，带防抖）
        /// </summary>
        private void ForceTopmost()
        {
            if (_targetWindow == null) return;

            var now = DateTime.Now;
            var elapsed = (now - _lastForceTopmostTime).TotalMilliseconds;

            if (elapsed < DEBOUNCE_INTERVAL_MS)
            {
                return;
            }

            _lastForceTopmostTime = now;

            try
            {
                ForceTopmostUsingWin32();
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TopmostService", "强制置顶失败", ex);
            }
        }

        /// <summary>
        /// 强制置顶窗口（使用 Topmost 属性）
        /// </summary>
        private void ForceTopmostUsingWin32()
        {
            if (_targetWindow == null) return;

            try
            {
                _targetWindow.Topmost = false;
                _targetWindow.Topmost = true;
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TopmostService", "置顶失败", ex);
            }
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            Logger.Info("ReTime_Testing.Services.TopmostService", "OnWindowDeactivated 事件触发");
            if (_currentMode == TopmostMode.OnDeactivated)
            {
                ForceTopmost();
            }
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            Logger.Info("ReTime_Testing.Services.TopmostService", "OnWindowActivated 事件触发");
            if (_currentMode == TopmostMode.OnDeactivated)
            {
                ForceTopmost();
            }
        }

        private void OnWindowLocationChanged(object? sender, EventArgs e)
        {
            Logger.Info("ReTime_Testing.Services.TopmostService", "OnWindowLocationChanged 事件触发");
            if (_currentMode == TopmostMode.OnDeactivated)
            {
                ForceTopmost();
            }
        }

        private void OnWindowSizeChanged(object? sender, EventArgs e)
        {
            Logger.Info("ReTime_Testing.Services.TopmostService", "OnWindowSizeChanged 事件触发");
            if (_currentMode == TopmostMode.OnDeactivated)
            {
                ForceTopmost();
            }
        }

        private void OnWindowIsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            Logger.Info("ReTime_Testing.Services.TopmostService", 
                $"OnWindowIsVisibleChanged 事件触发，IsVisible: {_targetWindow?.IsVisible}");
            if (_currentMode == TopmostMode.OnDeactivated && _targetWindow != null && _targetWindow.IsVisible)
            {
                ForceTopmost();
            }
        }

        private void OnWindowSourceInitialized(object? sender, EventArgs e)
        {
            Logger.Info("ReTime_Testing.Services.TopmostService", "OnWindowSourceInitialized 事件触发");
            if (_currentMode == TopmostMode.OnDeactivated)
            {
                SubscribeWindowMessages();
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