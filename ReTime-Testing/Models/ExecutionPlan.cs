namespace ReTime_Testing.Models;

/// <summary>
/// 执行计划
/// 预生成的完整时间执行计划，包含所有时间点和时间段
/// </summary>
public class ExecutionPlan
{
    /// <summary>
    /// 时间计划 ID
    /// </summary>
    public string ScheduleId { get; set; } = string.Empty;

    /// <summary>
    /// 计划日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 时间点列表（按时间排序）
    /// </summary>
    public List<TimePoint> TimePoints { get; set; } = new();

    /// <summary>
    /// 时间段列表
    /// </summary>
    public List<TimeSegment> TimeSegments { get; set; } = new();

    /// <summary>
    /// 当前时间段
    /// </summary>
    public TimeSegment? CurrentSegment { get; set; }

    /// <summary>
    /// 下一个时间点
    /// </summary>
    public TimePoint? NextTimePoint { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExecutionPlan() { }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="scheduleId">时间计划 ID</param>
    /// <param name="date">计划日期</param>
    public ExecutionPlan(string scheduleId, DateTime date)
    {
        ScheduleId = scheduleId;
        Date = date;
    }

    /// <summary>
    /// 根据当前时间更新当前时间段和下一个时间点
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    public void UpdateCurrentState(DateTime currentTime)
    {
        // 更新当前时间段
        CurrentSegment = TimeSegments.FirstOrDefault(seg => seg.Contains(currentTime));

        // 更新下一个时间点：指向最后一个已执行的时间点（小于等于当前时间的最近时间点）
        NextTimePoint = TimePoints.LastOrDefault(tp => tp.Time <= currentTime);
    }

    /// <summary>
    /// 获取指定时间范围内的所有时间点
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>时间范围内的所有时间点（按时间排序）</returns>
    public List<TimePoint> GetTimePointsInRange(DateTime startTime, DateTime endTime)
    {
        return TimePoints
            .Where(tp => tp.Time > startTime && tp.Time <= endTime)
            .OrderBy(tp => tp.Time)
            .ToList();
    }

    /// <summary>
    /// 克隆执行计划
    /// </summary>
    public ExecutionPlan Clone()
    {
        var plan = new ExecutionPlan(ScheduleId, Date)
        {
            CurrentSegment = CurrentSegment?.Clone(),
            NextTimePoint = NextTimePoint?.Clone()
        };

        plan.TimePoints.AddRange(TimePoints.Select(tp => tp.Clone()));
        plan.TimeSegments.AddRange(TimeSegments.Select(seg => seg.Clone()));

        return plan;
    }

    /// <summary>
    /// 获取执行计划的字符串表示
    /// </summary>
    public override string ToString()
    {
        return $"执行计划 [{ScheduleId}] - {Date:yyyy-MM-dd} - {TimePoints.Count} 个时间点, {TimeSegments.Count} 个时间段";
    }
}