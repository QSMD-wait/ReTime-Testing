using ReTime_Testing.Models;
using ReTime_Testing.Services;
using Xunit;

namespace ReTime_Testing.Tests.Services;

public class TextSlotResolverTests
{
    private readonly MockScheduleManager _scheduleManager;
    private readonly MockTimeService _timeService;
    private readonly TextSlotResolver _resolver;

    public TextSlotResolverTests()
    {
        _scheduleManager = new MockScheduleManager();
        _timeService = new MockTimeService();
        _resolver = new TextSlotResolver(_scheduleManager, _timeService);
    }

    [Fact]
    public void Resolve_None_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _resolver.Resolve(TextSourceType.None));
    }

    [Fact]
    public void Resolve_CustomText_ReturnsCustomText()
    {
        Assert.Equal("Hello", _resolver.Resolve(TextSourceType.CustomText, "Hello"));
    }

    [Fact]
    public void Resolve_CustomText_Null_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _resolver.Resolve(TextSourceType.CustomText, null));
    }

    [Fact]
    public void Resolve_SegmentName_ReturnsCurrentSegmentName()
    {
        _scheduleManager.CurrentPlan = CreatePlanWithSegment("工作时间段");
        Assert.Equal("工作时间段", _resolver.Resolve(TextSourceType.SegmentName));
    }

    [Fact]
    public void Resolve_SegmentName_NoPlan_ReturnsEmpty()
    {
        _scheduleManager.CurrentPlan = null;
        Assert.Equal(string.Empty, _resolver.Resolve(TextSourceType.SegmentName));
    }

    [Fact]
    public void Resolve_RemainingTime_ReturnsFormattedDuration()
    {
        var now = new DateTime(2026, 1, 1, 10, 0, 0);
        _timeService.CurrentTime = now;
        _scheduleManager.CurrentPlan = CreatePlanWithSegment(
            "Work", now, now.AddHours(2).AddMinutes(30).AddSeconds(45));

        var result = _resolver.Resolve(TextSourceType.RemainingTime);
        Assert.Equal("2h 30m 45s", result);
    }

    [Fact]
    public void Resolve_RemainingTime_LessThanOneMinute()
    {
        var now = new DateTime(2026, 1, 1, 10, 0, 0);
        _timeService.CurrentTime = now;
        _scheduleManager.CurrentPlan = CreatePlanWithSegment("Work", now, now.AddSeconds(45));

        var result = _resolver.Resolve(TextSourceType.RemainingTime);
        Assert.Equal("45s", result);
    }

    [Fact]
    public void Resolve_RemainingTime_MinutesAndSeconds()
    {
        var now = new DateTime(2026, 1, 1, 10, 0, 0);
        _timeService.CurrentTime = now;
        _scheduleManager.CurrentPlan = CreatePlanWithSegment(
            "Work", now, now.AddMinutes(2).AddSeconds(2));

        var result = _resolver.Resolve(TextSourceType.RemainingTime);
        Assert.Equal("2m 02s", result);
    }

    [Fact]
    public void Resolve_RemainingTime_ZeroRemaining()
    {
        var now = new DateTime(2026, 1, 1, 10, 0, 0);
        _timeService.CurrentTime = now.AddHours(3); // past end
        _scheduleManager.CurrentPlan = CreatePlanWithSegment("Work", now, now.AddHours(1));

        var result = _resolver.Resolve(TextSourceType.RemainingTime);
        Assert.Equal("0s", result);
    }

    [Fact]
    public void Resolve_ElapsedTime_ReturnsFormattedDuration()
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0);
        var end = start.AddHours(4);
        var now = start.AddHours(1).AddMinutes(30).AddSeconds(15);
        _timeService.CurrentTime = now;
        _scheduleManager.CurrentPlan = CreatePlanWithSegment("Work", start, end);

        var result = _resolver.Resolve(TextSourceType.ElapsedTime);
        Assert.Equal("1h 30m 15s", result);
    }

    [Fact]
    public void Resolve_ProgressPercent_ReturnsPercentage()
    {
        var start = new DateTime(2026, 1, 1, 8, 0, 0);
        var end = start.AddHours(4);
        var now = start.AddHours(2); // 50%
        _timeService.CurrentTime = now;
        _scheduleManager.CurrentPlan = CreatePlanWithSegment("Work", start, end);

        var result = _resolver.Resolve(TextSourceType.ProgressPercent);
        Assert.Equal("50.0%", result);
    }

    [Fact]
    public void Resolve_CurrentTime_ReturnsFormattedTime()
    {
        _timeService.CurrentTime = new DateTime(2026, 1, 1, 14, 30, 45);
        var result = _resolver.Resolve(TextSourceType.CurrentTime);
        Assert.Equal("14:30:45", result);
    }

    [Fact]
    public void Resolve_NextSegment_ReturnsNextSegmentName()
    {
        var now = new DateTime(2026, 1, 1, 10, 0, 0);
        _timeService.CurrentTime = now;
        var plan = CreatePlanWithSegment("当前段", now, now.AddHours(1));
        plan.TimeSegments.Add(new TimeSegment("next", "休息时间", now.AddHours(1), now.AddHours(2), ProgressStateType.Success, false));
        _scheduleManager.CurrentPlan = plan;

        var result = _resolver.Resolve(TextSourceType.NextSegment);
        Assert.Equal("休息时间", result);
    }

    [Fact]
    public void Resolve_NextSegment_NoNext_ReturnsEmpty()
    {
        var now = new DateTime(2026, 1, 1, 10, 0, 0);
        _timeService.CurrentTime = now;
        _scheduleManager.CurrentPlan = CreatePlanWithSegment("最后段", now, now.AddHours(1));

        var result = _resolver.Resolve(TextSourceType.NextSegment);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Resolve_CurrentDate_ReturnsFormattedDate()
    {
        _timeService.CurrentTime = new DateTime(2026, 5, 4, 14, 30, 0);
        var result = _resolver.Resolve(TextSourceType.CurrentDate);
        Assert.Equal("2026/05/04", result);
    }

    [Fact]
    public void Resolve_CurrentDayOfWeek_ReturnsDayName()
    {
        // 2026-05-04 is Monday
        _timeService.CurrentTime = new DateTime(2026, 5, 4, 14, 30, 0);
        var result = _resolver.Resolve(TextSourceType.CurrentDayOfWeek);
        Assert.Equal(DayOfWeek.Monday, _timeService.CurrentTime.DayOfWeek);
        Assert.False(string.IsNullOrEmpty(result));
    }

    private static ExecutionPlan CreatePlanWithSegment(string name, DateTime? start = null, DateTime? end = null)
    {
        var s = start ?? new DateTime(2026, 1, 1, 9, 0, 0);
        var e = end ?? s.AddHours(1);
        var plan = new ExecutionPlan("test", s.Date);
        plan.TimeSegments.Add(new TimeSegment("current", name, s, e, ProgressStateType.Progress, true));
        plan.CurrentSegment = plan.TimeSegments[0];
        return plan;
    }

    private class MockScheduleManager : IScheduleManager
    {
        public ExecutionPlan? CurrentPlan { get; set; }
        public bool IsRunning => true;
        public void Initialize(ExecutionPlan plan) { }
        public void RegenerateExecutionPlan(ExecutionPlan newPlan) { }
        public void Stop() { }
        public void ApplyCurrentState() { }
    }

    private class MockTimeService : ITimeService
    {
        public DateTime CurrentTime { get; set; } = DateTime.Now;
        public DateTime GetCurrentTime() => CurrentTime;
        public void Calibrate(DateTime cloudTime) { }
        public event EventHandler<TimeJumpedEventArgs>? TimeJumped;
        public bool IsCloudSynchronized => false;
    }
}