using System;
using System.Windows;
using ReTime_Testing.Helpers;
using ReTime_Testing.Models;
using ReTime_Testing.Views.TimeTopDesktop;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 桌面窗口管理器
    /// </summary>
    public class DesktopWindowManager : IDesktopWindowManager
    {
        private static readonly Lazy<DesktopWindowManager> _instance =
            new Lazy<DesktopWindowManager>(() => new DesktopWindowManager());

        public static DesktopWindowManager Instance => _instance.Value;

        private Window? _currentWindow;
        private ProgressBarPosition _currentPosition;

        private DesktopWindowManager()
        {
            _currentPosition = ProgressBarPosition.Top;
        }

        /// <summary>
        /// 设置进度条位置
        /// </summary>
        public void SetPosition(ProgressBarPosition position)
        {
            if (_currentPosition == position && _currentWindow != null && _currentWindow.IsLoaded)
            {
                return; // 位置未改变且窗口已加载，无需切换
            }

            // 关闭当前窗口
            CloseCurrentWindow();

            // 创建新窗口
            _currentWindow = CreateWindow(position);

            if (_currentWindow != null)
            {
                _currentWindow.Show();
                _currentPosition = position;

                // 根据配置应用层级维持模式
                ApplyTopmostMode();
            }
        }

        /// <summary>
        /// 根据配置应用层级维持模式
        /// </summary>
        private void ApplyTopmostMode()
        {
            if (_currentWindow == null) return;

            var config = ConfigurationManager.Instance.LoadTimeTopSetting();
            TopmostService.Instance.Apply(_currentWindow, config.Window.TopmostMode);
        }

        /// <summary>
        /// 获取当前窗口
        /// </summary>
        public Window? CurrentWindow => _currentWindow;

        /// <summary>
        /// 获取当前位置
        /// </summary>
        public ProgressBarPosition CurrentPosition => _currentPosition;

        /// <summary>
        /// 关闭当前窗口
        /// </summary>
        public void CloseCurrentWindow()
        {
            // 清理层级维持服务
            TopmostService.Instance.Cleanup();

            if (_currentWindow != null)
            {
                _currentWindow.Close();
                _currentWindow = null;
            }
        }

        /// <summary>
        /// 创建指定位置的窗口
        /// </summary>
        private Window? CreateWindow(ProgressBarPosition position)
        {
            return position switch
            {
                ProgressBarPosition.Top => new TimeTopDesktop(),
                ProgressBarPosition.Bottom => new TimeTopDesktop_Bottom(),
                ProgressBarPosition.Left => new TimeTopDesktop_Left(),
                ProgressBarPosition.Right => new TimeTopDesktop_Right(),
                _ => null
            };
        }
    }
}