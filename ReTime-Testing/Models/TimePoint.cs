using System.Windows.Media;

namespace ReTime_Testing.Models;

/// <summary>
/// 时间点
/// 表示状态切换的时刻
/// </summary>
public class TimePoint
{
    /// <summary>
    /// 时间点唯一标识
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 绝对时间
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// 时间点名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 时间点类型列表（数组形式，支持组合）
    /// </summary>
    public List<TimePointType> Types { get; set; } = new() { TimePointType.StateChange };

    /// <summary>
    /// 状态变更数据（当 Type = StateChange 时生效）
    /// </summary>
    public StateChangeData? StateChange { get; set; }

    /// <summary>
    /// 样式变更数据（当 Type = StyleChange 时生效）
    /// </summary>
    public StyleChangeData? StyleChange { get; set; }

    /// <summary>
    /// 尝试获取源状态（由 StateChange.FromState 提供）
    /// </summary>
    public bool TryGetFromState(out ProgressStateType fromState)
    {
        fromState = default;
        if (StateChange != null && StateChange.FromState.HasValue)
        {
            fromState = StateChange.FromState.Value;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 尝试获取目标状态（由 StateChange.ToState 提供）
    /// </summary>
    public bool TryGetToState(out ProgressStateType toState)
    {
        toState = default;
        if (StateChange != null)
        {
            toState = StateChange.ToState;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 获取样式覆盖（从 StyleChange 中读取样式属性并转换为 Brush）
    /// </summary>
    public StyleOverrides? GetStyleOverrides()
    {
        string? fg = null;
        string? bg = null;
        double? opacity = null;

        if (Types.Contains(TimePointType.StyleChange) && StyleChange != null)
        {
            fg = StyleChange.ForegroundColor;
            bg = StyleChange.BackgroundColor;
            opacity = StyleChange.Opacity;
        }

        if (fg == null && bg == null && opacity == null) return null;
        Brush? fgBrush = null;
        Brush? bgBrush = null;
        try
        {
            if (!string.IsNullOrEmpty(fg))
            {
                var brush = new System.Windows.Media.BrushConverter().ConvertFromString(fg);
                if (brush is Brush convertedBrush)
                {
                    fgBrush = convertedBrush;
                }
            }
        }
        catch { fgBrush = null; }
        try
        {
            if (!string.IsNullOrEmpty(bg))
            {
                var brush = new System.Windows.Media.BrushConverter().ConvertFromString(bg);
                if (brush is Brush convertedBrush)
                {
                    bgBrush = convertedBrush;
                }
            }
        }
        catch { bgBrush = null; }
        return new StyleOverrides
        {
            ForegroundColor = fgBrush,
            BackgroundColor = bgBrush,
            Opacity = opacity
        };
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    public TimePoint() { }

    /// <summary>
    /// 构造函数（带类型与数据）
    /// </summary>
    /// <param name="time">绝对时间</param>
    /// <param name="name">时间点名称</param>
    /// <param name="types">时间点类型列表</param>
    /// <param name="stateChange">状态变更数据（Types 包含 StateChange 时使用）</param>
    /// <param name="styleChange">样式变更数据（Types 包含 StyleChange 时使用）</param>
    public TimePoint(DateTime time, string name, List<TimePointType> types, StateChangeData? stateChange = null, StyleChangeData? styleChange = null)
    {
        Time = time;
        Name = name;
        Types = types;
        StateChange = stateChange;
        StyleChange = styleChange;
    }

    /// <summary>
    /// 克隆时间点
    /// </summary>
    public TimePoint Clone()
    {
        return new TimePoint(Time, Name, new List<TimePointType>(Types), StateChange, StyleChange);
    }

    /// <summary>
    /// 获取时间点的字符串表示
    /// </summary>
    public override string ToString()
    {
        if (TryGetFromState(out var fromState) && TryGetToState(out var toState))
            return $"{Time:HH:mm:ss} - {Name} ({fromState} -> {toState})";
        if (TryGetToState(out toState))
            return $"{Time:HH:mm:ss} - {Name} ({toState})";
        return $"{Time:HH:mm:ss} - {Name} ({string.Join("+", Types)})";
    }
}