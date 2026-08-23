using System;
using System.Linq;
using System.Windows;
using ReTime_Testing.Views;
using ReTime_Testing.Views.Settings;
using ReTime_Testing.Views.Testing;
using ReTime_Testing.Views.TimeScheduleEditor;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 窗口管理器
    /// 管理应用程序的单例窗口实例（不包括进度条窗口）
    /// </summary>
    public static class WindowManager
    {
        private static Window? _mainWindow;
        private static Window? _timeTopSetting;
        private static Window? _debugTest;
        private static Window? _timeScheduleEditor;
        private static Window? _logViewer;

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
        /// 获取或创建设置窗口（Setting）
        /// </summary>
        public static Window GetTimeTopSetting()
        {
            if (_timeTopSetting == null || !_timeTopSetting.IsLoaded)
            {
                _timeTopSetting = new Setting();
                _timeTopSetting.Closed += (s, e) => _timeTopSetting = null;
            }
            return _timeTopSetting;
        }

        /// <summary>
        /// 获取或创建调试测试窗口（DebugTest）
        /// </summary>
        public static Window GetDebugTest()
        {
            if (_debugTest == null || !_debugTest.IsLoaded)
            {
                _debugTest = new DebugTest();
                _debugTest.Closed += (s, e) => _debugTest = null;
            }
            return _debugTest;
        }

        /// <summary>
        /// 获取或创建时间计划表编辑器窗口（TimeScheduleEditor）
        /// </summary>
        public static Window GetTimeScheduleEditor()
        {
            if (_timeScheduleEditor == null || !_timeScheduleEditor.IsLoaded)
            {
                _timeScheduleEditor = new TimeScheduleEditor();
                _timeScheduleEditor.Closed += (s, e) => _timeScheduleEditor = null;
            }
            return _timeScheduleEditor;
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
        /// 显示调试测试窗口
        /// </summary>
        public static void ShowDebugTest()
        {
            var window = GetDebugTest();
            window.Show();
            window.Activate();
        }

        /// <summary>
        /// 获取或创建日志查看器窗口（LogViewer）
        /// </summary>
        public static Window GetLogViewer()
        {
            if (_logViewer == null || !_logViewer.IsLoaded)
            {
                _logViewer = new LogViewer();
                _logViewer.Closed += (s, e) => _logViewer = null;
            }
            return _logViewer;
        }

        /// <summary>
        /// 显示日志查看器窗口
        /// </summary>
        public static void ShowLogViewer()
        {
            var window = GetLogViewer();
            window.Show();
            window.Activate();
        }

        /// <summary>
        /// 显示时间计划表编辑器窗口
        /// 窗口为常驻隐藏模式：已存在的实例从后台唤起并刷新数据，而不是重建
        /// </summary>
        public static void ShowTimeScheduleEditor()
        {
            var window = GetTimeScheduleEditor();
            if (window is Views.TimeScheduleEditor.TimeScheduleEditor editor && editor.IsLoaded)
            {
                editor.PrepareForShow();
            }
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
            _debugTest?.Close();
            _timeScheduleEditor?.Close();
            _logViewer?.Close();
        }

        /// <summary>
        /// 清理所有窗口引用
        /// </summary>
        public static void ClearAllWindows()
        {
            _mainWindow = null;
            _timeTopSetting = null;
            _debugTest = null;
            _timeScheduleEditor = null;
            _logViewer = null;
        }
    }
}