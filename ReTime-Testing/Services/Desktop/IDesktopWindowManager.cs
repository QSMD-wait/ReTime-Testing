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

    /// <summary>
    /// 从配置重新应用层级维持模式
    /// </summary>
    void ApplyTopmostModeFromConfig();

    /// <summary>
    /// 刷新当前位置
    /// </summary>
    void RefreshPosition();

    /// <summary>
    /// 刷新文字覆盖配置
    /// </summary>
    void RefreshTextOverlay();

    /// <summary>
    /// 刷新进度条缩放比例
    /// </summary>
    void RefreshProgressBarScale();

    /// <summary>
    /// 刷新阴影配置（进度条阴影开关热重载）
    /// </summary>
    void RefreshShadow();
}