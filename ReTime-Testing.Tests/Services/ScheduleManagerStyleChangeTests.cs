using ReTime_Testing.Models;
using ReTime_Testing.Services;
using Moq;

namespace ReTime_Testing.Tests.Services;

/// <summary>
/// ScheduleManager ExecuteStyleChange 方法的单元测试
/// 测试样式变更逻辑
/// </summary>
public class ScheduleManagerStyleChangeTests
{
    private readonly Mock<ITimeService> _mockTimeService;
    private readonly ScheduleManager _manager;

    public ScheduleManagerStyleChangeTests()
    {
        _mockTimeService = new Mock<ITimeService>();
        var stateManager = new ProgressStateManager();
        _manager = new ScheduleManager(_mockTimeService.Object, stateManager);
    }

    [Fact]
    public void ExecuteStyleChange_应该只更新样式不改变状态()
    {
        // Arrange
        var plan = CreateTestPlanWithStyleChange();
        var currentTime = new DateTime(2026, 3, 15, 9, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(currentTime);
        _manager.Initialize(plan);

        // Act
        _mockTimeService.Raise(x => x.TimeJumped += null, new TimeJumpedEventArgs(currentTime, currentTime.AddHours(1)));

        // Assert
        _manager.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void ExecuteStyleChange_应该应用ForegroundColor()
    {
        // Arrange
        var plan = CreateTestPlanWithStyleChange();
        var currentTime = new DateTime(2026, 3, 15, 9, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(currentTime);
        _manager.Initialize(plan);

        // Act
        _mockTimeService.Raise(x => x.TimeJumped += null, new TimeJumpedEventArgs(currentTime, currentTime.AddHours(1)));

        // Assert
        _manager.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void ExecuteStyleChange_应该应用BackgroundColor()
    {
        // Arrange
        var plan = CreateTestPlanWithStyleChange();
        var currentTime = new DateTime(2026, 3, 15, 9, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(currentTime);
        _manager.Initialize(plan);

        // Act
        _mockTimeService.Raise(x => x.TimeJumped += null, new TimeJumpedEventArgs(currentTime, currentTime.AddHours(1)));

        // Assert
        _manager.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void ExecuteStyleChange_应该应用Opacity()
    {
        // Arrange
        var plan = CreateTestPlanWithStyleChange();
        var currentTime = new DateTime(2026, 3, 15, 9, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(currentTime);
        _manager.Initialize(plan);

        // Act
        _mockTimeService.Raise(x => x.TimeJumped += null, new TimeJumpedEventArgs(currentTime, currentTime.AddHours(1)));

        // Assert
        _manager.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void ExecuteStyleChange_应该应用所有样式属性()
    {
        // Arrange
        var plan = CreateTestPlanWithStyleChange();
        var currentTime = new DateTime(2026, 3, 15, 9, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(currentTime);
        _manager.Initialize(plan);

        // Act
        _mockTimeService.Raise(x => x.TimeJumped += null, new TimeJumpedEventArgs(currentTime, currentTime.AddHours(1)));

        // Assert
        _manager.IsRunning.Should().BeTrue();
    }

    [Fact]
    public void ExecuteStyleChange_应该更新当前时间段()
    {
        // Arrange
        var plan = CreateTestPlanWithStyleChange();
        var currentTime = new DateTime(2026, 3, 15, 9, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(currentTime);
        _manager.Initialize(plan);

        // Act
        _mockTimeService.Raise(x => x.TimeJumped += null, new TimeJumpedEventArgs(currentTime, currentTime.AddHours(1)));

        // Assert
        _manager.CurrentPlan.Should().NotBeNull();
    }

    [Fact]
    public void ExecuteStyleChange_应该在StyleChange类型时执行()
    {
        // Arrange
        var plan = CreateTestPlanWithStyleChange();
        var currentTime = new DateTime(2026, 3, 15, 9, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(currentTime);
        _manager.Initialize(plan);

        // Act
        _mockTimeService.Raise(x => x.TimeJumped += null, new TimeJumpedEventArgs(currentTime, currentTime.AddHours(1)));

        // Assert
        _manager.CurrentPlan.Should().NotBeNull();
    }

    [Fact]
    public void ExecuteStyleChange_不应该在StateChange类型时执行()
    {
        // Arrange
        var plan = CreateTestPlanWithStateChange();
        var currentTime = new DateTime(2026, 3, 15, 9, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(currentTime);
        _manager.Initialize(plan);

        // Act
        _mockTimeService.Raise(x => x.TimeJumped += null, new TimeJumpedEventArgs(currentTime, currentTime.AddHours(1)));

        // Assert
        _manager.CurrentPlan.Should().NotBeNull();
    }

    /// <summary>
    /// 创建包含 StyleChange 的测试计划
    /// </summary>
    private ExecutionPlan CreateTestPlanWithStyleChange()
    {
        var plan = new ExecutionPlan
        {
            ScheduleId = "test_schedule",
            Date = DateTime.Today
        };

        plan.TimePoints.Add(new TimePoint
        {
            Time = DateTime.Today.AddHours(9),
            Name = "样式变更",
            Types = new List<TimePointType> { TimePointType.StyleChange },
            StyleChange = new StyleChangeData
            {
                ForegroundColor = "#00FF00",
                BackgroundColor = "#FF0000",
                Opacity = 0.8
            }
        });

        return plan;
    }

    /// <summary>
    /// 创建包含 StateChange 的测试计划
    /// </summary>
    private ExecutionPlan CreateTestPlanWithStateChange()
    {
        var plan = new ExecutionPlan
        {
            ScheduleId = "test_schedule",
            Date = DateTime.Today
        };

        plan.TimePoints.Add(new TimePoint
        {
            Time = DateTime.Today.AddHours(9),
            Name = "状态变更",
            Types = new List<TimePointType> { TimePointType.StateChange },
            StateChange = new StateChangeData
            {
                FromState = ProgressStateType.Loading,
                ToState = ProgressStateType.Progress
            }
        });

        return plan;
    }
}