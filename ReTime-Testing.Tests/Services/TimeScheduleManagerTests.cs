using ReTime_Testing.Models;
using ReTime_Testing.Services;
using FluentAssertions;
using System.IO;

namespace ReTime_Testing.Tests.Services;

/// <summary>
/// TimeScheduleManager 单元测试
/// </summary>
public class TimeScheduleManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly TimeScheduleManager _manager;

    public TimeScheduleManagerTests()
    {
        // 创建临时测试目录
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TimeScheduleTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        // 创建测试用管理器
        _manager = TimeScheduleManager.Instance;
        
        // 使用反射设置测试目录
        var field = typeof(TimeScheduleManager).GetField("_timeSchedulesDirectory", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(_manager, _testDirectory);
    }

    public void Dispose()
    {
        // 清理测试目录
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    #region 计划表操作测试

    [Fact]
    public void GetScheduleList_空目录_应返回空列表()
    {
        // Act
        var result = _manager.GetScheduleList();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void CreateNewSchedule_应创建空白计划表()
    {
        // Act
        var result = _manager.CreateNewSchedule("test_schedule", "测试计划表");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be("test_schedule");
        result.Settings.Metadata.Name.Should().Be("测试计划表");
        result.Schedules.Should().BeEmpty();
        result.TimePoints.Should().BeEmpty();
    }

    [Fact]
    public void CreateNewSchedule_应保存到文件()
    {
        // Act
        _manager.CreateNewSchedule("file_test", "文件测试");

        // Assert
        var filePath = Path.Combine(_testDirectory, "file_test.json");
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public void CreateNewSchedule_后GetScheduleList应包含该计划表()
    {
        // Act
        _manager.CreateNewSchedule("list_test", "列表测试");
        var result = _manager.GetScheduleList();

        // Assert
        result.Should().Contain(s => s.Id == "list_test" && s.Name == "列表测试");
    }

    [Fact]
    public void CopySchedule_应复制计划表()
    {
        // Arrange
        var original = _manager.CreateNewSchedule("original", "原始计划表");
        original.Schedules.Add(new TimeScheduleItem
        {
            Id = "seg_1",
            Name = "时间段1",
            StartTime = "09:00:00",
            EndTime = "10:00:00"
        });
        original.TimePoints.Add(new CustomTimePoint
        {
            Id = "tp_1",
            Time = "10:00:00",
            StateChange = new StateChangeData
            {
                ToState = ProgressStateType.Success
            }
        });
        _manager.SaveSchedule(original);

        // Act
        var copy = _manager.CopySchedule("original", "copied");

        // Assert
        copy.Should().NotBeNull();
        copy!.Id.Should().Be("copied");
        copy.Settings.Metadata.Name.Should().Contain("副本");
        copy.Schedules.Should().HaveCount(1);
        copy.TimePoints.Should().HaveCount(1);
    }

    [Fact]
    public void CopySchedule_源不存在应返回null()
    {
        // Act
        var result = _manager.CopySchedule("nonexistent", "new_id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void RenameSchedule_应更新名称()
    {
        // Arrange
        _manager.CreateNewSchedule("rename_test", "旧名称");

        // Act
        var result = _manager.RenameSchedule("rename_test", "新名称");

        // Assert
        result.Should().BeTrue();
        var schedule = _manager.LoadSchedule("rename_test");
        schedule!.Settings.Metadata.Name.Should().Be("新名称");
    }

    [Fact]
    public void RenameSchedule_不存在应返回false()
    {
        // Act
        var result = _manager.RenameSchedule("nonexistent", "新名称");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ScheduleExists_存在应返回true()
    {
        // Arrange
        _manager.CreateNewSchedule("exists_test", "测试");

        // Act
        var result = _manager.ScheduleExists("exists_test");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ScheduleExists_不存在应返回false()
    {
        // Act
        var result = _manager.ScheduleExists("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void DeleteSchedule_应删除文件并返回true()
    {
        // Arrange
        _manager.CreateNewSchedule("delete_test", "待删除");

        // Act
        var result = _manager.DeleteSchedule("delete_test");

        // Assert
        result.Should().BeTrue();
        _manager.ScheduleExists("delete_test").Should().BeFalse();
    }

    [Fact]
    public void DeleteSchedule_不存在应返回false()
    {
        // Act
        var result = _manager.DeleteSchedule("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region 时间段操作测试

    [Fact]
    public void AddTimeSegment_应添加时间段()
    {
        // Arrange
        _manager.CreateNewSchedule("segment_test", "时间段测试");
        var segment = new TimeScheduleItem
        {
            Id = "seg_1",
            Name = "工作时间段",
            StartTime = "09:00:00",
            EndTime = "18:00:00"
        };

        // Act
        var result = _manager.AddTimeSegment("segment_test", segment);

        // Assert
        result.Should().BeTrue();
        var schedule = _manager.LoadSchedule("segment_test");
        schedule!.Schedules.Should().Contain(s => s.Id == "seg_1");
    }

    [Fact]
    public void AddTimeSegment_计划表不存在应返回false()
    {
        // Arrange
        var segment = new TimeScheduleItem { Id = "seg_1" };

        // Act
        var result = _manager.AddTimeSegment("nonexistent", segment);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateTimeSegment_应更新时间段()
    {
        // Arrange
        _manager.CreateNewSchedule("update_seg_test", "测试");
        _manager.AddTimeSegment("update_seg_test", new TimeScheduleItem
        {
            Id = "seg_1",
            Name = "旧名称",
            StartTime = "09:00:00",
            EndTime = "18:00:00"
        });

        // Act
        var result = _manager.UpdateTimeSegment("update_seg_test", new TimeScheduleItem
        {
            Id = "seg_1",
            Name = "新名称",
            StartTime = "08:00:00",
            EndTime = "17:00:00"
        });

        // Assert
        result.Should().BeTrue();
        var schedule = _manager.LoadSchedule("update_seg_test");
        var updated = schedule!.Schedules.First(s => s.Id == "seg_1");
        updated.Name.Should().Be("新名称");
        updated.StartTime.Should().Be("08:00:00");
    }

    [Fact]
    public void UpdateTimeSegment_时间段不存在应返回false()
    {
        // Arrange
        _manager.CreateNewSchedule("update_seg_test2", "测试");

        // Act
        var result = _manager.UpdateTimeSegment("update_seg_test2", new TimeScheduleItem
        {
            Id = "nonexistent_seg"
        });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveTimeSegment_应删除时间段()
    {
        // Arrange
        _manager.CreateNewSchedule("remove_seg_test", "测试");
        _manager.AddTimeSegment("remove_seg_test", new TimeScheduleItem
        {
            Id = "seg_to_remove",
            Name = "待删除",
            StartTime = "09:00:00",
            EndTime = "10:00:00"
        });

        // Act
        var result = _manager.RemoveTimeSegment("remove_seg_test", "seg_to_remove");

        // Assert
        result.Should().BeTrue();
        var schedule = _manager.LoadSchedule("remove_seg_test");
        schedule!.Schedules.Should().NotContain(s => s.Id == "seg_to_remove");
    }

    [Fact]
    public void RemoveTimeSegment_时间段不存在应返回false()
    {
        // Arrange
        _manager.CreateNewSchedule("remove_seg_test2", "测试");

        // Act
        var result = _manager.RemoveTimeSegment("remove_seg_test2", "nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region 时间点操作测试

    [Fact]
    public void AddTimePoint_应添加时间点()
    {
        // Arrange
        _manager.CreateNewSchedule("tp_test", "时间点测试");
        var timePoint = new CustomTimePoint
        {
            Id = "tp_1",
            Name = "结束提示",
            Time = "18:00:00",
            StateChange = new StateChangeData
            {
                ToState = ProgressStateType.Success
            }
        };

        // Act
        var result = _manager.AddTimePoint("tp_test", timePoint);

        // Assert
        result.Should().BeTrue();
        var schedule = _manager.LoadSchedule("tp_test");
        schedule!.TimePoints.Should().Contain(t => t.Id == "tp_1");
    }

    [Fact]
    public void AddTimePoint_计划表不存在应返回false()
    {
        // Arrange
        var timePoint = new CustomTimePoint { Id = "tp_1" };

        // Act
        var result = _manager.AddTimePoint("nonexistent", timePoint);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UpdateTimePoint_应更新时间点()
    {
        // Arrange
        _manager.CreateNewSchedule("update_tp_test", "测试");
        _manager.AddTimePoint("update_tp_test", new CustomTimePoint
        {
            Id = "tp_1",
            Name = "旧名称",
            Time = "18:00:00",
            StateChange = new StateChangeData
            {
                ToState = ProgressStateType.Success
            }
        });

        // Act
        var result = _manager.UpdateTimePoint("update_tp_test", new CustomTimePoint
        {
            Id = "tp_1",
            Name = "新名称",
            Time = "17:00:00",
            StateChange = new StateChangeData
            {
                ToState = ProgressStateType.Loading
            }
        });

        // Assert
        result.Should().BeTrue();
        var schedule = _manager.LoadSchedule("update_tp_test");
        var updated = schedule!.TimePoints.First(t => t.Id == "tp_1");
        updated.Name.Should().Be("新名称");
        updated.Time.Should().Be("17:00:00");
        updated.StateChange.Should().NotBeNull();
        updated.StateChange!.ToState.Should().Be(ProgressStateType.Loading);
    }

    [Fact]
    public void UpdateTimePoint_时间点不存在应返回false()
    {
        // Arrange
        _manager.CreateNewSchedule("update_tp_test2", "测试");

        // Act
        var result = _manager.UpdateTimePoint("update_tp_test2", new CustomTimePoint
        {
            Id = "nonexistent_tp"
        });

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveTimePoint_应删除时间点()
    {
        // Arrange
        _manager.CreateNewSchedule("remove_tp_test", "测试");
        _manager.AddTimePoint("remove_tp_test", new CustomTimePoint
        {
            Id = "tp_to_remove",
            Name = "待删除",
            Time = "18:00:00",
            StateChange = new StateChangeData
            {
                ToState = ProgressStateType.Success
            }
        });

        // Act
        var result = _manager.RemoveTimePoint("remove_tp_test", "tp_to_remove");

        // Assert
        result.Should().BeTrue();
        var schedule = _manager.LoadSchedule("remove_tp_test");
        schedule!.TimePoints.Should().NotContain(t => t.Id == "tp_to_remove");
    }

    [Fact]
    public void RemoveTimePoint_时间点不存在应返回false()
    {
        // Arrange
        _manager.CreateNewSchedule("remove_tp_test2", "测试");

        // Act
        var result = _manager.RemoveTimePoint("remove_tp_test2", "nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public void LoadSchedule_不存在应返回null()
    {
        // Act
        var result = _manager.LoadSchedule("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SaveSchedule_应更新缓存()
    {
        // Arrange
        var schedule = _manager.CreateNewSchedule("cache_test", "缓存测试");
        schedule.Settings.Metadata.Description = "更新描述";

        // Act
        _manager.SaveSchedule(schedule);
        var cached = _manager.LoadSchedule("cache_test");

        // Assert
        cached!.Settings.Metadata.Description.Should().Be("更新描述");
    }

    #endregion
}