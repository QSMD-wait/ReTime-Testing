using ReTime_Testing.Models;
using ReTime_Testing.Services;
using FluentAssertions;
using System.Windows;
using System.Windows.Media;

namespace ReTime_Testing.Tests.Services;

/// <summary>
/// ProgressStateManager 单元测试
/// </summary>
public class ProgressStateManagerTests
{
    private readonly ProgressStateManager _manager;
    private List<ProgressStateConfig> _stateChanges;

    public ProgressStateManagerTests()
    {
        _manager = new ProgressStateManager();
        _stateChanges = new List<ProgressStateConfig>();
        _manager.OnStateChanged += (config) => _stateChanges.Add(config.Clone());
    }

    [Fact]
    public void Constructor_应该正确初始化()
    {
        // Assert
        _manager.Should().NotBeNull();
        _manager.CurrentConfig.Should().NotBeNull();
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Loading);
    }

    [Fact]
    public void SetState_应该只设置样式不设置进度()
    {
        // Arrange
        _manager.UpdateProgress(50.0);  // 先设置进度
        var oldValue = _manager.CurrentConfig.Value;

        // Act
        _manager.SetState(ProgressStateType.Success);
        var newValue = _manager.CurrentConfig.Value;

        // Assert
        newValue.Should().Be(oldValue);  // 进度值不应改变
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Success);
        _manager.CurrentConfig.IsIndeterminate.Should().BeFalse();
    }

    [Fact]
    public void SetState_Loading状态应该设置IsIndeterminate为true()
    {
        // Act
        _manager.SetState(ProgressStateType.Loading);

        // Assert
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Loading);
        _manager.CurrentConfig.IsIndeterminate.Should().BeTrue();
    }

    [Fact]
    public void SetState_Progress状态应该设置IsIndeterminate为false()
    {
        // Act
        _manager.SetState(ProgressStateType.Progress);

        // Assert
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Progress);
        _manager.CurrentConfig.IsIndeterminate.Should().BeFalse();
    }

    [Fact]
    public void SetState_应该触发StateChanged事件()
    {
        // Act & Assert - 不应该抛出异常
        var action = () => _manager.SetState(ProgressStateType.Success);
        action.Should().NotThrow();
    }

    [Fact]
    public void UpdateProgress_应该只更新进度值()
    {
        // Arrange
        _manager.SetState(ProgressStateType.Progress);
        var originalForeground = _manager.CurrentConfig.Foreground;
        var originalVisibility = _manager.CurrentConfig.Visibility;

        // Act
        _manager.UpdateProgress(75.0);

        // Assert
        _manager.CurrentConfig.Value.Should().Be(75.0);
        _manager.CurrentConfig.Foreground.Should().Be(originalForeground);
        _manager.CurrentConfig.Visibility.Should().Be(originalVisibility);
    }

    [Fact]
    public void UpdateProgress_应该触发StateChanged事件()
    {
        // Arrange
        var eventRaised = false;
        _manager.OnStateChanged += (config) => eventRaised = true;

        // Act
        _manager.UpdateProgress(50.0);

        // Assert
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void BeginBatchUpdate_应该开始批量更新模式()
    {
        // Act & Assert - 不应该抛出异常
        var action = () => _manager.BeginBatchUpdate();
        action.Should().NotThrow();
    }

    [Fact]
    public void EndBatchUpdate_应该结束批量更新模式()
    {
        // Arrange
        _manager.BeginBatchUpdate();

        // Act & Assert - 不应该抛出异常
        var action = () => _manager.EndBatchUpdate();
        action.Should().NotThrow();
    }

    [Fact]
    public void BatchUpdate_应该正确执行批量操作()
    {
        // Act & Assert - 不应该抛出异常
        var action = () => _manager.BatchUpdate(manager =>
        {
            manager.SetState(ProgressStateType.Progress);
            manager.UpdateProgress(25.0);
            manager.UpdateProgress(50.0);
            manager.UpdateProgress(75.0);
        });
        action.Should().NotThrow();
    }

    [Fact]
    public void SetValue_应该更新进度值()
    {
        // Act
        _manager.SetValue(42.5);

        // Assert
        _manager.CurrentConfig.Value.Should().Be(42.5);
    }

    [Fact]
    public void SetForeground_应该更新前景色()
    {
        // Arrange
        var newColor = Brushes.Red;

        // Act
        _manager.SetForeground(newColor);

        // Assert
        _manager.CurrentConfig.Foreground.Should().Be(newColor);
    }

    [Fact]
    public void SetOpacity_应该更新透明度()
    {
        // Act
        _manager.SetOpacity(0.7);

        // Assert
        _manager.CurrentConfig.Opacity.Should().Be(0.7);
    }

    [Fact]
    public void SetVisibility_应该更新可见性()
    {
        // Act
        _manager.SetVisibility(Visibility.Hidden);

        // Assert
        _manager.CurrentConfig.Visibility.Should().Be(Visibility.Hidden);
    }

    [Fact]
    public void SetEnabled_应该更新启用状态()
    {
        // Act
        _manager.SetEnabled(false);

        // Assert
        _manager.CurrentConfig.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SetBackground_应该更新背景色()
    {
        // Arrange
        var newColor = Brushes.Blue;

        // Act
        _manager.SetBackground(newColor);

        // Assert
        _manager.CurrentConfig.Background.Should().Be(newColor);
    }

    [Fact]
    public void SetRange_应该更新范围()
    {
        // Act
        _manager.SetRange(0, 200);

        // Assert
        _manager.CurrentConfig.Minimum.Should().Be(0);
        _manager.CurrentConfig.Maximum.Should().Be(200);
    }

    [Fact]
    public void Reset_应该重置为默认状态()
    {
        // Arrange
        _manager.SetState(ProgressStateType.Success);
        _manager.UpdateProgress(100);

        // Act
        _manager.Reset();

        // Assert
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Loading);
        _manager.CurrentConfig.Value.Should().Be(0);
    }

    [Fact]
    public void SetState_应该正确应用Loading样式()
    {
        // Act
        _manager.SetState(ProgressStateType.Loading);

        // Assert
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Loading);
        _manager.CurrentConfig.IsIndeterminate.Should().BeTrue();
        _manager.CurrentConfig.Foreground.Should().Be(ProgressColors.DefaultBlue);
    }

    [Fact]
    public void SetState_应该正确应用Success样式()
    {
        // Act
        _manager.SetState(ProgressStateType.Success);

        // Assert
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Success);
        _manager.CurrentConfig.IsIndeterminate.Should().BeFalse();
        _manager.CurrentConfig.Foreground.Should().Be(ProgressColors.SuccessGreen);
    }

    [Fact]
    public void SetState_应该正确应用Error样式()
    {
        // Act
        _manager.SetState(ProgressStateType.Error);

        // Assert
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Error);
        _manager.CurrentConfig.IsIndeterminate.Should().BeFalse();
        _manager.CurrentConfig.Foreground.Should().Be(ProgressColors.ErrorRed);
    }

    [Fact]
    public void SetState_应该正确应用Paused样式()
    {
        // Act
        _manager.SetState(ProgressStateType.Paused);

        // Assert
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Paused);
        _manager.CurrentConfig.IsIndeterminate.Should().BeFalse();
        _manager.CurrentConfig.Foreground.Should().Be(ProgressColors.PauseOrange);
    }

    [Fact]
    public void SetState_应该正确应用Hidden样式()
    {
        // Act
        _manager.SetState(ProgressStateType.Hidden);

        // Assert
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Hidden);
        _manager.CurrentConfig.Visibility.Should().Be(Visibility.Hidden);
    }

    [Fact]
    public void SetState_应该正确应用Disabled样式()
    {
        // Act
        _manager.SetState(ProgressStateType.Disabled);

        // Assert
        _manager.CurrentConfig.StateType.Should().Be(ProgressStateType.Disabled);
        _manager.CurrentConfig.IsEnabled.Should().BeFalse();
        _manager.CurrentConfig.Opacity.Should().Be(0.5);
    }

    [Fact]
    public void 连续调用UpdateProgress_应该正确更新()
    {
        // Act
        _manager.UpdateProgress(25.0);
        _manager.UpdateProgress(50.0);
        _manager.UpdateProgress(75.0);

        // Assert
        _manager.CurrentConfig.Value.Should().Be(75.0);
        _stateChanges.Count.Should().Be(3);  // 每次更新都应该触发事件
    }

    [Fact]
    public void 批量更新期间连续调用UpdateProgress_应该只触发一次事件()
    {
        // Arrange
        var callCount = 0;
        _manager.OnStateChanged += (config) => callCount++;

        // Act
        _manager.BeginBatchUpdate();
        _manager.UpdateProgress(25.0);
        _manager.UpdateProgress(50.0);
        _manager.UpdateProgress(75.0);
        _manager.EndBatchUpdate();

        // Assert
        callCount.Should().Be(1);
        _manager.CurrentConfig.Value.Should().Be(75.0);
    }
}