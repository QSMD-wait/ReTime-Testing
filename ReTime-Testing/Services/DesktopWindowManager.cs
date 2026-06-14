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
        private readonly ISettingsService _settingsService;

        private Window? _currentWindow;
        private ProgressBarPosition _currentPosition;

        /// <summary>
        /// 构造函数（支持 DI 注入）
        /// </summary>
        /// <param name="settingsService">设置服务</param>
        public DesktopWindowManager(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _currentPosition = ProgressBarPosition.Top;
        }

        /// <summary>
        /// 设置进度条位置
        /// </summary>
        public void SetPosition(ProgressBarPosition position)
        {
            if (_currentPosition == position && _currentWindow != null && _currentWindow.IsLoaded)
            {
                return;
            }

            CloseCurrentWindow();

            _currentWindow = CreateWindow(position);

            if (_currentWindow != null)
            {
                _currentWindow.Show();
                _currentPosition = position;

                ApplyTopmostMode();
            }
        }

        /// <summary>
        /// 根据配置应用层级维持模式
        /// </summary>
        private void ApplyTopmostMode()
        {
            if (_currentWindow == null) return;

            var config = _settingsService.GetTimeTopSetting();
            TopmostService.Instance.Apply(_currentWindow, config.Window.TopmostMode);
        }

        /// <summary>
        /// 从配置重新应用层级维持模式（供热重载调用）
        /// </summary>
        public void ApplyTopmostModeFromConfig()
        {
            if (_currentWindow == null || !_currentWindow.IsLoaded) return;

            var config = _settingsService.GetTimeTopSetting();
            TopmostService.Instance.Apply(_currentWindow, config.Window.TopmostMode);
        }

        /// <summary>
        /// 刷新当前位置
        /// </summary>
        public void RefreshPosition()
        {
            if (_currentWindow != null && _currentWindow.IsLoaded)
            {
                DesktopWindowHelper.SetWindowPosition(_currentWindow, _currentPosition);
            }
        }

        /// <summary>
        /// 刷新文字覆盖配置
        /// </summary>
        public void RefreshTextOverlay()
        {
            if (_currentWindow == null || !_currentWindow.IsLoaded) return;

            if (_currentWindow.DataContext is TimeTopDesktopViewModel vm && vm.TextOverlay != null)
            {
                vm.TextOverlay.LoadConfig();
            }
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