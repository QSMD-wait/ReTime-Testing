using ReTime_Testing.Models;
using ReTime_Testing.Services;
using FluentAssertions;

namespace ReTime_Testing.Tests.Services;

/// <summary>
/// TimeScheduleValidator 类型特定验证的单元测试
/// 测试 StateChange 和 StyleChange 类型的验证逻辑
/// </summary>
public class TimeScheduleValidatorTypeSpecificTests
{
    private readonly TimeScheduleValidator _validator;

    public TimeScheduleValidatorTypeSpecificTests()
    {
        _validator = new TimeScheduleValidator();
    }

    [Fact]
    public void Validate_StateChange类型_StateChange为null_应该返回错误()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StateChange,
                    StateChange = null
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("StateChange 为 null"));
    }

    [Fact]
    public void Validate_StateChange类型_StateChange不为null_应该通过验证()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StateChange,
                    StateChange = new StateChangeData
                    {
                        ToState = ProgressStateType.Success
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StateChange类型_ToState为Progress_应该返回错误()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StateChange,
                    StateChange = new StateChangeData
                    {
                        ToState = ProgressStateType.Progress
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("不能设置 Progress 状态"));
    }

    [Fact]
    public void Validate_StateChange类型_ToState为Loading_应该通过验证()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StateChange,
                    StateChange = new StateChangeData
                    {
                        ToState = ProgressStateType.Loading
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StateChange类型_ToState为Success_应该通过验证()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StateChange,
                    StateChange = new StateChangeData
                    {
                        ToState = ProgressStateType.Success
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StyleChange类型_StyleChange为null_应该返回错误()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StyleChange,
                    StyleChange = null
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("StyleChange 为 null"));
    }

    [Fact]
    public void Validate_StyleChange类型_StyleChange不为null_应该通过验证()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StyleChange,
                    StyleChange = new StyleChangeData
                    {
                        ForegroundColor = "#00FF00"
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StyleChange类型_所有属性为null_应该返回错误()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StyleChange,
                    StyleChange = new StyleChangeData
                    {
                        ForegroundColor = null,
                        BackgroundColor = null,
                        Opacity = null
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("StyleChange 必须至少包含一个属性"));
    }

    [Fact]
    public void Validate_StyleChange类型_只有ForegroundColor_应该通过验证()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StyleChange,
                    StyleChange = new StyleChangeData
                    {
                        ForegroundColor = "#00FF00"
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StyleChange类型_只有BackgroundColor_应该通过验证()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StyleChange,
                    StyleChange = new StyleChangeData
                    {
                        BackgroundColor = "#FF0000"
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StyleChange类型_只有Opacity_应该通过验证()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StyleChange,
                    StyleChange = new StyleChangeData
                    {
                        Opacity = 0.5
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StyleChange类型_所有属性都有值_应该通过验证()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "09:00:00",
                    Type = TimePointType.StyleChange,
                    StyleChange = new StyleChangeData
                    {
                        ForegroundColor = "#00FF00",
                        BackgroundColor = "#FF0000",
                        Opacity = 0.8
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StyleChange类型_可以在时间段内部()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "class_001",
                    Name = "第一节课",
                    StartTime = "08:00:00",
                    EndTime = "09:00:00"
                }
            },
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "08:30:00",
                    Type = TimePointType.StyleChange,
                    StyleChange = new StyleChangeData
                    {
                        ForegroundColor = "#00FF00"
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_StateChange类型_不可以在时间段内部()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "class_001",
                    Name = "第一节课",
                    StartTime = "08:00:00",
                    EndTime = "09:00:00"
                }
            },
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_test",
                    Name = "测试时间点",
                    Time = "08:30:00",
                    Type = TimePointType.StateChange,
                    StateChange = new StateChangeData
                    {
                        ToState = ProgressStateType.Success
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("位于时间段") && error.Contains("内部"));
    }

    [Fact]
    public void Validate_混合类型_应该分别验证()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_state",
                    Name = "状态变更",
                    Time = "09:00:00",
                    Type = TimePointType.StateChange,
                    StateChange = new StateChangeData
                    {
                        ToState = ProgressStateType.Success
                    }
                },
                new CustomTimePoint
                {
                    Id = "tp_style",
                    Name = "样式变更",
                    Time = "10:00:00",
                    Type = TimePointType.StyleChange,
                    StyleChange = new StyleChangeData
                    {
                        ForegroundColor = "#00FF00"
                    }
                }
            }
        };

        // Act
        var result = _validator.Validate(schedule);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}