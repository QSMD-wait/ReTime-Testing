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
    /// 从云端校准时间
    /// </summary>
    /// <param name="cloudTime">云端时间</param>
    void Calibrate(DateTime cloudTime);

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