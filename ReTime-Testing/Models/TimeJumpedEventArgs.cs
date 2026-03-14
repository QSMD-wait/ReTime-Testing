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
    /// 构造函数
    /// </summary>
    /// <param name="oldTime">旧时间</param>
    /// <param name="newTime">新时间</param>
    public TimeJumpedEventArgs(DateTime oldTime, DateTime newTime)
    {
        OldTime = oldTime;
        NewTime = newTime;
    }
}