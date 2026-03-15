namespace ReTime_Testing.Tests.Services;

/// <summary>
/// ExecutionPlanGenerator 类的单元测试
/// 测试执行计划生成器的各种场景
/// </summary>
public class ExecutionPlanGeneratorTests
{
    private readonly ExecutionPlanGenerator _generator;

    public ExecutionPlanGeneratorTests()
    {
        _generator = new ExecutionPlanGenerator();
    }

    [Fact]
    public void Generate_简单时间段_应该生成正确的时间点()
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
        plan.TimePoints.Should().HaveCount(2);

        // 验证第一个时间点（开始）
        var startPoint = plan.TimePoints[0];
        startPoint.Time.Should().Be(date.AddHours(9));
        startPoint.Name.Should().Contain("开始");
        startPoint.FromState.Should().Be(ProgressStateType.Loading);
        startPoint.ToState.Should().Be(ProgressStateType.Progress);

        // 验证第二个时间点（结束）
        var endPoint = plan.TimePoints[1];
        endPoint.Time.Should().Be(date.AddHours(18));
        endPoint.Name.Should().Contain("结束");
        endPoint.FromState.Should().Be(ProgressStateType.Progress);
        endPoint.ToState.Should().Be(ProgressStateType.Success);
    }

    [Fact]
    public void Generate_多个时间段_应该生成正确的时间点()
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

        // Assert
        plan.TimePoints.Should().HaveCount(4);

        // 验证时间点顺序
        plan.TimePoints[0].Time.Should().Be(date.AddHours(9));   // 上午开始
        plan.TimePoints[1].Time.Should().Be(date.AddHours(12));  // 上午结束
        plan.TimePoints[2].Time.Should().Be(date.AddHours(13));  // 下午开始
        plan.TimePoints[3].Time.Should().Be(date.AddHours(18));  // 下午结束

        // 验证状态切换
        plan.TimePoints[0].ToState.Should().Be(ProgressStateType.Progress);
        plan.TimePoints[1].ToState.Should().Be(ProgressStateType.Success);
        plan.TimePoints[2].ToState.Should().Be(ProgressStateType.Progress);
        plan.TimePoints[3].ToState.Should().Be(ProgressStateType.Success);
    }

    [Fact]
    public void Generate_简单时间段_应该生成连续的时间段()
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

        // 验证空闲开始时间段
        var idleStart = plan.TimeSegments[0];
        idleStart.Name.Should().Be("空闲");
        idleStart.State.Should().Be(ProgressStateType.Loading);
        idleStart.IsActive.Should().BeFalse();

        // 验证工作时间段
        var workSegment = plan.TimeSegments[1];
        workSegment.Name.Should().Contain("开始");
        workSegment.State.Should().Be(ProgressStateType.Progress);
        workSegment.IsActive.Should().BeTrue();
        workSegment.StartTime.Should().Be(date.AddHours(9));
        workSegment.EndTime.Should().Be(date.AddHours(18));

        // 验证空闲结束时间段
        var idleEnd = plan.TimeSegments[2];
        idleEnd.Name.Should().Be("空闲");
        idleEnd.State.Should().Be(ProgressStateType.Loading);
        idleEnd.IsActive.Should().BeFalse();
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
        plan.CurrentSegment!.Name.Should().Contain("开始");
        plan.CurrentSegment.State.Should().Be(ProgressStateType.Progress);
        plan.CurrentSegment.IsActive.Should().BeTrue();
        plan.CurrentSegment.Contains(currentTime).Should().BeTrue();

        plan.NextTimePoint.Should().NotBeNull();
        plan.NextTimePoint!.Time.Should().Be(date.AddHours(18));
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
        plan.CurrentSegment.Contains(currentTime).Should().BeTrue();

        plan.NextTimePoint.Should().NotBeNull();
        plan.NextTimePoint!.Time.Should().Be(date.AddHours(9));
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
        plan.TimePoints.Should().BeEmpty();
        plan.TimeSegments.Should().HaveCount(1);

        var segment = plan.TimeSegments[0];
        segment.Name.Should().Be("空闲");
        segment.State.Should().Be(ProgressStateType.Loading);
        segment.IsActive.Should().BeFalse();
        segment.StartTime.Should().Be(date.Date);
        segment.EndTime.Should().Be(date.Date.AddDays(1).AddTicks(-1));
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
        plan.TimePoints[0].Time.Should().Be(date.AddHours(9).AddMinutes(30).AddSeconds(15));
        plan.TimePoints[1].Time.Should().Be(date.AddHours(17).AddMinutes(45).AddSeconds(30));
    }

    [Fact]
    public void Generate_时间点应该按时间排序()
    {
        // Arrange
        var schedule = new TimeSchedule
        {
            Id = "test_schedule",
            Schedules = new List<TimeScheduleItem>
            {
                new TimeScheduleItem
                {
                    Id = "2",
                    Name = "下午工作",
                    StartTime = "14:00:00",
                    EndTime = "17:00:00"
                },
                new TimeScheduleItem
                {
                    Id = "1",
                    Name = "上午工作",
                    StartTime = "09:00:00",
                    EndTime = "12:00:00"
                }
            }
        };
        var date = new DateTime(2026, 3, 15);

        // Act
        var plan = _generator.Generate(schedule, date, date.AddHours(10));

        // Assert
        for (int i = 1; i < plan.TimePoints.Count; i++)
        {
            plan.TimePoints[i].Time.Should().BeAfter(plan.TimePoints[i - 1].Time);
        }
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
    public void Generate_无效时间格式_应该跳过该项()
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

        // Act
        var plan = _generator.Generate(schedule, date, date.AddHours(10));

        // Assert
        plan.TimePoints.Should().HaveCount(2); // 只生成有效的时间点
        plan.TimeSegments.Should().NotBeEmpty();
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
                    EndTime = "02:00:00"
                }
            }
        };
        var date = new DateTime(2026, 3, 15, 23, 0, 0); // 晚上 23:00
        var currentTime = date.AddHours(1); // 次日 00:00

        // Act
        var plan = _generator.Generate(schedule, date, currentTime);

        // Assert
        plan.TimePoints.Should().HaveCount(2);
        plan.TimePoints[0].Time.Should().Be(date.AddHours(22)); // 当天 22:00
        plan.TimePoints[1].Time.Should().Be(date.AddDays(1).AddHours(2)); // 次日 02:00

        // 验证当前状态
        plan.CurrentSegment.Should().NotBeNull();
        plan.CurrentSegment!.Contains(currentTime).Should().BeTrue();
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
    public void GetTimePointsInRange_应该返回正确的时间点()
    {
        // Arrange
        var plan = TestDataHelper.CreateSimpleExecutionPlan();
        var startTime = DateTime.Today.AddHours(8);
        var endTime = DateTime.Today.AddHours(20);

        // Act
        var pointsInRange = plan.GetTimePointsInRange(startTime, endTime);

        // Assert
        pointsInRange.Should().HaveCount(2);
        pointsInRange.All(p => p.Time > startTime && p.Time <= endTime).Should().BeTrue();
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
        cloned.TimePoints.Should().HaveCount(original.TimePoints.Count);
        cloned.TimeSegments.Should().HaveCount(original.TimeSegments.Count);
    }

    [Fact]
    public void Clone_修改副本不应影响原对象()
    {
        // Arrange
        var original = TestDataHelper.CreateSimpleExecutionPlan();
        var cloned = original.Clone();

        // Act
        cloned.ScheduleId = "modified";
        cloned.TimePoints[0].Name = "modified";
        cloned.TimeSegments[0].Name = "modified";

        // Assert
        original.ScheduleId.Should().Be("test_schedule");
        original.TimePoints[0].Name.Should().Contain("开始");
        original.TimeSegments[0].Name.Should().Be("空闲");
    }
}