using ReTime_Testing.Models;
using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.Services;

/// <summary>
/// 执行计划生成器
/// 从时间计划生成执行计划
/// </summary>
/// <remarks>
/// 设计原则（v3.0）：
/// - 时间段：固定为 Progress 状态，负责进度显示
/// - 时间点：非 Progress 状态，负责状态切换
/// - 时间点不能在时间段内部
/// - 时间点 = 时间段开始时间 → 忽略
/// - 时间点 = 时间段结束时间 → 立即执行
/// - 每个时间段进度独立计算 0% → 100%
/// </remarks>
public class ExecutionPlanGenerator
{
    private readonly TimeScheduleValidator _validator = new();

    /// <summary>
    /// 生成执行计划
    /// </summary>
    /// <param name="schedule">时间计划</param>
    /// <param name="date">计划日期</param>
    /// <param name="currentTime">当前时间</param>
    /// <returns>执行计划</returns>
    /// <exception cref="InvalidOperationException">配置验证失败时抛出</exception>
    public ExecutionPlan Generate(TimeSchedule schedule, DateTime date, DateTime currentTime)
    {
        // 1. 验证配置
        var validationResult = _validator.Validate(schedule);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join("\n", validationResult.Errors);
            throw new InvalidOperationException($"配置验证失败:\n{errorMessage}");
        }

        // 输出警告
        foreach (var warning in validationResult.Warnings)
        {
            Logger.Warn("ExecutionPlanGenerator", warning);
        }

        var plan = new ExecutionPlan
        {
            ScheduleId = schedule.Id,
            Date = date
        };

        // 2. 生成时间段列表（固定为 Progress）
        plan.TimeSegments = GenerateTimeSegments(schedule, date);

        // 3. 生成时间点列表（过滤无效时间点）
        plan.TimePoints = GenerateTimePoints(schedule, date, plan.TimeSegments);

        // 4. 计算当前状态
        plan.UpdateCurrentState(currentTime);

