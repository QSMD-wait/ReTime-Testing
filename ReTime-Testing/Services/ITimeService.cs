using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 时间服务接口
/// 提供绝对时间管理，支持云端校准
/// </summary>
public interface ITimeService
{
    /// <summary>
    /// 获取当前绝对时间
    /// </summary>
    /// <returns>当前绝对时间</returns>
    DateTime GetCurrentTime();

    /// <summary>
    /// 校准时间（硬跳，重置基准并触发 TimeJumped 事件）
    /// 适用于偏差较大的校准，需要重新计算调度状态
    /// </summary>
    /// <param name="calibratedTime">校准后的时间</param>
    /// <param name="reason">跳跃原因</param>
    /// <param name="severity">跳跃严重程度</param>
    void Calibrate(DateTime calibratedTime, TimeJumpReason reason = TimeJumpReason.CloudCalibration, TimeJumpSeverity severity = TimeJumpSeverity.Major);

    /// <summary>
    /// 应用偏移量（微调，不重置 Stopwatch 起点以保持单调性，不触发 TimeJumped 事件）
    /// 适用于偏差较小的校准，仅调整基准时间偏移
    /// </summary>
    /// <param name="offset">时间偏移量</param>
    void ApplyOffset(TimeSpan offset);

    /// <summary>
    /// 应用用户时间偏移量（持久化偏移，与校准偏移独立）
    /// </summary>
    /// <param name="offset">用户偏移量</param>
    void ApplyUserOffset(TimeSpan offset);

    /// <summary>
    /// 当前用户时间偏移量
    /// </summary>
    TimeSpan CurrentUserOffset { get; }

    /// <summary>
    /// 时间跳跃事件
    /// 当时间发生校准或跳跃时触发
    /// </summary>
    event EventHandler<TimeJumpedEventArgs>? TimeJumped;

    /// <summary>
    /// 是否云端同步
    /// </summary>
    bool IsCloudSynchronized { get; }
}