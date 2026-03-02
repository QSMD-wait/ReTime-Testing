using System;
using System.Linq;
using System.Windows;
using ReTime_Testing.Views;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 窗口管理器
    /// 管理应用程序的单例窗口实例
    /// </summary>
    public static class WindowManager
    {
        private static Window? _mainWindow;
        private static Window? _timeTopSetting;
        private static Window? _timeTopDesktop;

        /// <summary>
        /// 获取或创建主窗口（MainWindow）
        /// </summary>
        public static Window GetMainWindow()
        {
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow();
                _mainWindow.Closed += (s, e) => _mainWindow = null;
            }
            return _mainWindow;
        }

        /// <summary>
        /// 获取或创建调试窗口（TimeTopSetting）
        /// </summary>
        public static Window GetTimeTopSetting()
        {
            if (_timeTopSetting == null || !_timeTopSetting.IsLoaded)
            {
                _timeTopSetting = new TimeTopSetting();
                _timeTopSetting.Closed += (s, e) => _timeTopSetting = null;
            }
            return _timeTopSetting;
        }

        /// <summary>
        /// 获取或创建进度条窗口（TimeTopDesktop）
        /// </summary>
        public static Window GetTimeTopDesktop()
        {
            if (_timeTopDesktop == null || !_timeTopDesktop.IsLoaded)
            {
                _timeTopDesktop = new TimeTopDesktop();
                _timeTopDesktop.Closed += (s, e) => _timeTopDesktop = null;
            }
            return _timeTopDesktop;
        }

        /// <summary>
        /// 显示主窗口
        /// </summary>
        public static void ShowMainWindow()
        {
            var window = GetMainWindow();
            window.Show();
            window.Activate();
        }

        /// <summary>
        /// 显示调试窗口
        /// </summary>
        public static void ShowTimeTopSetting()
        {
            var window = GetTimeTopSetting();
            window.Show();
            window.Activate();
        }

        /// <summary>
        /// 显示进度条窗口
        /// </summary>
        public static void ShowTimeTopDesktop()
        {
            var window = GetTimeTopDesktop();
            window.Show();
            window.Activate();
        }

        /// <summary>
        /// 关闭所有窗口
        /// </summary>
        public static void CloseAllWindows()
        {
            _mainWindow?.Close();
            _timeTopSetting?.Close();
            _timeTopDesktop?.Close();
        }

        /// <summary>
        /// 清理所有窗口引用
        /// </summary>
        public static void ClearAllWindows()
        {
            _mainWindow = null;
            _timeTopSetting = null;
            _timeTopDesktop = null;
        }
    }
}