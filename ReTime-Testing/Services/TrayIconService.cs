using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Hardcodet.Wpf.TaskbarNotification;
using ReTime_Testing.Helpers;
using ReTime_Testing.Services;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 系统托盘图标服务
    /// 管理应用程序的系统托盘图标和右键菜单
    /// </summary>
    public class TrayIconService : ITrayIconService
    {
        private static readonly Lazy<TrayIconService> _instance =
            new Lazy<TrayIconService>(() => new TrayIconService());

        public static TrayIconService Instance => _instance.Value;

        private Window? _trayIconWindow;
        private TaskbarIcon? _trayIcon;
        private bool _disposed = false;

        // 全局鼠标钩子
        private static IntPtr _hookId = IntPtr.Zero;
        private ContextMenu? _menuToClose;

        // 可配置的延迟关闭时间（毫秒）
        private const int MenuCloseDelayMs = 150;

        // P/Invoke
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        private static readonly LowLevelMouseProc _mouseProc = MouseHookCallback;

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                try
                {
                    var instance = _instance.Value;
                    if (instance._menuToClose != null && instance._menuToClose.IsOpen)
                    {
                        // 延迟关闭，给菜单时间处理点击
                        var timer = new System.Windows.Threading.DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(MenuCloseDelayMs)
                        };
                        timer.Tick += (s, args) =>
                        {
                            timer.Stop();
                            try
                            {
                                if (instance._menuToClose != null && instance._menuToClose.IsOpen)
                                {
                                    instance._menuToClose.IsOpen = false;
                                    instance._menuToClose = null;
                                }
                            }
                            catch { }
                        };
                        timer.Start();
                    }
                }
                catch { }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// 停止全局鼠标钩子
        /// </summary>
        private static void StopMouseHook()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 打开设置请求事件
        /// </summary>
        public event Action? OpenSettingRequested;

        /// <summary>
        /// 打开调试请求事件
        /// </summary>
        public event Action? OpenDebugRequested;

        /// <summary>
        /// 打开时间计划表编辑器请求事件
        /// </summary>
        public event Action? OpenTimeScheduleEditorRequested;

        /// <summary>
        /// 关于请求事件
        /// </summary>
        public event Action? AboutRequested;

        /// <summary>
        /// 退出请求事件
        /// </summary>
        public event Action? ExitRequested;

        /// <summary>
        /// 重启请求事件
        /// </summary>
        public event Action? RestartRequested;

/// <summary>
        /// 托盘图标服务配置
        /// </summary>
        public class TrayIconConfig
        {
            public string Title { get; set; } = "ReTime-Testing";
            public string? IconPath { get; set; }       // 外部文件路径
            public string? IconResource { get; set; }   // 内嵌资源名（如 Resources/app.ico）
        }

        /// <summary>
        /// 托盘图标配置
        /// </summary>
        private TrayIconConfig _config = new()
        {
            Title = "ReTime-Testing"
        };

        // 主题服务引用
        private IThemeService? _themeService;

        private TrayIconService()
        {
        }

        /// <summary>
        /// 初始化托盘图标服务
        /// </summary>
        public void Initialize(TrayIconConfig? config = null)
        {
            if (_trayIcon != null)
                return;

            _config = config ?? new TrayIconConfig();
            
            // 获取主题服务实例
            var app = Application.Current as App;
            _themeService = app?.ThemeService;
            
            InitializeIcon();
        }

        /// <summary>
        /// 初始化托盘图标
        /// </summary>
        private void InitializeIcon()
        {
            try
            {
                _trayIcon = new TaskbarIcon
                {
                    Icon = LoadIcon(),
                    ToolTipText = _config.Title,
                    Visibility = Visibility.Visible
                };

                SetupContextMenu();
                _trayIcon.TrayMouseDoubleClick += OnTrayMouseDoubleClick;
                _trayIcon.TrayLeftMouseDown += OnTrayLeftMouseDown;

                Logger.Info("TrayIconService", "系统托盘图标初始化成功（无窗口模式）");
            }
            catch (Exception ex)
            {
                Logger.Error("TrayIconService", "初始化系统托盘图标时发生异常", ex);
                Logger.Warn("TrayIconService", "无窗口模式失败，尝试使用窗口承载模式");
                InitializeWithWindow();
            }
        }

        /// <summary>
        /// 使用窗口承载模式初始化（备用方案）
        /// </summary>
        private void InitializeWithWindow()
        {
            try
            {
                _trayIconWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Width = 0,
                    Height = 0,
                    ShowInTaskbar = false,
                    ShowActivated = false,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };

                _trayIconWindow.Show();
                WindowHelper.SetToolWindowStyle(_trayIconWindow);

                _trayIcon = new TaskbarIcon
                {
                    Icon = LoadIcon(),
                    ToolTipText = _config.Title,
                    Visibility = Visibility.Visible
                };

                SetupContextMenu();
                _trayIcon.TrayMouseDoubleClick += OnTrayMouseDoubleClick;
                _trayIcon.TrayLeftMouseDown += OnTrayLeftMouseDown;
                _trayIconWindow.Content = _trayIcon;

                Logger.Info("TrayIconService", "系统托盘图标初始化成功（窗口承载模式）");
            }
            catch (Exception ex)
            {
                Logger.Error("TrayIconService", "窗口承载模式初始化失败", ex);
                throw;
            }
        }

