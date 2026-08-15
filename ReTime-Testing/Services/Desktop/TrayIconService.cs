using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Helpers;
using ReTime_Testing.Models;
using ReTime_Testing.Services;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 系统托盘图标服务
    /// 管理应用程序的系统托盘图标和右键菜单
    /// 菜单展开使用库的原生 MenuActivation 机制（含空点击消失、双击判定），不依赖全局鼠标钩子
    /// </summary>
    public class TrayIconService : ITrayIconService
    {
        private Window? _trayIconWindow;
        private TaskbarIcon? _trayIcon;
        private bool _disposed = false;

        /// <summary>
        /// 托盘图标配置
        /// </summary>
        private TrayIconConfig _config = new()
        {
            Title = "ReTime-Testing"
        };

        // 主题服务引用
        private IThemeService? _themeService;

        // 设置服务引用（用于主题热响应）
        private ISettingsService? _settingsService;

        // 应用图标（同时用于托盘图标与菜单顶部图标）
        private Icon? _appIcon;
        private ImageSource? _appIconSource;

        /// <summary>
        /// 打开设置请求事件
        /// </summary>
        public event Action? OpenSettingRequested;

        /// <summary>
        /// 打开调试请求事件
        /// </summary>
        public event Action? OpenDebugRequested;

        /// <summary>
        /// 打开日志查看器请求事件
        /// </summary>
        public event Action? OpenLogViewerRequested;

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
        /// 构造函数（支持 DI 注入）
        /// </summary>
        /// <param name="themeService">主题服务</param>
        /// <param name="settingsService">设置服务（订阅主题热响应）</param>
        public TrayIconService(IThemeService? themeService = null, ISettingsService? settingsService = null)
        {
            _themeService = themeService;
            _settingsService = settingsService;
        }

        /// <summary>
        /// 初始化托盘图标服务
        /// </summary>
        public void Initialize(TrayIconConfig? config = null)
        {
            if (_trayIcon != null)
                return;

            _config = config ?? new TrayIconConfig();

            if (_themeService == null)
            {
                var app = Application.Current as App;
                _themeService = app?.ThemeService;
            }

            if (_settingsService == null)
            {
                var app = Application.Current as App;
                _settingsService = app?.Services.GetRequiredService<ISettingsService>();
            }

            // 订阅主题变更（热响应）
            if (_settingsService != null)
            {
                _settingsService.OnGlobalSettingChanged += OnGlobalSettingChanged;
            }

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
            Icon? icon = null;

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
                        icon = new Icon(stream);
                    }
                    else
                    {
                        Logger.Warn("TrayIconService", $"内嵌资源未找到: {uriStr}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("TrayIconService", $"加载内嵌图标失败: {ex.Message}");
                }
            }

            // 2. 尝试加载外部文件
            if (icon == null && !string.IsNullOrEmpty(_config.IconPath) && File.Exists(_config.IconPath))
            {
                try
                {
                    icon = new Icon(_config.IconPath);
                }
                catch (Exception ex)
                {
                    Logger.Warn("TrayIconService", $"加载自定义图标失败: {ex.Message}");
                }
            }

            // 3. 回退到系统默认图标
            icon ??= SystemIcons.Application;

            _appIcon = icon;
            return icon;
        }

        /// <summary>
        /// 设置上下文菜单（原生 MenuActivation 展开，含空点击消失与双击判定）
        /// </summary>
        private void SetupContextMenu()
        {
            var isDark = IsDarkTheme(_themeService?.CurrentTheme);

            var contextMenu = new ContextMenu
            {
                Background = GetThemeBackgroundBrush(isDark),
                BorderBrush = GetThemeBorderBrush(isDark),
                BorderThickness = new Thickness(1)
            };

            // 1. 应用名称（顶部，使用应用图标）
            EnsureAppIconSource();
            contextMenu.Items.Add(CreateAppTitleMenuItem(isDark));
            contextMenu.Items.Add(new Separator()
            {
                Background = GetThemeSeparatorBrush(isDark)
            });

            // 2. 编辑时间计划
            contextMenu.Items.Add(CreateMenuItem("编辑时间计划", "\uE787", () => OpenTimeScheduleEditorRequested?.Invoke(), isDark));

            // 3. 设置
            contextMenu.Items.Add(CreateMenuItem("设置", "\uE713", () => OpenSettingRequested?.Invoke(), isDark));

            // 4. 调试与测试（子菜单）
            contextMenu.Items.Add(CreateSubMenuItem("调试与测试", "\uE90F", isDark,
                CreateMenuItem("开发者工具", "\uE90F", () => OpenDebugRequested?.Invoke(), isDark),
                CreateMenuItem("日志查看器", "\uE8BD", () => OpenLogViewerRequested?.Invoke(), isDark)));

            contextMenu.Items.Add(new Separator()
            {
                Background = GetThemeSeparatorBrush(isDark)
            });

            // 5. 重启
            contextMenu.Items.Add(CreateMenuItem("重启", "\uE72C", () => RestartRequested?.Invoke(), isDark));

            // 6. 退出
            contextMenu.Items.Add(CreateMenuItem("退出", "\uE711", () => ExitRequested?.Invoke(), isDark));

            _trayIcon!.ContextMenu = contextMenu;

            // 原生展开：左键/右键均走库的 ShowContextMenu 路径（空点击自动消失，双击自动取消展开）
            _trayIcon.MenuActivation = PopupActivationMode.LeftOrRightClick;
            // 左键展开不等待双击判定，消除约 500ms 延迟
            _trayIcon.NoLeftClickDelay = true;
            // 每次打开前刷新主题（覆盖左键与右键两种展开方式）
            _trayIcon.PreviewTrayContextMenuOpen += OnPreviewTrayContextMenuOpen;
        }

        /// <summary>
        /// 根据当前主题获取菜单背景画刷
        /// </summary>
        private static System.Windows.Media.Brush GetThemeBackgroundBrush(bool isDark)
        {
            if (isDark)
            {
                // 使用深色背景 (#23292d)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x23, 0x29, 0x2D));
            }
            return System.Windows.Media.Brushes.White;
        }

        /// <summary>
        /// 根据当前主题获取菜单边框画刷
        /// </summary>
        private static System.Windows.Media.Brush GetThemeBorderBrush(bool isDark)
        {
            if (isDark)
            {
                // 使用 Fluent Design 风格的深色边框 (#3D3D3D)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x3D, 0x3D, 0x3D));
            }
            return System.Windows.Media.Brushes.Gray;
        }

        /// <summary>
        /// 根据当前主题获取分割线画刷
        /// </summary>
        private static System.Windows.Media.Brush GetThemeSeparatorBrush(bool isDark)
        {
            if (isDark)
            {
                // 使用 Fluent Design 风格的深色分割线 (#3D3D3D)
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x3D, 0x3D, 0x3D));
            }
            return System.Windows.Media.Brushes.LightGray;
        }

        /// <summary>
        /// 根据当前主题获取菜单项前景色（文字颜色）
        /// </summary>
        private static System.Windows.Media.Brush GetThemeForegroundBrush(bool isDark)
        {
            if (isDark)
            {
                // 使用纯白色文字
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            }
            return System.Windows.Media.Brushes.Black;
        }

        /// <summary>
        /// 判断是否为深色主题
        /// </summary>
        private static bool IsDarkTheme(string? themeName)
        {
            return string.Equals(themeName, "dark", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 创建菜单项
        /// </summary>
        private MenuItem CreateMenuItem(string header, string iconGlyph, Action click, bool isDark)
        {
            var menuItem = new MenuItem
            {
                Padding = new Thickness(8, 6, 8, 6),
                Background = GetThemeBackgroundBrush(isDark),
                Foreground = GetThemeForegroundBrush(isDark)
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
        /// 创建带子菜单的菜单项
        /// </summary>
        private MenuItem CreateSubMenuItem(string header, string iconGlyph, bool isDark, params MenuItem[] items)
        {
            var menuItem = new MenuItem
            {
                Padding = new Thickness(8, 6, 8, 6),
                Background = GetThemeBackgroundBrush(isDark),
                Foreground = GetThemeForegroundBrush(isDark)
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
            menuItem.CommandTarget = menuItem;

            foreach (var item in items)
            {
                menuItem.Items.Add(item);
            }

            return menuItem;
        }

        /// <summary>
        /// 创建顶部应用名称菜单项（图标使用应用图标）
        /// </summary>
        private MenuItem CreateAppTitleMenuItem(bool isDark)
        {
            var menuItem = new MenuItem
            {
                Padding = new Thickness(8, 6, 8, 6),
                Background = GetThemeBackgroundBrush(isDark),
                Foreground = GetThemeForegroundBrush(isDark)
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            if (_appIconSource != null)
            {
                stackPanel.Children.Add(new System.Windows.Controls.Image
                {
                    Source = _appIconSource,
                    Width = 16,
                    Height = 16,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            stackPanel.Children.Add(new TextBlock { Text = "ReTime - Testing", VerticalAlignment = VerticalAlignment.Center });

            menuItem.Header = stackPanel;
            menuItem.Click += (s, e) => AboutRequested?.Invoke();
            menuItem.CommandTarget = menuItem;

            return menuItem;
        }

        /// <summary>
        /// 将应用图标转换为菜单可用的 ImageSource（只创建一次并缓存）
        /// </summary>
        private void EnsureAppIconSource()
        {
            if (_appIconSource != null || _appIcon == null)
                return;

            try
            {
                using var smallIcon = new Icon(_appIcon, 16, 16);
                _appIconSource = Imaging.CreateBitmapSourceFromHIcon(
                    smallIcon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Exception ex)
            {
                Logger.Warn("TrayIconService", $"创建应用图标源失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 菜单即将打开（左键或右键触发）——刷新主题
        /// </summary>
        private void OnPreviewTrayContextMenuOpen(object sender, RoutedEventArgs e)
        {
            ApplyMenuTheme(null);
        }

        /// <summary>
        /// 全局设置变更（热响应）——使用最新主题刷新菜单
        /// </summary>
        private void OnGlobalSettingChanged(GlobalSetting setting)
        {
            ApplyMenuTheme(setting.Basic.Theme);
        }

        /// <summary>
        /// 应用菜单主题（递归刷新所有层级，包含二级子菜单）
        /// </summary>
        /// <param name="themeName">主题名称；为 null 时使用当前主题服务的值</param>
        private void ApplyMenuTheme(string? themeName)
        {
            if (_trayIcon?.ContextMenu is not ContextMenu menu)
                return;

            var isDark = IsDarkTheme(themeName ?? _themeService?.CurrentTheme);

            menu.Background = GetThemeBackgroundBrush(isDark);
            menu.BorderBrush = GetThemeBorderBrush(isDark);

            foreach (var item in menu.Items)
            {
                ApplyMenuItemTheme(item, isDark);
            }
        }

        /// <summary>
        /// 递归应用菜单项主题（含子菜单）
        /// </summary>
        private static void ApplyMenuItemTheme(object item, bool isDark)
        {
            if (item is MenuItem menuItem)
            {
                menuItem.Background = GetThemeBackgroundBrush(isDark);
                menuItem.Foreground = GetThemeForegroundBrush(isDark);

                foreach (var child in menuItem.Items)
                {
                    ApplyMenuItemTheme(child, isDark);
                }
            }
            else if (item is Separator separator)
            {
                separator.Background = GetThemeSeparatorBrush(isDark);
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
                if (_settingsService != null)
                {
                    _settingsService.OnGlobalSettingChanged -= OnGlobalSettingChanged;
                }

                _trayIcon?.Dispose();
                _trayIcon = null;

                _trayIconWindow?.Close();
                _trayIconWindow = null;

                _appIconSource = null;
                _appIcon?.Dispose();
                _appIcon = null;

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