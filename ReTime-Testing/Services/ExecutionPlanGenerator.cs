using ReTime_Testing.Models;
using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.Services;

/// <summary>
/// 执行计划生成器
/// 从时间计划生成执行计划
/// </summary>
public class ExecutionPlanGenerator
{
    /// <summary>
    /// 生成执行计划
    /// </summary>
    /// <param name="schedule">时间计划</param>
    /// <param name="date">计划日期</param>
    /// <param name="currentTime">当前时间</param>
    /// <returns>执行计划</returns>
    public ExecutionPlan Generate(TimeSchedule schedule, DateTime date, DateTime currentTime)
    {
        var plan = new ExecutionPlan
        {
            ScheduleId = schedule.Id,
            Date = date
        };

        // 1. 生成时间点
        plan.TimePoints = GenerateTimePoints(schedule, date);

        // 2. 生成时间段
        plan.TimeSegments = GenerateTimeSegments(schedule, date, plan.TimePoints);

        // 3. 计算当前状态
        plan.UpdateCurrentState(currentTime);

        return plan;
    }

    /// <summary>
    /// 生成时间点列表
    /// </summary>
    /// <param name="schedule">时间计划</param>
    /// <param name="date">计划日期</param>
    /// <returns>时间点列表（按时间排序）</returns>
    private List<TimePoint> GenerateTimePoints(TimeSchedule schedule, DateTime date)
    {
        var timePoints = new List<TimePoint>();

        foreach (var item in schedule.Schedules)
        {
            try
            {
                var startTime = date + ParseTime(item.StartTime);
                var endTime = date + ParseTime(item.EndTime);

                // 处理跨午夜场景：如果结束时间小于开始时间，说明跨午夜，结束时间应该加一天
                if (endTime < startTime)
                {
                    endTime = endTime.AddDays(1);
                }

                // 开始时间点：空闲/等待 → 工作中
                timePoints.Add(new TimePoint
                {
                    Time = startTime,
                    Name = $"{item.Name} 开始",
                    FromState = ProgressStateType.Loading,
                    ToState = ProgressStateType.Progress
                });

                // 结束时间点：工作中 → 已完成
                timePoints.Add(new TimePoint
                {
                    Time = endTime,
                    Name = $"{item.Name} 结束",
                    FromState = ProgressStateType.Progress,
                    ToState = ProgressStateType.Success
                });
            }
            catch (Exception ex)
            {
                // 跳过无法解析的时间项
                Logger.Warn("ExecutionPlanGenerator", $"生成时间点失败: {item.Name}, 错误: {ex.Message}");
            }
        }

        // 按时间排序
        return timePoints.OrderBy(tp => tp.Time).ToList();
    }

    /// <summary>
    /// 生成时间段列表
    /// </summary>
    /// <param name="schedule">时间计划</param>
    /// <param name="date">计划日期</param>
    /// <param name="timePoints">时间点列表</param>
    /// <returns>时间段列表</returns>
    private List<TimeSegment> GenerateTimeSegments(
        TimeSchedule schedule,
        DateTime date,
        List<TimePoint> timePoints)
    {
        var segments = new List<TimeSegment>();

        // 如果没有时间点，返回一个全天的空闲时间段
        if (!timePoints.Any())
        {
            segments.Add(new TimeSegment
            {
                Id = "idle_full_day",
                Name = "空闲",
                StartTime = date.Date,
                EndTime = date.Date.AddDays(1).AddTicks(-1),
                State = ProgressStateType.Loading,
                IsActive = false
            });
            return segments;
        }

        // 添加开始前的空闲时间段
        var firstPoint = timePoints.First();
        if (firstPoint.Time > date.Date)
        {
            segments.Add(new TimeSegment
            {
                Id = "idle_start",
                Name = "空闲",
                StartTime = date.Date,
                EndTime = firstPoint.Time,
                State = ProgressStateType.Loading,
                IsActive = false
            });
        }

        // 添加各个时间段（每两个时间点为一个时间段）
        for (int i = 0; i < timePoints.Count; i += 2)
        {
            if (i + 1 < timePoints.Count)
            {
                var startPoint = timePoints[i];
                var endPoint = timePoints[i + 1];

                // 查找对应的时间计划项
                var scheduleItem = schedule.Schedules.FirstOrDefault(s =>
                    s.Name.Contains(startPoint.Name.Replace(" 开始", "")) ||
                    s.Name.Contains(startPoint.Name.Replace(" 结束", "")));

                segments.Add(new TimeSegment
                {
                    Id = $"segment_{i / 2}",
                    Name = startPoint.Name,
                    StartTime = startPoint.Time,
                    EndTime = endPoint.Time,
                    State = startPoint.ToState,
                    IsActive = true,
                    StyleOverrides = scheduleItem != null ? ParseStyleOverrides(scheduleItem) : null
                });
            }
        }

        // 添加结束后的空闲时间段
        var lastPoint = timePoints.Last();
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);
        if (lastPoint.Time < endOfDay)
        {
            segments.Add(new TimeSegment
            {
                Id = "idle_end",
                Name = "空闲",
                StartTime = lastPoint.Time,
                EndTime = endOfDay,
                State = ProgressStateType.Loading,
                IsActive = false
            });
        }

