using System.Text.Json;

namespace ReTime_Testing.Tests.Models;

/// <summary>
/// 手动覆盖配置：「仅当天临时启用」与「覆盖式启用」的生效判定
/// </summary>
public class ScheduleOverrideConfigTests
{
    [Fact]
    public void 未启用_不生效()
    {
        var config = new ScheduleOverrideConfig { Enabled = false, ScheduleId = "s1" };

        Assert.False(config.IsEffectiveToday);
    }

    [Fact]
    public void 覆盖式启用_长期生效()
    {
        // 空 temporaryDate = 覆盖式启用，不随日期过期
        var config = new ScheduleOverrideConfig { Enabled = true, ScheduleId = "s1", TemporaryDate = "" };

        Assert.True(config.IsEffectiveToday);
    }

    [Fact]
    public void 临时启用_当天生效()
    {
        var config = new ScheduleOverrideConfig
        {
            Enabled = true,
            ScheduleId = "s1",
            TemporaryDate = DateTime.Now.ToString("yyyy-MM-dd")
        };

        Assert.True(config.IsEffectiveToday);
    }

    [Fact]
    public void 临时启用_过期后视为未覆盖()
    {
        // 隔天后自动回落组轮换（即使 enabled 仍为 true）
        var config = new ScheduleOverrideConfig
        {
            Enabled = true,
            ScheduleId = "s1",
            TemporaryDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd")
        };

        Assert.False(config.IsEffectiveToday);
    }

    [Fact]
    public void 旧配置无temporaryDate字段_反序列化默认为空()
    {
        // 既有配置文件没有该字段，反序列化后应默认为覆盖式启用（空字符串）
        var json = """{"enabled":true,"scheduleId":"s1"}""";

        var config = JsonSerializer.Deserialize<ScheduleOverrideConfig>(json);

        Assert.NotNull(config);
        Assert.Equal("", config!.TemporaryDate);
        Assert.True(config.IsEffectiveToday);
    }

    [Fact]
    public void 序列化反序列化_保留temporaryDate()
    {
        var config = new ScheduleOverrideConfig
        {
            Enabled = true,
            ScheduleId = "s1",
            TemporaryDate = "2026-10-05"
        };

        var json = JsonSerializer.Serialize(config);
        var restored = JsonSerializer.Deserialize<ScheduleOverrideConfig>(json);

        Assert.NotNull(restored);
        Assert.Equal("2026-10-05", restored!.TemporaryDate);
    }
}
