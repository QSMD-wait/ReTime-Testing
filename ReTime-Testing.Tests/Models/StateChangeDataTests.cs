using ReTime_Testing.Models;

namespace ReTime_Testing.Tests.Models;

/// <summary>
/// StateChangeData 类的单元测试
/// 测试状态变更数据的基本功能
/// </summary>
public class StateChangeDataTests
{
    [Fact]
    public void Constructor_默认构造函数_应该创建空对象()
    {
        // Arrange & Act
        var stateChange = new StateChangeData();

        // Assert
        stateChange.ToState.Should().Be(default);
        stateChange.FromState.Should().BeNull();
    }

    [Fact]
    public void Constructor_设置ToState_应该正确保存()
    {
        // Arrange & Act
        var stateChange = new StateChangeData
        {
            ToState = ProgressStateType.Success
        };

        // Assert
        stateChange.ToState.Should().Be(ProgressStateType.Success);
    }

    [Fact]
    public void Constructor_设置FromState_应该正确保存()
    {
        // Arrange & Act
        var stateChange = new StateChangeData
        {
            FromState = ProgressStateType.Loading
        };

        // Assert
        stateChange.FromState.Should().Be(ProgressStateType.Loading);
    }

    [Fact]
    public void Constructor_设置所有属性_应该正确保存()
    {
        // Arrange & Act
        var stateChange = new StateChangeData
        {
            FromState = ProgressStateType.Loading,
            ToState = ProgressStateType.Progress
        };

        // Assert
        stateChange.FromState.Should().Be(ProgressStateType.Loading);
        stateChange.ToState.Should().Be(ProgressStateType.Progress);
    }

    [Fact]
    public void ToState_设置为Loading_应该正确保存()
    {
        // Arrange & Act
        var stateChange = new StateChangeData
        {
            ToState = ProgressStateType.Loading
        };

        // Assert
        stateChange.ToState.Should().Be(ProgressStateType.Loading);
    }

    [Fact]
    public void ToState_设置为Progress_应该正确保存()
    {
        // Arrange & Act
        var stateChange = new StateChangeData
        {
            ToState = ProgressStateType.Progress
        };

        // Assert
        stateChange.ToState.Should().Be(ProgressStateType.Progress);
    }

    [Fact]
    public void ToState_设置为Success_应该正确保存()
    {
        // Arrange & Act
        var stateChange = new StateChangeData
        {
            ToState = ProgressStateType.Success
        };

        // Assert
        stateChange.ToState.Should().Be(ProgressStateType.Success);
    }

    [Fact]
    public void ToState_设置为Error_应该正确保存()
    {
        // Arrange & Act
        var stateChange = new StateChangeData
        {
            ToState = ProgressStateType.Error
        };

        // Assert
        stateChange.ToState.Should().Be(ProgressStateType.Error);
    }

    [Fact]
    public void ToState_设置为Paused_应该正确保存()
    {
        // Arrange & Act
        var stateChange = new StateChangeData
        {
            ToState = ProgressStateType.Paused
        };

        // Assert
        stateChange.ToState.Should().Be(ProgressStateType.Paused);
    }

    [Fact]
    public void FromState_设置为null_应该正确保存()
    {
        // Arrange & Act
        var stateChange = new StateChangeData
        {
            FromState = null
        };

        // Assert
        stateChange.FromState.Should().BeNull();
    }

    [Fact]
    public void FromState_设置为Loading_应该正确保存()
    {
        // Arrange & Act
        var stateChange = new StateChangeData
        {
            FromState = ProgressStateType.Loading
        };

        // Assert
        stateChange.FromState.Should().Be(ProgressStateType.Loading);
    }

    [Fact]
    public void Clone_应该创建独立副本()
    {
        // Arrange
        var original = new StateChangeData
        {
            FromState = ProgressStateType.Loading,
            ToState = ProgressStateType.Progress
        };

        // Act
        var cloned = new StateChangeData
        {
            FromState = original.FromState,
            ToState = original.ToState
        };

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.FromState.Should().Be(original.FromState);
        cloned.ToState.Should().Be(original.ToState);
    }

    [Fact]
    public void Clone_修改副本不应影响原对象()
    {
        // Arrange
        var original = new StateChangeData
        {
            FromState = ProgressStateType.Loading,
            ToState = ProgressStateType.Progress
        };
        var cloned = new StateChangeData
        {
            FromState = original.FromState,
            ToState = original.ToState
        };

        // Act
        cloned.ToState = ProgressStateType.Success;

        // Assert
        original.ToState.Should().Be(ProgressStateType.Progress);
    }
}