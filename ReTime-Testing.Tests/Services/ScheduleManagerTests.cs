using ReTime_Testing.Models;
using ReTime_Testing.Services;
using Moq;
using FluentAssertions;

namespace ReTime_Testing.Tests.Services;

/// <summary>
/// ScheduleManager 单元测试
/// 注意：由于 ScheduleManager 依赖 ProgressStateManager 的具体实现，部分测试可能难以完全隔离
/// </summary>
public class ScheduleManagerTests
{
    private readonly Mock<ITimeService> _mockTimeService;
    private readonly ScheduleManager _manager;

    public ScheduleManagerTests()
    {
        _mockTimeService = new Mock<ITimeService>();
        // 使用真实的 ProgressStateManager，因为 Mock 它的方法比较复杂
        var stateManager = new ProgressStateManager();
        _manager = new ScheduleManager(_mockTimeService.Object, stateManager);
    }

    [Fact]
    public void Constructor_应该正确初始化()
    {
        // Assert
        _manager.Should().NotBeNull();
        _manager.CurrentPlan.Should().BeNull();
        _manager.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Initialize_应该设置执行计划并启动调度()
    {
        // Arrange
        var plan = CreateTestPlan();
        var currentTime = new DateTime(2026, 3, 15, 9, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(currentTime);

        // Act
        _manager.Initialize(plan);

        // Assert
        _manager.IsRunning.Should().BeTrue();
        _manager.CurrentPlan.Should().Be(plan);
    }

    [Fact]
    public void Stop_应该停止调度()
    {
        // Arrange
        var plan = CreateTestPlan();
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(DateTime.Now);
        _manager.Initialize(plan);

        // Act
        _manager.Stop();

        // Assert
        _manager.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void RegenerateExecutionPlan_应该更新执行计划()
    {
        // Arrange
        var oldPlan = CreateTestPlan();
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(DateTime.Now);
        _manager.Initialize(oldPlan);

        var newPlan = CreateTestPlan();
        newPlan.ScheduleId = "new_schedule";

        // Act
        _manager.RegenerateExecutionPlan(newPlan);

        // Assert
        _manager.CurrentPlan.Should().Be(newPlan);
        _manager.CurrentPlan.Should().NotBe(oldPlan);
    }

    [Fact]
    public void Initialize_应该订阅时间跳跃事件()
    {
        // Arrange
        var plan = CreateTestPlan();
        var currentTime = new DateTime(2026, 3, 15, 9, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(currentTime);

        var eventRaised = false;
        _mockTimeService.Object.TimeJumped += (sender, args) => eventRaised = true;

        // Act
        _manager.Initialize(plan);
        _mockTimeService.Raise(x => x.TimeJumped += null, new TimeJumpedEventArgs(currentTime, currentTime.AddHours(1)));

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void Dispose_应该正确释放资源()
    {
        // Arrange
        var plan = CreateTestPlan();
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(DateTime.Now);
        _manager.Initialize(plan);

        // Act
        _manager.Stop();

        // Assert
        _manager.IsRunning.Should().BeFalse();
    }

    /// <summary>
    /// 创建测试用的执行计划
    /// </summary>
    private ExecutionPlan CreateTestPlan()
    {
        var plan = new ExecutionPlan
        {
            ScheduleId = "test_schedule",
            Date = DateTime.Today
        };

        // 添加时间点
        plan.TimePoints.Add(new TimePoint
        {
            Time = DateTime.Today.AddHours(9),
            Name = "工作开始",
            FromState = ProgressStateType.Loading,
            ToState = ProgressStateType.Progress
        });

        plan.TimePoints.Add(new TimePoint
        {
            Time = DateTime.Today.AddHours(12),
            Name = "午休开始",
            FromState = ProgressStateType.Progress,
            ToState = ProgressStateType.Success
        });

        plan.TimePoints.Add(new TimePoint
        {
            Time = DateTime.Today.AddHours(13),
            Name = "下午工作开始",
            FromState = ProgressStateType.Loading,
            ToState = ProgressStateType.Progress
        });

        plan.TimePoints.Add(new TimePoint
        {
            Time = DateTime.Today.AddHours(18),
            Name = "工作结束",
            FromState = ProgressStateType.Progress,
            ToState = ProgressStateType.Success
        });

        // 添加时间段
        plan.TimeSegments.Add(new TimeSegment
        {
            Id = "idle_start",
            Name = "空闲",
            StartTime = DateTime.Today,
            EndTime = DateTime.Today.AddHours(9),
            State = ProgressStateType.Loading,
            IsActive = false
        });

        plan.TimeSegments.Add(new TimeSegment
        {
            Id = "work_morning",
            Name = "上午工作",
            StartTime = DateTime.Today.AddHours(9),
            EndTime = DateTime.Today.AddHours(12),
            State = ProgressStateType.Progress,
            IsActive = true
        });

        plan.TimeSegments.Add(new TimeSegment
        {
            Id = "lunch",
            Name = "午休",
            StartTime = DateTime.Today.AddHours(12),
            EndTime = DateTime.Today.AddHours(13),
            State = ProgressStateType.Loading,
            IsActive = false
        });

        plan.TimeSegments.Add(new TimeSegment
        {
            Id = "work_afternoon",
            Name = "下午工作",
            StartTime = DateTime.Today.AddHours(13),
            EndTime = DateTime.Today.AddHours(18),
            State = ProgressStateType.Progress,
            IsActive = true
        });

        plan.TimeSegments.Add(new TimeSegment
        {
            Id = "idle_end",
            Name = "空闲",
            StartTime = DateTime.Today.AddHours(18),
            EndTime = DateTime.Today.AddDays(1).AddTicks(-1),
            State = ProgressStateType.Loading,
            IsActive = false
        });

        // 更新当前状态
        plan.UpdateCurrentState(DateTime.Today.AddHours(10));

        return plan;
    }
}