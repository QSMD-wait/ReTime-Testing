using ReTime_Testing.Models;
using FluentAssertions;

namespace ReTime_Testing.Tests.Models;

/// <summary>
/// ScheduleBehaviorData 和 ScheduleBehavior 的单元测试
/// </summary>
public class ScheduleBehaviorDataTests
{
    #region MergeWith 测试

    [Fact]
    public void MergeWith_BothNull_ReturnsAllNull()
    {
        var higher = new ScheduleBehaviorData();
        var lower = new ScheduleBehaviorData();

        var result = higher.MergeWith(lower);

        result.PollingIntervalMs.Should().BeNull();
        result.ReverseProgress.Should().BeNull();
    }

    [Fact]
    public void MergeWith_HigherHasValue_LowerNull_HigherWins()
    {
        var higher = new ScheduleBehaviorData { PollingIntervalMs = 500, ReverseProgress = true };
        var lower = new ScheduleBehaviorData();

        var result = higher.MergeWith(lower);

        result.PollingIntervalMs.Should().Be(500);
        result.ReverseProgress.Should().BeTrue();
    }

    [Fact]
    public void MergeWith_HigherNull_LowerHasValue_LowerUsed()
    {
        var higher = new ScheduleBehaviorData();
        var lower = new ScheduleBehaviorData { PollingIntervalMs = 500, ReverseProgress = true };

        var result = higher.MergeWith(lower);

        result.PollingIntervalMs.Should().Be(500);
        result.ReverseProgress.Should().BeTrue();
    }

    [Fact]
    public void MergeWith_BothHaveValue_HigherWins()
    {
        var higher = new ScheduleBehaviorData { PollingIntervalMs = 200, ReverseProgress = true };
        var lower = new ScheduleBehaviorData { PollingIntervalMs = 500, ReverseProgress = false };

        var result = higher.MergeWith(lower);

        result.PollingIntervalMs.Should().Be(200);
        result.ReverseProgress.Should().BeTrue();
    }

    [Fact]
    public void MergeWith_PartialOverride_OnlyNonNullFieldsOverride()
    {
        var higher = new ScheduleBehaviorData { PollingIntervalMs = 200 };
        var lower = new ScheduleBehaviorData { PollingIntervalMs = 500, ReverseProgress = true };

        var result = higher.MergeWith(lower);

        result.PollingIntervalMs.Should().Be(200);    // higher 覆盖
        result.ReverseProgress.Should().BeTrue();      // lower 回退
    }

    [Fact]
    public void MergeWith_LowerIsNull_ReturnsHigherAsIs()
    {
        var higher = new ScheduleBehaviorData { PollingIntervalMs = 500 };

        var result = higher.MergeWith(null);

        result.PollingIntervalMs.Should().Be(500);
        result.ReverseProgress.Should().BeNull();
    }

    [Fact]
    public void MergeWith_ThreeLevelChain_HighestPriorityWins()
    {
        // 模拟三级优先级：硬编码默认 → 配置文件 → 时间计划表
        var hardcoded = new ScheduleBehaviorData { PollingIntervalMs = 1000, ReverseProgress = false };
        var configFile = new ScheduleBehaviorData { PollingIntervalMs = 500 };
        var schedule = new ScheduleBehaviorData { ReverseProgress = true };

        // 链式合并
        var result = schedule.MergeWith(configFile.MergeWith(hardcoded));

        result.PollingIntervalMs.Should().Be(500);     // configFile 覆盖硬编码
        result.ReverseProgress.Should().BeTrue();       // schedule 覆盖硬编码
    }

    [Fact]
    public void MergeWith_DoesNotModifyOriginal()
    {
        var higher = new ScheduleBehaviorData { PollingIntervalMs = 200 };
        var lower = new ScheduleBehaviorData { PollingIntervalMs = 500 };

        higher.MergeWith(lower);

        higher.PollingIntervalMs.Should().Be(200);
        lower.PollingIntervalMs.Should().Be(500);
    }

