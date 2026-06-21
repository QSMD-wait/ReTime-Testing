namespace ReTime_Testing.Models;

/// <summary>
/// 时间跳跃事件参数
/// </summary>
public class TimeJumpedEventArgs : EventArgs
{
    /// <summary>
    /// 跳跃前的旧时间
    /// </summary>
    public DateTime OldTime { get; }

    /// <summary>
    /// 跳跃后的新时间
    /// </summary>
    public DateTime NewTime { get; }

    /// <summary>
    /// 时间偏移量
    /// 正数表示向前跳跃，负数表示向后跳跃
    /// </summary>
    public TimeSpan Offset => NewTime - OldTime;

    /// <summary>
    /// 跳跃原因
    /// </summary>
    public TimeJumpReason Reason { get; }

    /// <summary>
    /// 跳跃严重程度
    /// </summary>
    public TimeJumpSeverity Severity { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="oldTime">旧时间</param>
    /// <param name="newTime">新时间</param>
    /// <param name="reason">跳跃原因</param>
    /// <param name="severity">跳跃严重程度</param>
    public TimeJumpedEventArgs(DateTime oldTime, DateTime newTime,
        TimeJumpReason reason = TimeJumpReason.CloudCalibration,
        TimeJumpSeverity severity = TimeJumpSeverity.Major)
    {
        OldTime = oldTime;
        NewTime = newTime;
        Reason = reason;
        Severity = severity;
    }
}