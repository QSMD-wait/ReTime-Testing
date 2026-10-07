using System.Text.Json;
using ReTime_Testing.Models;

namespace ReTime_Testing.Tests.Models;

/// <summary>
/// 手动指定计划表配置：覆盖式 / 仅当天的生效判定、JSON 序列化形状与 ScheduleConfig 状态迁移语义
/// </summary>
public class ScheduleManualConfigTests
{
    [Fact]
    public void 未手动指定_不生效()
    {
        var config = new ScheduleConfig();

        Assert.Null(config.Manual);
        Assert.Null(config.EffectiveManual);
    }

    [Fact]
    public void 覆盖式指定_长期生效()
    {
        var config = new ScheduleConfig();
        config.EnableManualPermanent("s1");

        Assert.True(config.Manual!.IsEffectiveToday);
        Assert.NotNull(config.EffectiveManual);
    }

    [Fact]
    public void 仅当天指定_当天生效()
    {
        var config = new ScheduleConfig();
        config.EnableManualToday("s1", DateTime.Now.ToString("yyyy-MM-dd"));

        Assert.True(config.Manual!.IsEffectiveToday);
        Assert.Equal(ScheduleManualMode.Today, config.Manual.Mode);
    }

    [Fact]
    public void 仅当天指定_过期后视为未指定()
    {
        var config = new ScheduleConfig();
        config.EnableManualToday("s1", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"));

        Assert.False(config.Manual!.IsEffectiveToday);
        Assert.Null(config.EffectiveManual);
    }

    [Fact]
    public void 指定表为空ID_不生效()
    {
        // 防御：手动对象存在但未填表 ID 视为未指定
        var config = new ScheduleConfig { Manual = new ScheduleManualConfig { ScheduleId = "", Mode = ScheduleManualMode.Permanent } };

        Assert.False(config.Manual.IsEffectiveToday);
        Assert.Null(config.EffectiveManual);
    }

    [Fact]
    public void JSON形状_scheduleId_mode小写_date()
    {
        var config = new ScheduleConfig();
        config.EnableManualToday("s1", "2026-10-07");

        var json = JsonSerializer.Serialize(config);

        Assert.Contains("\"manual\"", json);
        Assert.Contains("\"mode\":\"today\"", json);
        Assert.Contains("\"date\":\"2026-10-07\"", json);
        // 不再有旧的 enabled / temporaryDate 三字段形状
        Assert.DoesNotContain("temporaryDate", json);
    }

    [Fact]
    public void JSON反序列化_mode字符串与默认值()
    {
        var today = JsonSerializer.Deserialize<ScheduleConfig>("""{"manual":{"scheduleId":"s1","mode":"today","date":"2026-10-07"}}""");
        Assert.Equal(ScheduleManualMode.Today, today!.Manual!.Mode);

        // 缺省 mode 按覆盖式（Permanent）
        var permanent = JsonSerializer.Deserialize<ScheduleConfig>("""{"manual":{"scheduleId":"s1"}}""");
        Assert.Equal(ScheduleManualMode.Permanent, permanent!.Manual!.Mode);

        // 无效值按覆盖式回退
        var invalid = JsonSerializer.Deserialize<ScheduleConfig>("""{"manual":{"scheduleId":"s1","mode":"bogus"}}""");
        Assert.Equal(ScheduleManualMode.Permanent, invalid!.Manual!.Mode);
    }

    [Fact]
    public void 仅当天指定_保留激活组()
    {
        var config = new ScheduleConfig { ActiveGroupId = "group_1" };

        config.EnableManualToday("s1", DateTime.Now.ToString("yyyy-MM-dd"));

        Assert.Equal("group_1", config.ActiveGroupId);
    }

    [Fact]
    public void 覆盖式指定_清除激活组()
    {
        var config = new ScheduleConfig { ActiveGroupId = "group_1" };

        config.EnableManualPermanent("s1");

        Assert.Null(config.ActiveGroupId);
    }

    [Fact]
    public void 激活表组_清除手动指定()
    {
        var config = new ScheduleConfig();
        config.EnableManualToday("s1", DateTime.Now.ToString("yyyy-MM-dd"));

        config.ActivateGroup("group_1");

        Assert.Equal("group_1", config.ActiveGroupId);
        Assert.Null(config.Manual);
    }

    [Fact]
    public void 清除手动指定_仅匹配表才清除并返回true()
    {
        var config = new ScheduleConfig();
        config.EnableManualPermanent("s1");

        // 不匹配：不清除
        Assert.False(config.ClearManualFor("other"));
        Assert.NotNull(config.Manual);

        // 匹配：清除
        Assert.True(config.ClearManualFor("s1"));
        Assert.Null(config.Manual);

        // 无指定再清：返回 false
        Assert.False(config.ClearManualFor("s1"));
    }
}
