using System;
using System.Windows;
using ReTime_Testing.Helpers;
using ReTime_Testing.Models;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 进度条位置枚举
    /// </summary>
    public enum ProgressBarPosition
    {
        /// <summary>
        /// 顶部
        /// </summary>
        Top,

        /// <summary>
        /// 底部
        /// </summary>
        Bottom,

        /// <summary>
        /// 左侧
        /// </summary>
        Left,

        /// <summary>
        /// 右侧
        /// </summary>
        Right
    }
}

namespace ReTime_Testing.Helpers
{
    /// <summary>
    /// 进度条窗口辅助类
    /// 提供标准接口和初始化流程
    /// </summary>
    public static class DesktopWindowHelper
    {
        /// <summary>
        /// 进度条高度/宽度
        /// </summary>
        public const int ProgressBarSize = 5;

        /// <summary>
        /// 窗口扩展高度（用于信息显示）
        /// </summary>
        public const int WindowExtension = 400;

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        public static void SetWindowPosition(Window window, ProgressBarPosition position)
        {
            var workingArea = SystemParameters.WorkArea;

            switch (position)
            {
                case ProgressBarPosition.Top:
                    WindowHelper.SetWindowPosition(window, 0, 0, workingArea.Width, WindowExtension);
                    break;
                case ProgressBarPosition.Bottom:
                    WindowHelper.SetWindowPosition(window, 0, workingArea.Height - WindowExtension, workingArea.Width, WindowExtension);
                    break;
                case ProgressBarPosition.Left:
                    WindowHelper.SetWindowPosition(window, 0, 0, WindowExtension, workingArea.Height);
                    break;
                case ProgressBarPosition.Right:
                    WindowHelper.SetWindowPosition(window, workingArea.Width - WindowExtension, 0, WindowExtension, workingArea.Height);
                    break;
            }
        }

        /// <summary>
        /// 应用标准样式
        /// </summary>
        public static void ApplyStandardStyles(Window window)
        {
            window.WindowStyle = WindowStyle.None;
            window.AllowsTransparency = true;
            window.Background = System.Windows.Media.Brushes.Transparent;
            window.ShowInTaskbar = false;
            window.ResizeMode = ResizeMode.NoResize;
            window.Topmost = true;
        }

        /// <summary>
        /// 设置工具窗口样式
        /// </summary>
        public static void SetToolWindowStyle(Window window)
        {
            WindowHelper.SetToolWindowStyle(window);
        }
    }
}