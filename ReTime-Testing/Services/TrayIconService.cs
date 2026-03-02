using System;
using System.Drawing;
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
        /// 打开调试请求事件
        /// </summary>
        public event Action? OpenDebugRequested;

        /// <summary>
        /// 关于请求事件
        /// </summary>
        public event Action? AboutRequested;

        /// <summary>
        /// 退出请求事件
        /// </summary>
        public event Action? ExitRequested;

        private TrayIconService()
        {
        }

        /// <summary>
        /// 初始化托盘图标服务
        /// </summary>
        public void Initialize()
        {
            if (_trayIcon != null)
                return;

            try
            {
                // 尝试无窗口模式创建 TaskbarIcon
                _trayIcon = new TaskbarIcon
                {
                    Icon = GetDefaultIcon(),
                    ToolTipText = "ReTime-Testing",
                    Visibility = Visibility.Visible
                };

                // 设置右键菜单
                SetupContextMenu();

                // 绑定事件（只需要双击事件）
                _trayIcon.TrayMouseDoubleClick += OnTrayMouseDoubleClick;

                Logger.Info("TrayIconService", "系统托盘图标初始化成功（无窗口模式）");
            }
            catch (Exception ex)
            {
                Logger.Error("TrayIconService", "初始化系统托盘图标时发生异常", ex);

                // 如果无窗口模式失败，回退到有窗口模式
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
                    Icon = GetDefaultIcon(),
                    ToolTipText = "ReTime-Testing",
                    Visibility = Visibility.Visible
                };

                // 设置右键菜单
                SetupContextMenu();

                // 绑定事件（只需要双击事件）
                _trayIcon.TrayMouseDoubleClick += OnTrayMouseDoubleClick;

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
        /// 设置右键菜单（默认样式，背景不透明）
        /// </summary>
        private void SetupContextMenu()
        {
            var contextMenu = new ContextMenu
            {
                Background = System.Windows.Media.Brushes.White,  // 白色背景，不透明
                BorderBrush = System.Windows.Media.Brushes.Gray,  // 灰色边框
                BorderThickness = new Thickness(1)
            };

            // 打开调试菜单项
            var openDebugItem = CreateMenuItem("打开调试", "\uE713");
            openDebugItem.Click += (s, e) => OpenDebugRequested?.Invoke();
            contextMenu.Items.Add(openDebugItem);

            // 关于菜单项
            var aboutItem = CreateMenuItem("关于", "\uE946");
            aboutItem.Click += (s, e) => AboutRequested?.Invoke();
            contextMenu.Items.Add(aboutItem);

            // 分隔符
            var separator = new Separator();
            contextMenu.Items.Add(separator);

            // 退出菜单项
            var exitItem = CreateMenuItem("退出", "\uE711");
            exitItem.Click += (s, e) => ExitRequested?.Invoke();
            contextMenu.Items.Add(exitItem);

            _trayIcon!.ContextMenu = contextMenu;
        }

        /// <summary>
        /// 创建菜单项（默认样式）
        /// </summary>
        private MenuItem CreateMenuItem(string header, string iconGlyph)
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
            stackPanel.Children.Add(new TextBlock { Text = header, VerticalAlignment = VerticalAlignment.Center });

            menuItem.Header = stackPanel;

            return menuItem;
        }

        /// <summary>
        /// 左键双击事件处理 - 打开调试窗口
        /// </summary>
        private void OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            OpenDebugRequested?.Invoke();
        }

        /// <summary>
        /// 获取默认图标
        /// </summary>
        private Icon GetDefaultIcon()
        {
            // 使用系统应用图标作为默认图标
            return SystemIcons.Application;
        }

        /// <summary>
        /// 显示气球提示
        /// </summary>
        public void ShowBalloon(string title, string message, BalloonIcon icon = BalloonIcon.Info)
        {
            if (_trayIcon != null)
            {
                _trayIcon.ShowBalloonTip(title, message, icon);
            }
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