/// <summary>
        /// 加载图标
        /// </summary>
        private Icon LoadIcon()
        {
            // 1. 尝试加载内嵌资源
            if (!string.IsNullOrEmpty(_config.IconResource))
            {
                try
                {
                    // 处理 pack URI 格式: pack://application:,,,/程序集名;component/资源路径
                    string uriStr;
                    if (_config.IconResource.Contains(";component/"))
                    {
                        uriStr = $"pack://application:,,,/{_config.IconResource}";
                    }
                    else
                    {
                        uriStr = $"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/{_config.IconResource}";
                    }

                    var uri = new Uri(uriStr);
                    var streamInfo = Application.GetResourceStream(uri);
                    if (streamInfo != null)
                    {
                        using var stream = streamInfo.Stream;
                        Logger.Info("TrayIconService", $"内嵌图标加载成功: {uriStr}");
                        return new Icon(stream);
                    }

                    Logger.Warn("TrayIconService", $"内嵌资源未找到: {uriStr}");
                }
                catch (Exception ex)
                {
                    Logger.Warn("TrayIconService", $"加载内嵌图标失败: {ex.Message}");
                }
            }

            // 2. 尝试加载外部文件
            if (!string.IsNullOrEmpty(_config.IconPath) && File.Exists(_config.IconPath))
            {
                try
                {
                    return new Icon(_config.IconPath);
                }
                catch (Exception ex)
                {
                    Logger.Warn("TrayIconService", $"加载自定义图标失败: {ex.Message}");
                }
            }

            // 3. 回退到系统默认图标
            Logger.Warn("TrayIconService", "使用系统默认图标");
            return SystemIcons.Application;
        }

        /// <summary>
        /// 设置右键菜单
        /// </summary>
        private void SetupContextMenu()
        {
            var contextMenu = new ContextMenu
            {
                Background = GetThemeBackgroundBrush(),
                BorderBrush = GetThemeBorderBrush(),
                BorderThickness = new Thickness(1)
            };

            // 1. 应用名称（顶部）
            contextMenu.Items.Add(CreateMenuItem("ReTime - Testing", "\uE946", () => AboutRequested?.Invoke()));
            contextMenu.Items.Add(new Separator()
            {
                Background = GetThemeSeparatorBrush()
            });

            // 2. 编辑时间计划
            contextMenu.Items.Add(CreateMenuItem("编辑时间计划", "\uE787", () => OpenTimeScheduleEditorRequested?.Invoke()));

            // 3. 设置
            contextMenu.Items.Add(CreateMenuItem("设置", "\uE713", () => OpenSettingRequested?.Invoke()));

            // 4. 调试
            contextMenu.Items.Add(CreateMenuItem("调试与测试", "\uE90F", () => OpenDebugRequested?.Invoke()));

            contextMenu.Items.Add(new Separator()
            {
                Background = GetThemeSeparatorBrush()
            });

            // 5. 重启
            contextMenu.Items.Add(CreateMenuItem("重启", "\uE72C", () => RestartRequested?.Invoke()));

            // 6. 退出
            contextMenu.Items.Add(CreateMenuItem("退出", "\uE711", () => ExitRequested?.Invoke()));

            _trayIcon!.ContextMenu = contextMenu;
        }

        /// <summary>
        /// 根据当前主题获取菜单背景画刷
        /// </summary>
        private System.Windows.Media.Brush GetThemeBackgroundBrush()
        {
            if (_themeService?.CurrentTheme == "dark")
            {
                // 使用深色背景 (#23292d)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x23, 0x29, 0x2D));
            }
            return System.Windows.Media.Brushes.White;
        }

        /// <summary>
        /// 根据当前主题获取菜单边框画刷
        /// </summary>
        private System.Windows.Media.Brush GetThemeBorderBrush()
        {
            if (_themeService?.CurrentTheme == "dark")
            {
                // 使用 Fluent Design 风格的深色边框 (#3D3D3D)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x3D, 0x3D, 0x3D));
            }
            return System.Windows.Media.Brushes.Gray;
        }

        /// <summary>
        /// 根据当前主题获取分割线画刷
        /// </summary>
        private System.Windows.Media.Brush GetThemeSeparatorBrush()
        {
            if (_themeService?.CurrentTheme == "dark")
            {
                // 使用 Fluent Design 风格的深色分割线 (#3D3D3D)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x3D, 0x3D, 0x3D));
            }
            return System.Windows.Media.Brushes.LightGray;
        }

        /// <summary>
        /// 创建菜单项
        /// </summary>
        private MenuItem CreateMenuItem(string header, string iconGlyph, Action click)
        {
            var menuItem = new MenuItem
            {
                Padding = new Thickness(8, 6, 8, 6),
                Background = GetThemeBackgroundBrush(),
                Foreground = GetThemeForegroundBrush()
            };

            var iconTextBlock = new TextBlock
            {
                Text = iconGlyph,
                FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                FontSize = 16,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            stackPanel.Children.Add(iconTextBlock);
            stackPanel.Children.Add(new TextBlock { Text = header, VerticalAlignment = VerticalAlignment.Center });

            menuItem.Header = stackPanel;
            menuItem.Click += (s, e) => click.Invoke();
            menuItem.CommandTarget = menuItem;

            return menuItem;
        }

        /// <summary>
        /// 根据当前主题获取菜单项前景色（文字颜色）
        /// </summary>
        private System.Windows.Media.Brush GetThemeForegroundBrush()
        {
            if (_themeService?.CurrentTheme == "dark")
            {
                // 使用纯白色文字
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            }
            return System.Windows.Media.Brushes.Black;
        }

        /// <summary>
        /// 左键双击事件处理
        /// </summary>
        private void OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            OpenSettingRequested?.Invoke();
        }

        /// <summary>
        /// 左键单击 - 显示上下文菜单
        /// </summary>
        private void OnTrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            if (_trayIcon?.ContextMenu != null)
            {
                // 更新菜单主题
                UpdateMenuTheme();

                _trayIcon.ContextMenu.IsOpen = true;
                _menuToClose = _trayIcon.ContextMenu;
                StartMouseHook();
            }
        }

        /// <summary>
        /// 更新菜单主题
        /// </summary>
        private void UpdateMenuTheme()
        {
            if (_trayIcon?.ContextMenu is ContextMenu menu)
            {
                menu.Background = GetThemeBackgroundBrush();
                menu.BorderBrush = GetThemeBorderBrush();

                foreach (var item in menu.Items)
                {
                    if (item is MenuItem menuItem)
                    {
                        menuItem.Background = GetThemeBackgroundBrush();
                        menuItem.Foreground = GetThemeForegroundBrush();
                    }
                    else if (item is Separator separator)
                    {
                        separator.Background = GetThemeSeparatorBrush();
                    }
                }
            }
        }

        /// <summary>
        /// 启动全局鼠标钩子
        /// </summary>
        private void StartMouseHook()
        {
            if (_hookId == IntPtr.Zero)
            {
                try
                {
                    using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
                    using var curModule = curProcess.MainModule;
                    if (curModule != null)
                    {
                        _hookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(curModule.ModuleName), 0);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("TrayIconService", "启动鼠标钩子失败", ex);
                }
            }
        }

        /// <summary>
        /// 显示气泡提示
        /// </summary>
        public void ShowBalloon(string title, string message, BalloonIcon icon = BalloonIcon.Info)
        {
            _trayIcon?.ShowBalloonTip(title, message, icon);
        }

        /// <summary>
        /// 接口显式实现：显示气泡提示
        /// </summary>
        void ITrayIconService.ShowBalloon(string title, string message) => ShowBalloon(title, message);

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                StopMouseHook();

                _trayIcon?.Dispose();
                _trayIcon = null;

                _trayIconWindow?.Close();
                _trayIconWindow = null;

                _disposed = true;

                Logger.Info("TrayIconService", "系统托盘图标已释放");
            }
            catch (Exception ex)
            {
                Logger.Error("TrayIconService", "释放系统托盘图标时发生异常", ex);
            }
        }
    }
}