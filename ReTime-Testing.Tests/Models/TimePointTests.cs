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
        timePoint.FromState.Should().Be(default);
        timePoint.ToState.Should().Be(default);
        timePoint.StyleOverrides.Should().BeNull();
    }

    [Fact]
    public void Constructor_带参数_应该正确设置属性()
    {
        // Arrange
        var time = DateTime.Today.AddHours(9);
        var name = "工作开始";
        var fromState = ProgressStateType.Loading;
        var toState = ProgressStateType.Progress;

        // Act
        var timePoint = new TimePoint(time, name, fromState, toState);

        // Assert
        timePoint.Time.Should().Be(time);
        timePoint.Name.Should().Be(name);
        timePoint.FromState.Should().Be(fromState);
        timePoint.ToState.Should().Be(toState);
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
        cloned.FromState.Should().Be(original.FromState);
        cloned.ToState.Should().Be(original.ToState);
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