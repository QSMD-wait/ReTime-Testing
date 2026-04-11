using System;
using System.Drawing;
using System.IO;
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

            // 总是添加菜单项（事件检查移至点击时）
            var openSettingItem = CreateMenuItem("打开设置", "\uE713", () => OpenSettingRequested?.Invoke());
            contextMenu.Items.Add(openSettingItem);

            var openDebugItem = CreateMenuItem("打开调试", "\uE713", () => OpenDebugRequested?.Invoke());
            contextMenu.Items.Add(openDebugItem);

            var openEditorItem = CreateMenuItem("时间计划表编辑器", "\uE787", () => OpenTimeScheduleEditorRequested?.Invoke());
            contextMenu.Items.Add(openEditorItem);

            var aboutItem = CreateMenuItem("关于", "\uE946", () => AboutRequested?.Invoke());
            contextMenu.Items.Add(aboutItem);

            var separator = new Separator();
            contextMenu.Items.Add(separator);

            var restartItem = CreateMenuItem("重启", "\uE72C", () => RestartRequested?.Invoke());
            contextMenu.Items.Add(restartItem);

            var exitItem = CreateMenuItem("退出", "\uE711", () => ExitRequested?.Invoke());
            contextMenu.Items.Add(exitItem);

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