using System.Windows;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 窗口层级维持服务接口
/// </summary>
public interface ITopmostService
{
    /// <summary>
    /// 应用层级维持模式到指定窗口
    /// </summary>
    void Apply(Window window, TopmostMode mode);

    /// <summary>
    /// 清理当前模式的资源和事件订阅
    /// </summary>
    void Cleanup();
}