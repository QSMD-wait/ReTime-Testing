namespace ReTime_Testing.Models;

/// <summary>
/// 时间段
/// 表示状态持续的期间
/// </summary>
public class TimeSegment
{
    /// <summary>
    /// 时间段 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 时间段名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 状态类型
    /// </summary>
    public ProgressStateType State { get; set; }

    /// <summary>
    /// 是否活跃（需要进度轮询）
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// 样式覆盖（可选）
    /// </summary>
    public StyleOverrides? StyleOverrides { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public TimeSegment() { }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">时间段 ID</param>
    /// <param name="name">时间段名称</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="state">状态类型</param>
    /// <param name="isActive">是否活跃</param>
    public TimeSegment(string id, string name, DateTime startTime, DateTime endTime, ProgressStateType state, bool isActive)
    {
        Id = id;
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
        State = state;
        IsActive = isActive;
    }

    /// <summary>
    /// 构造函数（带样式覆盖）
    /// </summary>
    /// <param name="id">时间段 ID</param>
    /// <param name="name">时间段名称</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="state">状态类型</param>
    /// <param name="isActive">是否活跃</param>
    /// <param name="styleOverrides">样式覆盖</param>
    public TimeSegment(string id, string name, DateTime startTime, DateTime endTime, ProgressStateType state, bool isActive, StyleOverrides? styleOverrides)
        : this(id, name, startTime, endTime, state, isActive)
    {
        StyleOverrides = styleOverrides;
    }

    /// <summary>
    /// 获取时间段持续时间
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// 检查指定时间是否在时间段内
    /// </summary>
    /// <param name="time">要检查的时间</param>
    /// <returns>如果时间在时间段内返回 true，否则返回 false</returns>
    public bool Contains(DateTime time)
    {
        return time >= StartTime && time < EndTime;
    }

    /// <summary>
    /// 克隆时间段
    /// </summary>
    public TimeSegment Clone()
    {
        return new TimeSegment(Id, Name, StartTime, EndTime, State, IsActive, StyleOverrides);
    }

    /// <summary>
    /// 获取时间段的字符串表示
    /// </summary>
    public override string ToString()
    {
        return $"{StartTime:HH:mm:ss} - {EndTime:HH:mm:ss} - {Name} ({State}){(IsActive ? " [活跃]" : "")}";
    }
}