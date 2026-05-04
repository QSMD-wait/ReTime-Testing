using System.Windows;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 桌面窗口管理器接口
/// </summary>
public interface IDesktopWindowManager
{
    /// <summary>
    /// 获取当前窗口
    /// </summary>
    Window? CurrentWindow { get; }

    /// <summary>
    /// 获取当前位置
    /// </summary>
    ProgressBarPosition CurrentPosition { get; }

    /// <summary>
    /// 设置进度条位置
    /// </summary>
    void SetPosition(ProgressBarPosition position);

    /// <summary>
    /// 关闭当前窗口
    /// </summary>
    void CloseCurrentWindow();
}