        return segments;
    }

    /// <summary>
    /// 解析时间字符串
    /// </summary>
    /// <param name="timeString">时间字符串（HH:mm:ss 格式）</param>
    /// <returns>TimeSpan</returns>
    private TimeSpan ParseTime(string timeString)
    {
        return TimeSpan.Parse(timeString);
    }

    /// <summary>
    /// 解析样式覆盖
    /// </summary>
    /// <param name="scheduleItem">时间计划项</param>
    /// <returns>样式覆盖</returns>
    private StyleOverrides? ParseStyleOverrides(TimeScheduleItem scheduleItem)
    {
        if (scheduleItem.Styles == null) return null;

        var overrides = new StyleOverrides();

        // 解析前景色
        if (!string.IsNullOrEmpty(scheduleItem.Styles.ForegroundColor))
        {
            overrides.ForegroundColor = ParseColor(scheduleItem.Styles.ForegroundColor);
        }

        // 解析背景色
        if (!string.IsNullOrEmpty(scheduleItem.Styles.BackgroundColor))
        {
            overrides.BackgroundColor = ParseColor(scheduleItem.Styles.BackgroundColor);
        }

        // 解析透明度
        if (scheduleItem.Styles.Opacity.HasValue)
        {
            overrides.Opacity = scheduleItem.Styles.Opacity.Value;
        }

        // 解析可见性
        if (!string.IsNullOrEmpty(scheduleItem.Styles.Visibility))
        {
            overrides.Visibility = ParseVisibility(scheduleItem.Styles.Visibility);
        }

        // 解析启用状态
        if (scheduleItem.Styles.IsEnabled.HasValue)
        {
            overrides.IsEnabled = scheduleItem.Styles.IsEnabled.Value;
        }

        // 解析不确定动画
        if (scheduleItem.Styles.IsIndeterminate.HasValue)
        {
            overrides.IsIndeterminate = scheduleItem.Styles.IsIndeterminate.Value;
        }

        // 如果没有任何覆盖，返回null
        if (!overrides.HasAnyOverride)
        {
            return null;
        }

        return overrides;
    }

    /// <summary>
    /// 解析颜色字符串
    /// </summary>
    /// <param name="colorString">颜色字符串（如 #007ACC）</param>
    /// <returns>Brush</returns>
    private Brush? ParseColor(string colorString)
    {
        try
        {
            if (colorString.StartsWith("#"))
            {
                var color = (Color)ColorConverter.ConvertFromString(colorString);
                return new SolidColorBrush(color);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析可见性字符串
    /// </summary>
    /// <param name="visibilityString">可见性字符串</param>
    /// <returns>Visibility</returns>
    private Visibility ParseVisibility(string visibilityString)
    {
        return visibilityString.ToLower() switch
        {
            "visible" => Visibility.Visible,
            "hidden" => Visibility.Hidden,
            "collapsed" => Visibility.Collapsed,
            _ => Visibility.Visible
        };
    }
}