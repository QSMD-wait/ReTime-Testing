using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 进度条状态管理器接口
/// </summary>
public interface IProgressStateManager
{
    /// <summary>
    /// 状态变更事件（支持多订阅者）
    /// </summary>
    event Action<ProgressStateConfig>? OnStateChanged;

    /// <summary>
    /// 当前配置
    /// </summary>
    ProgressStateConfig CurrentConfig { get; }

    /// <summary>
    /// 设置状态（应用样式）
    /// </summary>
    /// <param name="stateType">状态类型</param>
    /// <param name="overrides">样式覆盖（可选）</param>
    void SetState(ProgressStateType stateType, StyleOverrides? overrides = null);

    /// <summary>
    /// 更新进度值
    /// </summary>
    /// <param name="value">进度值</param>
    void UpdateProgress(double value);

    /// <summary>
    /// 开始批量更新（期间不会触发回调）
    /// </summary>
    IProgressStateManager BeginBatchUpdate();

    /// <summary>
    /// 结束批量更新（触发一次回调）
    /// </summary>
    IProgressStateManager EndBatchUpdate();

    /// <summary>
    /// 批量更新操作
    /// </summary>
    void BatchUpdate(Action<IProgressStateManager> action);
}