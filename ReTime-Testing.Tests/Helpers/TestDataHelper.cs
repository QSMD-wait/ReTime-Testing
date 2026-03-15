using ReTime_Testing.Models;

namespace ReTime_Testing.Tests.Helpers;

/// <summary>
/// 测试数据辅助类
/// 提供创建测试数据的静态方法
/// </summary>
public static class TestDataHelper
{
    /// <summary>
    /// 创建测试用的时间点
    /// </summary>
    public static TimePoint CreateTestTimePoint(
        int hour,
        int minute,
        string name = "测试时间点",
        ProgressStateType fromState = ProgressStateType.Loading,
        ProgressStateType toState = ProgressStateType.Progress)
    {
        return new TimePoint
        {
            Time = DateTime.Today.AddHours(hour).AddMinutes(minute),
            Name = name,
            FromState = fromState,
            ToState = toState
        };
    }

    /// <summary>
    /// 创建测试用的时间段
    /// </summary>
    public static TimeSegment CreateTestTimeSegment(
        int startHour,
        int endHour,
        string name = "测试时间段",
        ProgressStateType state = ProgressStateType.Progress,
        bool isActive = true)
    {
        return new TimeSegment
        {
            Id = "test_segment",
            Name = name,
            StartTime = DateTime.Today.AddHours(startHour),
            EndTime = DateTime.Today.AddHours(endHour),
            State = state,
            IsActive = isActive
        };
    }

    /// <summary>
    /// 创建简单的测试执行计划
    /// </summary>
    public static ExecutionPlan CreateSimpleExecutionPlan()
    {
        var plan = new ExecutionPlan
        {
            ScheduleId = "test_schedule",
            Date = DateTime.Today
        };

        // 添加时间点
        plan.TimePoints.Add(CreateTestTimePoint(9, 0, "工作开始", ProgressStateType.Loading, ProgressStateType.Progress));
        plan.TimePoints.Add(CreateTestTimePoint(18, 0, "工作结束", ProgressStateType.Progress, ProgressStateType.Success));

        // 添加时间段
        plan.TimeSegments.Add(CreateTestTimeSegment(0, 9, "空闲", ProgressStateType.Loading, false));
        plan.TimeSegments.Add(CreateTestTimeSegment(9, 18, "工作", ProgressStateType.Progress, true));
        plan.TimeSegments.Add(CreateTestTimeSegment(18, 24, "空闲", ProgressStateType.Loading, false));

        // 设置当前状态
        plan.UpdateCurrentState(DateTime.Today.AddHours(10));

        return plan;
    }

    /// <summary>
    /// 创建测试用的时间计划
    /// </summary>
    public static TimeSchedule CreateTestSchedule()
    {
        return new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "工作时间",
                    StartTime = "09:00:00",
                    EndTime = "18:00:00"
                }
            }
        };
    }
}