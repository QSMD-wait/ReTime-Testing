using ReTime_Testing.Models;
using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.Services;

/// <summary>
/// TimeTop 桌面进度条全局服务接口
/// </summary>
public interface IGlobalTimeTopDesktopService
{
    /// <summary>
    /// 状态管理器
    /// </summary>
    IProgressStateManager StateManager { get; }

    /// <summary>
    /// 状态变更回调（用于 ViewModel 更新 UI）
    /// </summary>
    Action<ProgressStateConfig>? OnStateChanged { get; set; }

    /// <summary>
    /// 设置为加载状态
    /// </summary>
    void SetLoading();

    /// <summary>
    /// 设置为进度状态
    /// </summary>
    void SetProgress(double value);

    /// <summary>
    /// 仅更新进度值（不设置样式）
    /// </summary>
    void UpdateProgressOnly(double value);

    /// <summary>
    /// 设置为成功状态
    /// </summary>
    void SetSuccess();

    /// <summary>
    /// 设置为错误状态
    /// </summary>
    void SetError();

    /// <summary>
    /// 设置为暂停状态
    /// </summary>
    void SetPaused();

    /// <summary>
    /// 设置为隐藏状态
    /// </summary>
    void SetHidden();

    /// <summary>
    /// 设置为禁用状态
    /// </summary>
    void SetDisabled();

    /// <summary>
    /// 设置进度值
    /// </summary>
    void SetValue(double value);

    /// <summary>
    /// 设置前景色
    /// </summary>
    void SetForeground(Brush foreground);

    /// <summary>
    /// 设置背景色
    /// </summary>
    void SetBackground(Brush background);

    /// <summary>
    /// 设置透明度
    /// </summary>
    void SetOpacity(double opacity);

    /// <summary>
    /// 设置可见性
    /// </summary>
    void SetVisibility(Visibility visibility);

    /// <summary>
    /// 设置启用状态
    /// </summary>
    void SetEnabled(bool isEnabled);

    /// <summary>
    /// 设置进度范围
    /// </summary>
    void SetRange(double minimum, double maximum);

    /// <summary>
    /// 批量更新操作
    /// </summary>
    void BatchUpdate(Action<IGlobalTimeTopDesktopService> action);

    /// <summary>
    /// 重置为默认状态
    /// </summary>
    void Reset();

    /// <summary>
    /// 获取当前配置
    /// </summary>
    ProgressStateConfig GetCurrentConfig();
}