    #endregion

    #region ToResolved 测试

    [Fact]
    public void ToResolved_AllNull_FallsBackToDefaults()
    {
        var data = new ScheduleBehaviorData();

        var result = data.ToResolved();

        result.PollingIntervalMs.Should().Be(ScheduleBehavior.DefaultPollingIntervalMs);
        result.ReverseProgress.Should().Be(ScheduleBehavior.DefaultReverseProgress);
    }

    [Fact]
    public void ToResolved_AllSet_UsesProvidedValues()
    {
        var data = new ScheduleBehaviorData { PollingIntervalMs = 250, ReverseProgress = true };

        var result = data.ToResolved();

        result.PollingIntervalMs.Should().Be(250);
        result.ReverseProgress.Should().BeTrue();
    }

    [Fact]
    public void ToResolved_PartialSet_MixesWithDefaults()
    {
        var data = new ScheduleBehaviorData { PollingIntervalMs = 250 };

        var result = data.ToResolved();

        result.PollingIntervalMs.Should().Be(250);
        result.ReverseProgress.Should().BeFalse();
    }

    #endregion

    #region HasAnyOverride 测试

    [Fact]
    public void HasAnyOverride_AllNull_ReturnsFalse()
    {
        var data = new ScheduleBehaviorData();

        data.HasAnyOverride.Should().BeFalse();
    }

    [Fact]
    public void HasAnyOverride_PollingIntervalSet_ReturnsTrue()
    {
        var data = new ScheduleBehaviorData { PollingIntervalMs = 500 };

        data.HasAnyOverride.Should().BeTrue();
    }

    [Fact]
    public void HasAnyOverride_ReverseProgressSet_ReturnsTrue()
    {
        var data = new ScheduleBehaviorData { ReverseProgress = false };

        data.HasAnyOverride.Should().BeTrue();
    }

    #endregion

    #region ScheduleBehavior 默认值测试

    [Fact]
    public void Default_HasCorrectDefaults()
    {
        var behavior = ScheduleBehavior.Default;

        behavior.PollingIntervalMs.Should().Be(1000);
        behavior.ReverseProgress.Should().BeFalse();
    }

    [Fact]
    public void Default_IsNewInstance()
    {
        var a = ScheduleBehavior.Default;
        var b = ScheduleBehavior.Default;

        a.Should().NotBeSameAs(b);
    }

    #endregion

    #region 端到端：三级合并 + 解析

    [Fact]
    public void EndToEnd_ThreeLevelMergeAndResolve()
    {
        // 硬编码默认
        var hardcoded = new ScheduleBehaviorData();
        // 配置文件：设置轮询间隔为 500ms
        var configFile = new ScheduleBehaviorData { PollingIntervalMs = 500 };
        // 时间计划表：设置倒计时
        var schedule = new ScheduleBehaviorData { ReverseProgress = true };

        var resolved = schedule.MergeWith(configFile.MergeWith(hardcoded)).ToResolved();

        resolved.PollingIntervalMs.Should().Be(500);
        resolved.ReverseProgress.Should().BeTrue();
    }

    [Fact]
    public void EndToEnd_OnlyScheduleProvided()
    {
        var schedule = new ScheduleBehaviorData { PollingIntervalMs = 250 };
        var resolved = schedule.MergeWith(null).ToResolved();

        resolved.PollingIntervalMs.Should().Be(250);
        resolved.ReverseProgress.Should().BeFalse();
    }

    [Fact]
    public void EndToEnd_NothingProvided_AllDefaults()
    {
        var resolved = new ScheduleBehaviorData().MergeWith(null).ToResolved();

        resolved.PollingIntervalMs.Should().Be(ScheduleBehavior.DefaultPollingIntervalMs);
        resolved.ReverseProgress.Should().Be(ScheduleBehavior.DefaultReverseProgress);
    }

    #endregion
}
