namespace ReTime_Testing.Models;

/// <summary>
/// 时间点
/// 表示状态切换的时刻
/// </summary>
public class TimePoint
{
    /// <summary>
    /// 绝对时间
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// 时间点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 源状态（切换前的状态）
    /// </summary>
    public ProgressStateType FromState { get; set; }

    /// <summary>
    /// 目标状态（切换后的状态）
    /// </summary>
    public ProgressStateType ToState { get; set; }

    /// <summary>
    /// 样式覆盖（可选）
    /// </summary>
    public StyleOverrides? StyleOverrides { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public TimePoint() { }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="time">绝对时间</param>
    /// <param name="name">时间点名称</param>
    /// <param name="fromState">源状态</param>
    /// <param name="toState">目标状态</param>
    public TimePoint(DateTime time, string name, ProgressStateType fromState, ProgressStateType toState)
    {
        Time = time;
        Name = name;
        FromState = fromState;
        ToState = toState;
    }

    /// <summary>
    /// 构造函数（带样式覆盖）
    /// </summary>
    /// <param name="time">绝对时间</param>
    /// <param name="name">时间点名称</param>
    /// <param name="fromState">源状态</param>
    /// <param name="toState">目标状态</param>
    /// <param name="styleOverrides">样式覆盖</param>
    public TimePoint(DateTime time, string name, ProgressStateType fromState, ProgressStateType toState, StyleOverrides? styleOverrides)
        : this(time, name, fromState, toState)
    {
        StyleOverrides = styleOverrides;
    }

    /// <summary>
    /// 克隆时间点
    /// </summary>
    public TimePoint Clone()
    {
        return new TimePoint(Time, Name, FromState, ToState, StyleOverrides);
    }

    /// <summary>
    /// 获取时间点的字符串表示
    /// </summary>
    public override string ToString()
    {
        return $"{Time:HH:mm:ss} - {Name} ({FromState} → {ToState})";
    }
}