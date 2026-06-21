namespace ReTime_Testing.Models;

/// <summary>
/// 时间跳跃原因
/// </summary>
public enum TimeJumpReason
{
    /// <summary>
    /// 云端校准导致的跳跃
    /// </summary>
    CloudCalibration,

    /// <summary>
    /// 系统休眠唤醒后的时间跳跃
    /// </summary>
    SystemResume,

    /// <summary>
    /// 手动触发校准
    /// </summary>
    ManualCalibration,

    /// <summary>
    /// 系统时间校准导致的跳跃
    /// </summary>
    SystemCalibration
}