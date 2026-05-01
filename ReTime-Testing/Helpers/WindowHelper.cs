using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ReTime_Testing.Helpers
{
    /// <summary>
    /// 窗口帮助类，提供 Win32 API 封装和窗口操作功能
    /// </summary>
    public static class WindowHelper
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        private const int GWL_EXSTYLE = -20;

        #endregion

        #region 窗口扩展样式常量

        /// <summary>
        /// 窗口扩展样式常量
        /// </summary>
        public static class ExtendedStyles
        {
            /// <summary>
            /// 工具窗口样式（不在任务栏和 Alt+Tab 中显示）
            /// </summary>
            public const uint WS_EX_TOOLWINDOW = 0x00000080;

            /// <summary>
            /// 置顶窗口
            /// </summary>
            public const uint WS_EX_TOPMOST = 0x00000008;

            /// <summary>
            /// 透明窗口（点击穿透）
            /// </summary>
            public const uint WS_EX_TRANSPARENT = 0x00000020;

            /// <summary>
            /// 应用程序窗口
            /// </summary>
            public const uint WS_EX_APPWINDOW = 0x00040000;
        }

        #endregion

        /// <summary>
        /// 设置窗口为工具窗口（不在任务栏和 Alt+Tab 中显示）
        /// </summary>
        /// <param name="window">目标窗口</param>
        public static void SetToolWindowStyle(Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
                throw new InvalidOperationException("窗口句柄未初始化，请在 OnSourceInitialized 后调用");

            var exStyle = (uint)GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | ExtendedStyles.WS_EX_TOOLWINDOW);
        }

/// <summary>
        /// 移除工具窗口样式
        /// </summary>
        /// <param name="window">目标窗口</param>
        public static void RemoveToolWindowStyle(Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
                throw new InvalidOperationException("窗口句柄未初始化，请在 OnSourceInitialized 后调用");

            var exStyle = (uint)GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle & ~ExtendedStyles.WS_EX_TOOLWINDOW);
        }

        /// <summary>
        /// 设置窗口点击穿透（所有点击事件直接穿透到下层窗口）
        /// </summary>
        /// <param name="window">目标窗口</param>
        public static void SetClickThrough(Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
                throw new InvalidOperationException("窗口句柄未初始化，请在 OnSourceInitialized 后调用");

            var exStyle = (uint)GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle | ExtendedStyles.WS_EX_TRANSPARENT);
        }

        /// <summary>
        /// 移除窗口点击穿透
        /// </summary>
        /// <param name="window">目标窗口</param>
        public static void RemoveClickThrough(Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
                throw new InvalidOperationException("窗口句柄未初始化，请在 OnSourceInitialized 后调用");

            var exStyle = (uint)GetWindowLong(helper.Handle, GWL_EXSTYLE);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, exStyle & ~ExtendedStyles.WS_EX_TRANSPARENT);
        }

        /// <summary>
        /// 设置窗口扩展样式
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="style">扩展样式</param>
        /// <param name="add">是否添加样式（true）或移除样式（false）</param>
        public static void SetExtendedStyle(Window window, uint style, bool add = true)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
                throw new InvalidOperationException("窗口句柄未初始化，请在 OnSourceInitialized 后调用");

            var exStyle = (uint)GetWindowLong(helper.Handle, GWL_EXSTYLE);
            var newStyle = add ? (exStyle | style) : (exStyle & ~style);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, newStyle);
        }

        /// <summary>
        /// 获取窗口扩展样式
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <returns>窗口扩展样式</returns>
        public static uint GetExtendedStyle(Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero)
                throw new InvalidOperationException("窗口句柄未初始化，请在 OnSourceInitialized 后调用");

            return (uint)GetWindowLong(helper.Handle, GWL_EXSTYLE);
        }

        /// <summary>
        /// 检查窗口是否包含指定的扩展样式
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="style">要检查的样式</param>
        /// <returns>是否包含该样式</returns>
        public static bool HasExtendedStyle(Window window, uint style)
        {
            return (GetExtendedStyle(window) & style) == style;
        }

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="x">X 坐标</param>
        /// <param name="y">Y 坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public static void SetWindowPosition(Window window, double x, double y, double width, double height)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.Left = x;
            window.Top = y;
            window.Width = width;
            window.Height = height;
        }

        /// <summary>
        /// 设置窗口置顶
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="topmost">是否置顶</param>
        public static void SetTopmost(Window window, bool topmost)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            window.Topmost = topmost;
        }
    }
}