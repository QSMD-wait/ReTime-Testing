namespace ReTime_Testing.Tests.Models;

/// <summary>
/// TimePoint 类的单元测试
/// 测试时间点的基本功能
/// </summary>
public class TimePointTests
{
    [Fact]
    public void Constructor_默认构造函数_应该创建空时间点()
    {
        // Arrange & Act
        var timePoint = new TimePoint();

        // Assert
        timePoint.Time.Should().Be(default);
        timePoint.Name.Should().BeEmpty();
        timePoint.Type.Should().Be(TimePointType.StateChange);
        timePoint.StateChange.Should().BeNull();
        timePoint.StyleChange.Should().BeNull();
        timePoint.TryGetFromState(out _).Should().BeFalse();
        timePoint.TryGetToState(out _).Should().BeFalse();
        timePoint.GetStyleOverrides().Should().BeNull();
    }

    [Fact]
    public void Constructor_带StateChangeData_应该正确读取状态()
    {
        // Arrange
        var time = DateTime.Today.AddHours(9);
        var name = "工作开始";
        var fromState = ProgressStateType.Loading;
        var toState = ProgressStateType.Progress;
        var stateChange = new StateChangeData
        {
            FromState = fromState,
            ToState = toState
        };

        // Act
        var timePoint = new TimePoint(time, name, TimePointType.StateChange, stateChange);

        // Assert
        timePoint.Time.Should().Be(time);
        timePoint.Name.Should().Be(name);
        timePoint.Type.Should().Be(TimePointType.StateChange);
        timePoint.StateChange.Should().NotBeNull();
        timePoint.StyleChange.Should().BeNull();
        timePoint.TryGetFromState(out var actualFrom).Should().BeTrue();
        actualFrom.Should().Be(fromState);
        timePoint.TryGetToState(out var actualTo).Should().BeTrue();
        actualTo.Should().Be(toState);
    }

    [Fact]
    public void Constructor_带StyleChangeData_应该正确读取样式()
    {
        // Arrange
        var time = DateTime.Today.AddHours(12);
        var name = "样式调整";
        var styleChange = new StyleChangeData
        {
            ForegroundColor = "#00FF00",
            BackgroundColor = "#FF0000",
            Opacity = 0.8
        };

        // Act
        var timePoint = new TimePoint(time, name, TimePointType.StyleChange, null, styleChange);

        // Assert
        timePoint.Time.Should().Be(time);
        timePoint.Name.Should().Be(name);
        timePoint.Type.Should().Be(TimePointType.StyleChange);
        timePoint.StateChange.Should().BeNull();
        timePoint.StyleChange.Should().NotBeNull();
        var styleOverrides = timePoint.GetStyleOverrides();
        styleOverrides.Should().NotBeNull();
        styleOverrides!.HasAnyOverride.Should().BeTrue();
        styleOverrides.Opacity.Should().Be(0.8);
    }

    [Fact]
    public void Clone_应该创建独立副本()
    {
        // Arrange
        var original = TestDataHelper.CreateTestTimePoint(9, 0, "测试");

        // Act
        var cloned = original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.Time.Should().Be(original.Time);
        cloned.Name.Should().Be(original.Name);
        cloned.TryGetFromState(out var originalFrom).Should().BeTrue();
        cloned.TryGetFromState(out var clonedFrom).Should().BeTrue();
        clonedFrom.Should().Be(originalFrom);
        cloned.TryGetToState(out var originalTo).Should().BeTrue();
        cloned.TryGetToState(out var clonedTo).Should().BeTrue();
        clonedTo.Should().Be(originalTo);
    }

    [Fact]
    public void Clone_修改副本不应影响原对象()
    {
        // Arrange
        var original = TestDataHelper.CreateTestTimePoint(9, 0, "测试");
        var cloned = original.Clone();

        // Act
        cloned.Name = "修改后的名称";
        cloned.Time = cloned.Time.AddHours(1);

        // Assert
        original.Name.Should().Be("测试");
        original.Time.Should().Be(DateTime.Today.AddHours(9));
    }

    [Fact]
    public void ToString_应该返回格式正确的字符串()
    {
        // Arrange
        var timePoint = TestDataHelper.CreateTestTimePoint(9, 30, "工作开始", ProgressStateType.Loading, ProgressStateType.Progress);

        // Act
        var result = timePoint.ToString();

        // Assert
        result.Should().Contain("09:30");
        result.Should().Contain("工作开始");
        result.Should().Contain("Loading");
        result.Should().Contain("Progress");
    }
}