        return plan;
    }

    /// <summary>
    /// 生成时间段列表
    /// 时间段固定为 Progress 状态
    /// </summary>
    /// <param name="schedule">时间计划</param>
    /// <param name="date">计划日期</param>
    /// <returns>时间段列表</returns>
    private List<TimeSegment> GenerateTimeSegments(TimeSchedule schedule, DateTime date)
    {
        var segments = new List<TimeSegment>();
        var dayStart = date.Date;
        var dayEnd = date.Date.AddDays(1).AddTicks(-1);

        // 如果没有时间段，返回一个全天的空闲时间段
        if (schedule.Schedules.Count == 0)
        {
            segments.Add(new TimeSegment
            {
                Id = "segment_full_day",
                Name = "空闲",
                StartTime = dayStart,
                EndTime = dayEnd,
                State = ProgressStateType.Loading,
                IsActive = false
            });
            return segments;
        }

        // 解析并排序时间段
        var scheduleItems = schedule.Schedules
            .Select(item =>
            {
                try
                {
                    var startTime = CombineDateAndTime(date, item.StartTime);
                    var endTime = CombineDateAndTime(date, item.EndTime);
                    if (endTime < startTime)
                    {
                        endTime = endTime.AddDays(1);
                    }
                    return new { Item = item, StartTime = startTime, EndTime = endTime };
                }
                catch
                {
                    return null;
                }
            })
            .Where(x => x != null)
            .OrderBy(x => x!.StartTime)
            .ToList();

        // 生成时间段
        DateTime lastEnd = dayStart;
        int segmentIndex = 0;

        foreach (var current in scheduleItems)
        {
            // 如果当前时间段开始时间大于上一个结束时间，添加间隙段（Loading）
            if (current!.StartTime > lastEnd)
            {
                segments.Add(new TimeSegment
                {
                    Id = $"segment_gap_{segmentIndex++}",
                    Name = "空闲",
                    StartTime = lastEnd,
                    EndTime = current.StartTime,
                    State = ProgressStateType.Loading,
                    IsActive = false
                });
            }

            // 添加时间段（固定为 Progress）
            segments.Add(new TimeSegment
            {
                Id = $"segment_{current.Item.Id}",
                Name = current.Item.Name,
                StartTime = current.StartTime,
                EndTime = current.EndTime,
                State = ProgressStateType.Progress,  // 固定为 Progress
                IsActive = true,
                StyleOverrides = ParseStyle(current.Item.Styles)
            });

            lastEnd = current.EndTime;
        }

        // 添加最后的间隙段（Loading）
        if (lastEnd < dayEnd)
        {
            segments.Add(new TimeSegment
            {
                Id = "segment_end",
                Name = "空闲",
                StartTime = lastEnd,
                EndTime = dayEnd,
                State = ProgressStateType.Loading,
                IsActive = false
            });
        }

        return segments;
    }

    /// <summary>
    /// 生成时间点列表
    /// 过滤掉与时间段开始时间相同的时间点
    /// </summary>
    /// <param name="schedule">时间计划</param>
    /// <param name="date">计划日期</param>
    /// <param name="segments">时间段列表</param>
    /// <returns>时间点列表</returns>
    private List<TimePoint> GenerateTimePoints(TimeSchedule schedule, DateTime date, List<TimeSegment> segments)
    {
        var timePoints = new List<TimePoint>();

        // 获取所有时间段的开始时间（用于过滤）
        var segmentStartTimes = segments
            .Where(s => s.State == ProgressStateType.Progress)
            .Select(s => s.StartTime)
            .ToHashSet();

        foreach (var custom in schedule.TimePoints)
        {
            try
            {
                var customTime = CombineDateAndTime(date, custom.Time);

                // 如果时间点与时间段开始时间相同，忽略（时间段优先）
                if (segmentStartTimes.Contains(customTime))
                {
                    Logger.Info("ExecutionPlanGenerator", $"时间点 {custom.Id} ({custom.Time}) 与时间段开始时间相同，已忽略");
                    continue;
                }

                timePoints.Add(new TimePoint
                {
                    Id = custom.Id,
                    Name = string.IsNullOrEmpty(custom.Name) ? custom.Time : custom.Name,
                    Time = customTime,
                    ToState = custom.ToState,
                    StyleOverrides = ParseStyle(custom.Style)
                });
            }
            catch (Exception ex)
            {
                Logger.Warn("ExecutionPlanGenerator", $"生成时间点失败: {custom.Id}, 错误: {ex.Message}");
            }
        }

        // 按时间排序
        timePoints = timePoints.OrderBy(tp => tp.Time).ToList();

        // 自动计算 fromState
        for (int i = 0; i < timePoints.Count; i++)
        {
            if (i == 0)
            {
                timePoints[i].FromState = ProgressStateType.Loading;
            }
            else
            {
                timePoints[i].FromState = timePoints[i - 1].ToState;
            }
        }

        return timePoints;
    }

    /// <summary>
    /// 合并日期和时间字符串
    /// </summary>
    /// <param name="date">日期</param>
    /// <param name="timeString">时间字符串（HH:mm:ss 格式）</param>
    /// <returns>DateTime</returns>
    private DateTime CombineDateAndTime(DateTime date, string timeString)
    {
        var time = TimeSpan.Parse(timeString);
        return date.Date.Add(time);
    }

    /// <summary>
    /// 解析样式覆盖
    /// </summary>
    /// <param name="styleData">样式数据</param>
    /// <returns>样式覆盖对象</returns>
    private StyleOverrides? ParseStyle(StyleOverridesData? styleData)
    {
        if (styleData == null) return null;

        return new StyleOverrides
        {
            ForegroundColor = ParseColor(styleData.ForegroundColor),
            BackgroundColor = ParseColor(styleData.BackgroundColor),
            Opacity = styleData.Opacity,
            Visibility = ParseVisibility(styleData.Visibility),
            IsEnabled = styleData.IsEnabled,
            IsIndeterminate = styleData.IsIndeterminate
        };
    }

    /// <summary>
    /// 解析颜色字符串
    /// </summary>
    /// <param name="colorString">颜色字符串（如 #007ACC）</param>
    /// <returns>Brush</returns>
    private Brush? ParseColor(string? colorString)
    {
        if (string.IsNullOrEmpty(colorString)) return null;

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
    private Visibility ParseVisibility(string? visibilityString)
    {
        if (string.IsNullOrEmpty(visibilityString)) return Visibility.Visible;

        return visibilityString.ToLower() switch
        {
            "visible" => Visibility.Visible,
            "hidden" => Visibility.Hidden,
            "collapsed" => Visibility.Collapsed,
            _ => Visibility.Visible
        };
    }
}
