using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using ReTime_Testing.Helpers;
using ReTime_Testing.Services;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 系统托盘图标服务
    /// 管理应用程序的系统托盘图标和右键菜单
    /// </summary>
    public class TrayIconService : IDisposable
    {
        private static readonly Lazy<TrayIconService> _instance =
            new Lazy<TrayIconService>(() => new TrayIconService());

        public static TrayIconService Instance => _instance.Value;

        private Window? _trayIconWindow;
        private TaskbarIcon? _trayIcon;
        private bool _disposed = false;

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
            public string? IconPath { get; set; }
        }

        /// <summary>
        /// 托盘图标配置
        /// </summary>
        private TrayIconConfig _config = new()
        {
            Title = "ReTime-Testing"
        };

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

            // 使用默认配置或传入的配置
            _config = config ?? new TrayIconConfig();

            InitializeIcon();
        }

        /// <summary>
        /// 初始化托盘图标
        /// </summary>
        private void InitializeIcon()
        {
            try
            {
                // 尝试无窗口模式创建 TaskbarIcon
                _trayIcon = new TaskbarIcon
                {
                    Icon = LoadIcon(),
                    ToolTipText = _config.Title,
                    Visibility = Visibility.Visible
                };

                // 设置右键菜单
                SetupContextMenu();

                // 绑定双击事件
                _trayIcon.TrayMouseDoubleClick += OnTrayMouseDoubleClick;

                // 左键单击显示菜单
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
                // 创建隐藏窗口承载 TaskbarIcon
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

                // 应用 ToolWindow 样式
                WindowHelper.SetToolWindowStyle(_trayIconWindow);

                // 创建 TaskbarIcon
                _trayIcon = new TaskbarIcon
                {
                    Icon = LoadIcon(),
                    ToolTipText = _config.Title,
                    Visibility = Visibility.Visible
                };

                // 设置右键菜单
                SetupContextMenu();

                // 绑定双击事件
                _trayIcon.TrayMouseDoubleClick += OnTrayMouseDoubleClick;

                // 左键单击显示菜单
                _trayIcon.TrayLeftMouseDown += OnTrayLeftMouseDown;

                // 将 TaskbarIcon 添加到窗口
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
            // 优先使用自定义图标
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

            // 使用系统应用图标作为默认图标
            return SystemIcons.Application;
        }

        /// <summary>
        /// 设置右键菜单
        /// </summary>
        private void SetupContextMenu()
        {
            var contextMenu = new ContextMenu
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1)
            };

            // 1. 应用名称（顶部）
            var aboutItem = CreateMenuItem("ReTime - Testing", "\uE946", () => AboutRequested?.Invoke());
            contextMenu.Items.Add(aboutItem);

            // 2. 分隔符
            contextMenu.Items.Add(new Separator());

            // 3. 编辑时间计划
            var openEditorItem = CreateMenuItem("编辑时间计划", "\uE787", () => OpenTimeScheduleEditorRequested?.Invoke());
            contextMenu.Items.Add(openEditorItem);

            // 4. 设置
            var openSettingItem = CreateMenuItem("设置", "\uE713", () => OpenSettingRequested?.Invoke());
            contextMenu.Items.Add(openSettingItem);

            // 5. 调试
            var openDebugItem = CreateMenuItem("调试", "\uE90F", () => OpenDebugRequested?.Invoke());
            contextMenu.Items.Add(openDebugItem);

            // 6. 分隔符
            contextMenu.Items.Add(new Separator());

            // 7. 重启
            var restartItem = CreateMenuItem("重启", "\uE72C", () => RestartRequested?.Invoke());
            contextMenu.Items.Add(restartItem);

            // 8. 退出
            var exitItem = CreateMenuItem("退出", "\uE711", () => ExitRequested?.Invoke());
            contextMenu.Items.Add(exitItem);

            // 添加外部点击关闭监听器
            AddMenuExternalClickListener(contextMenu);

            _trayIcon!.ContextMenu = contextMenu;
        }

        /// <summary>
        /// 创建菜单项
        /// </summary>
        private MenuItem CreateMenuItem(string header, string iconGlyph, Action click)
        {
            var menuItem = new MenuItem
            {
                Padding = new Thickness(8, 6, 8, 6)
            };

            var iconTextBlock = new TextBlock
            {
                Text = iconGlyph,
                FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
                FontSize = 16,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            stackPanel.Children.Add(iconTextBlock);
            stackPanel.Children.Add(new TextBlock
            {
                Text = header,
                VerticalAlignment = VerticalAlignment.Center
            });

            menuItem.Header = stackPanel;
            menuItem.Click += (s, e) => click.Invoke();
            menuItem.CommandTarget = menuItem;

            return menuItem;
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
                _trayIcon.ContextMenu.IsOpen = true;
            }
        }

        /// <summary>
        /// 添加菜单外部点击监听器，处理菜单外部点击时自动关闭
        /// </summary>
        private void AddMenuExternalClickListener(ContextMenu menu)
        {
            // 使用PreviewMouseLeftButtonDown检测点击是否在菜单外部
            menu.PreviewMouseLeftButtonDown += (s, e) =>
            {
                // 检查点击位置是否在菜单内部
                var hitResult = VisualTreeHelper.HitTest(menu, e.GetPosition(menu));
                if (hitResult?.VisualHit != null)
                {
                    // 尝试获取FrameworkElement（如果HitTest返回的不是FE，向上查找）
                    DependencyObject? current = hitResult.VisualHit as FrameworkElement;
                    if (current == null && hitResult.VisualHit is DependencyObject dobj)
                    {
                        current = dobj;
                    }

                    // 向上查找，看是否在MenuItem内或在菜单内部
                    while (current != null)
                    {
                        if (current is MenuItem)
                            return; // 在MenuItem内，不处理
                        if (current is Separator)
                        {
                            menu.IsOpen = false;
                            return;
                        }
                        if (current == menu)
                            return; // 在菜单内部，不处理
                        current = VisualTreeHelper.GetParent(current);
                    }
                }

                // 点击在菜单外部，关闭菜单
                menu.IsOpen = false;
            };
        }

        /// <summary>
        /// 显示气泡提示
        /// </summary>
        public void ShowBalloon(string title, string message, BalloonIcon icon = BalloonIcon.Info)
        {
            _trayIcon?.ShowBalloonTip(title, message, icon);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
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