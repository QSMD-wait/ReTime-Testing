namespace ReTime_Testing.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// ExecutionPlanGenerator 类的单元测试
/// 测试执行计划生成器的各种场景
/// 
/// v3.0 设计原则：
/// - 时间段：固定为 Progress 状态
/// - 时间点：非 Progress 状态，不能在时间段内部
/// </summary>
public class ExecutionPlanGeneratorTests
{
    private readonly ExecutionPlanGenerator _generator;

    public ExecutionPlanGeneratorTests()
    {
        _generator = new ExecutionPlanGenerator(NullLogger<ExecutionPlanGenerator>.Instance);
    }

    [Fact]
    public void Generate_简单时间段_应该生成正确的时间段()
    {
        // Arrange
        var schedule = new TimeSchedule
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
        var date = new DateTime(2026, 3, 15);
        var currentTime = date.AddHours(10);

        // Act
        var plan = _generator.Generate(schedule, date, currentTime);

        // Assert
        plan.Should().NotBeNull();
        plan.ScheduleId.Should().Be("test_schedule");
        plan.Date.Should().Be(date);

        // 验证时间段：3个（空闲开始、工作时间、空闲结束）
        plan.TimeSegments.Should().HaveCount(3);

        // 验证工作时间段时间固定为 Progress
        var workSegment = plan.TimeSegments[1];
        workSegment.Name.Should().Be("工作时间");
        workSegment.State.Should().Be(ProgressStateType.Progress);
        workSegment.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Generate_多个时间段_应该生成正确的时间段()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "上午工作",
                    StartTime = "09:00:00",
                    EndTime = "12:00:00"
                },
                new TimeScheduleItem
                {
                    Id = "2",
                    Name = "下午工作",
                    StartTime = "13:00:00",
                    EndTime = "18:00:00"
                }
            }
        };
        var date = new DateTime(2026, 3, 15);
        var currentTime = date.AddHours(10);

        // Act
        var plan = _generator.Generate(schedule, date, currentTime);

        // Assert: 5个时间段（空闲开始、上午工作、间隙、下午工作、空闲结束）
        plan.TimeSegments.Should().HaveCount(5);

        // 验证所有工作时间状态为 Progress
        plan.TimeSegments[1].State.Should().Be(ProgressStateType.Progress);
        plan.TimeSegments[1].Name.Should().Be("上午工作");
        plan.TimeSegments[3].State.Should().Be(ProgressStateType.Progress);
        plan.TimeSegments[3].Name.Should().Be("下午工作");

        // 验证间隙状态为 Loading
        plan.TimeSegments[2].State.Should().Be(ProgressStateType.Loading);
    }

    [Fact]
    public void Generate_当前在工作时间_应该正确设置当前状态()
    {
        // Arrange
        var schedule = new TimeSchedule
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
        var date = new DateTime(2026, 3, 15);
        var currentTime = date.AddHours(10).AddMinutes(30);

        // Act
        var plan = _generator.Generate(schedule, date, currentTime);

        // Assert
        plan.CurrentSegment.Should().NotBeNull();
        plan.CurrentSegment!.Name.Should().Be("工作时间");
        plan.CurrentSegment.State.Should().Be(ProgressStateType.Progress);
        plan.CurrentSegment.IsActive.Should().BeTrue();
        plan.CurrentSegment.Contains(currentTime).Should().BeTrue();
    }

    [Fact]
    public void Generate_当前在空闲时间_应该正确设置当前状态()
    {
        // Arrange
        var schedule = new TimeSchedule
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
        var date = new DateTime(2026, 3, 15);
        var currentTime = date.AddHours(8);

        // Act
        var plan = _generator.Generate(schedule, date, currentTime);

        // Assert
        plan.CurrentSegment.Should().NotBeNull();
        plan.CurrentSegment!.Name.Should().Be("空闲");
        plan.CurrentSegment.State.Should().Be(ProgressStateType.Loading);
        plan.CurrentSegment.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Generate_空时间计划_应该生成全天空闲时间段()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>()
        };
        var date = new DateTime(2026, 3, 15);

        // Act
        var plan = _generator.Generate(schedule, date, date.AddHours(10));

        // Assert
        plan.TimeSegments.Should().HaveCount(1);

        var segment = plan.TimeSegments[0];
        segment.Name.Should().Be("空闲");
        segment.State.Should().Be(ProgressStateType.Loading);
        segment.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Generate_包含秒数的时间_应该正确解析()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "精确时间",
                    StartTime = "09:30:15",
                    EndTime = "17:45:30"
                }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act
        var plan = _generator.Generate(schedule, date, date.AddHours(10));

        // Assert
        var workSegment = plan.TimeSegments[1];
        workSegment.StartTime.Should().Be(date.AddHours(9).AddMinutes(30).AddSeconds(15));
        workSegment.EndTime.Should().Be(date.AddHours(17).AddMinutes(45).AddSeconds(30));
    }

    [Fact]
    public void Generate_时间段应该连续无重叠()
    {
        // Arrange
        var schedule = new TimeSchedule
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
        var date = new DateTime(2026, 3, 15);

        // Act
        var plan = _generator.Generate(schedule, date, date.AddHours(10));

        // Assert
        for (int i = 1; i < plan.TimeSegments.Count; i++)
        {
            plan.TimeSegments[i].StartTime.Should().Be(plan.TimeSegments[i - 1].EndTime);
        }
    }

    [Fact]
    public void Generate_无效时间格式_应该抛出异常()
    {
        // Arrange: v3.0 配置验证会抛出异常
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "有效时间段",
                    StartTime = "09:00:00",
                    EndTime = "12:00:00"
                },
                new TimeScheduleItem
                {
                    Id = "2",
                    Name = "无效时间段",
                    StartTime = "invalid",
                    EndTime = "invalid"
                }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act & Assert
        var act = () => _generator.Generate(schedule, date, date.AddHours(10));
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("时间格式无效");
    }

    [Fact]
    public void Generate_跨午夜运行_应该正确处理日期()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "夜间工作",
                    StartTime = "22:00:00",
                    EndTime = "02:00:00" // 次日 02:00
                }
            }
        };
        var date = new DateTime(2026, 3, 15);
        var currentTime = new DateTime(2026, 3, 16, 0, 0, 0);

        // Act
        var plan = _generator.Generate(schedule, date, currentTime);

        // Assert: 跨午夜时间段会生成 2 个时间段（空闲开始、夜间工作）
        // 因为结束时间是次日 02:00，超过了当天的 24:00，所以没有"空闲结束"段
        plan.TimeSegments.Should().HaveCount(2);

        // 验证当前状态
        plan.CurrentSegment.Should().NotBeNull();
        plan.CurrentSegment!.Contains(currentTime).Should().BeTrue();
        plan.CurrentSegment.State.Should().Be(ProgressStateType.Progress);
        plan.CurrentSegment.Name.Should().Be("夜间工作");
    }

    [Fact]
    public void UpdateCurrentState_在时间段中间_应该返回该时间段()
    {
        // Arrange
        var plan = TestDataHelper.CreateSimpleExecutionPlan();
        var testTime = DateTime.Today.AddHours(10).AddMinutes(30);

        // Act
        plan.UpdateCurrentState(testTime);

        // Assert
        plan.CurrentSegment.Should().NotBeNull();
        plan.CurrentSegment!.Name.Should().Contain("工作");
        plan.CurrentSegment.Contains(testTime).Should().BeTrue();
    }

    [Fact]
    public void Clone_应该创建独立的副本()
    {
        // Arrange
        var original = TestDataHelper.CreateSimpleExecutionPlan();

        // Act
        var cloned = original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.ScheduleId.Should().Be(original.ScheduleId);
        cloned.Date.Should().Be(original.Date);
        cloned.TimeSegments.Should().HaveCount(original.TimeSegments.Count);
    }

    #region 时间点测试（v3.0）

    [Fact]
    public void Generate_时间点在时间段结束时间_应该覆盖自动生成()
    {
        // Arrange: 时间点 = 时间段结束时间，应该覆盖自动生成的结束时间点
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "工作时间",
                    StartTime = "09:00:00",
                    EndTime = "10:00:00"
                }
            },
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_end",
                    Time = "10:00:00",
                    StateChange = new StateChangeData { ToState = ProgressStateType.Success }
                },
                new CustomTimePoint
                {
                    Id = "tp_delay",
                    Time = "10:00:10",
                    StateChange = new StateChangeData { ToState = ProgressStateType.Loading }
                }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act
        var plan = _generator.Generate(schedule, date, date.AddHours(9));

        // Assert: 3个时间点（开始自动 + 结束覆盖 + 延迟）
        plan.TimePoints.Should().HaveCount(3);
        plan.TimePoints[0].TryGetToState(out var toState0).Should().BeTrue();
        toState0.Should().Be(ProgressStateType.Progress); // 自动生成的开始
        plan.TimePoints[1].TryGetToState(out var toState1).Should().BeTrue();
        toState1.Should().Be(ProgressStateType.Success);  // 覆盖的结束
        plan.TimePoints[2].TryGetToState(out var toState2).Should().BeTrue();
        toState2.Should().Be(ProgressStateType.Loading);  // 延迟
    }

    [Fact]
    public void Generate_时间点在时间段开始时间_应该覆盖自动生成()
    {
        // Arrange: 时间点 = 时间段开始时间，应该覆盖自动生成的开始时间点
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "工作时间",
                    StartTime = "09:00:00",
                    EndTime = "10:00:00"
                }
            },
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_start",
                    Time = "09:00:00",
                    StateChange = new StateChangeData { ToState = ProgressStateType.Paused } // 覆盖自动生成的开始时间点
                }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act
        var plan = _generator.Generate(schedule, date, date.AddHours(8));

        // Assert: 2个时间点（开始覆盖 + 结束自动）
        plan.TimePoints.Should().HaveCount(2);
        plan.TimePoints[0].TryGetToState(out var toState0).Should().BeTrue();
        toState0.Should().Be(ProgressStateType.Paused);   // 覆盖的开始
        plan.TimePoints[1].TryGetToState(out var toState1).Should().BeTrue();
        toState1.Should().Be(ProgressStateType.Loading);  // 自动生成的结束
    }

    [Fact]
    public void Generate_时间点设置Progress状态_应该抛出异常()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "工作时间",
                    StartTime = "09:00:00",
                    EndTime = "10:00:00"
                }
            },
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_invalid",
                    Time = "08:00:00",
                    StateChange = new StateChangeData { ToState = ProgressStateType.Progress } // 不允许
                }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act & Assert
        var act = () => _generator.Generate(schedule, date, date.AddHours(8));
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("不能设置 Progress 状态");
    }

    [Fact]
    public void Generate_时间点在时间段内部_应该抛出异常()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "工作时间",
                    StartTime = "09:00:00",
                    EndTime = "10:00:00"
                }
            },
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint
                {
                    Id = "tp_inside",
                    Time = "09:30:00", // 在时间段内部
                    StateChange = new StateChangeData { ToState = ProgressStateType.Paused }
                }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act & Assert
        var act = () => _generator.Generate(schedule, date, date.AddHours(8));
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("位于时间段");
    }

    [Fact]
    public void Generate_时间段重叠_应该抛出异常()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "时间段1",
                    StartTime = "09:00:00",
                    EndTime = "10:30:00"
                },
                new TimeScheduleItem
                {
                    Id = "2",
                    Name = "时间段2",
                    StartTime = "10:00:00", // 与时间段1重叠
                    EndTime = "11:00:00"
                }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act & Assert
        var act = () => _generator.Generate(schedule, date, date.AddHours(8));
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("重叠");
    }

    [Fact]
    public void Generate_时间点ID重复_应该抛出异常()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>(),
            TimePoints = new List<CustomTimePoint>
            {
                new CustomTimePoint { Id = "tp1", Time = "09:00:00", StateChange = new StateChangeData { ToState = ProgressStateType.Success } },
                new CustomTimePoint { Id = "tp1", Time = "10:00:00", StateChange = new StateChangeData { ToState = ProgressStateType.Loading } }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act & Assert
        var act = () => _generator.Generate(schedule, date, date.AddHours(8));
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("ID 重复");
    }

    [Fact]
    public void Generate_时间段ID重复_应该抛出异常()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem { Id = "1", StartTime = "09:00:00", EndTime = "10:00:00" },
                new TimeScheduleItem { Id = "1", StartTime = "11:00:00", EndTime = "12:00:00" }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act & Assert
        var act = () => _generator.Generate(schedule, date, date.AddHours(8));
        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("ID 重复");
    }

    #endregion

    #region 进度计算测试

    [Fact]
    public void Generate_时间段进度应该独立计算()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "工作时间",
                    StartTime = "09:00:00",
                    EndTime = "10:00:00" // 60分钟
                }
            }
        };
        var date = new DateTime(2026, 3, 15);
        var currentTime = date.AddHours(9).AddMinutes(30); // 30分钟 = 50%

        // Act
        var plan = _generator.Generate(schedule, date, currentTime);

        // Assert
        plan.CurrentSegment.Should().NotBeNull();
        plan.CurrentSegment!.StartTime.Should().Be(date.AddHours(9));
        plan.CurrentSegment.EndTime.Should().Be(date.AddHours(10));
    }

    #endregion

    #region 向后兼容性测试

    [Fact]
    public void Generate_不配置TimePoints_应该正常工作()
    {
        // Arrange
        var schedule = new TimeSchedule
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
        var date = new DateTime(2026, 3, 15);

        // Act
        var plan = _generator.Generate(schedule, date, date.AddHours(10));

        // Assert
        plan.TimeSegments.Should().HaveCount(3);
        plan.TimeSegments[1].State.Should().Be(ProgressStateType.Progress);
    }

    [Fact]
    public void Generate_配置State字段_应该被忽略()
    {
        // Arrange: State 字段已废弃，应该被忽略
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "工作时间",
                    StartTime = "09:00:00",
                    EndTime = "10:00:00",
                    State = ProgressStateType.Paused // 应该被忽略
                }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act
        var plan = _generator.Generate(schedule, date, date.AddHours(9));

        // Assert: 状态固定为 Progress
        plan.TimeSegments[1].State.Should().Be(ProgressStateType.Progress);
    }

    #endregion
}