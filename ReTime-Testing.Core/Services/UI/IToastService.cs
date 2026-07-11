namespace ReTime_Testing.Services;

/// <summary>
/// Toast 通知服务接口
/// 当前为临时实现，后续将替换为全局 Toast 栈组件
/// </summary>
public interface IToastService
{
    /// <summary>
    /// 显示 Toast 通知
    /// </summary>
    /// <param name="message">通知内容</param>
    /// <param name="type">通知类型</param>
    /// <param name="durationMs">显示时长（毫秒），默认 3000</param>
    void Show(string message, ToastType type = ToastType.Info, int durationMs = 3000);

    /// <summary>
    /// Toast 显示事件（供 View 层订阅以显示 UI）
    /// </summary>
    event Action<string, ToastType, int>? ToastRequested;
}

/// <summary>
/// Toast 通知类型
/// </summary>
public enum ToastType
{
    Info,
    Success,
    Warning,
    Error
}