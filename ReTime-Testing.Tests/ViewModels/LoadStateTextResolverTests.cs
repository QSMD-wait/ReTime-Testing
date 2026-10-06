using ReTime_Testing.ViewModels.TimeScheduleEditor;

namespace ReTime_Testing.Tests.ViewModels;

/// <summary>
/// 顶部加载按钮动态文字解析：生效方显示名称、非生效方回退动作文字、主次样式差分
/// </summary>
public class LoadStateTextResolverTests
{
    private static ScheduleConfig NewConfig() => new()
    {
        Enabled = true,
        Override = new ScheduleOverrideConfig()
    };

    [Fact]
    public void 组生效_组名为主_计划表按钮显示轮换得到的当前表_次级样式()
    {
        var config = NewConfig();
        config.ActiveGroupId = "group_1";

        var result = LoadStateTextResolver.Resolve(config, "高三(1)", "第1/2周", null, "语文");

        Assert.Equal("高三(1)", result.GroupLabel);
        Assert.Equal("轮换中 · 第1/2周", result.GroupDetail);
        Assert.Equal("语文", result.ScheduleLabel);
        Assert.Equal("由表组轮换生效", result.ScheduleDetail);
        Assert.True(result.IsScheduleSecondary);
    }

    [Fact]
    public void 非轮换组生效_组细节为按表组安排生效()
    {
        var config = NewConfig();
        config.ActiveGroupId = "group_1";

        var result = LoadStateTextResolver.Resolve(config, "默认组", "", null, "语文");

        Assert.Equal("默认组", result.GroupLabel);
        Assert.Equal("按表组安排生效", result.GroupDetail);
        Assert.True(result.IsScheduleSecondary);
    }

    [Fact]
    public void 组生效但当天无表_计划表按钮显示无计划提示_次级样式()
    {
        var config = NewConfig();
        config.ActiveGroupId = "group_1";

        var result = LoadStateTextResolver.Resolve(config, "高三(1)", "", null, null);

        Assert.Equal("高三(1)", result.GroupLabel);
        Assert.Equal(LoadStateTextResolver.NoScheduleText, result.ScheduleLabel);
        Assert.Equal("表组今天未安排计划表", result.ScheduleDetail);
        Assert.True(result.IsScheduleSecondary);
    }

    [Fact]
    public void 覆盖生效_表名为主_组按钮回退动作文字_非次级()
    {
        var config = NewConfig();
        config.ActiveGroupId = "group_1";
        config.Override.Enabled = true;
        config.Override.ScheduleId = "s1";

        var result = LoadStateTextResolver.Resolve(config, "高三(1)", "第1/2周", "数学复习", "数学复习");

        Assert.Equal(LoadStateTextResolver.GroupActionText, result.GroupLabel);
        Assert.Equal("", result.GroupDetail);
        Assert.Equal("数学复习", result.ScheduleLabel);
        Assert.Equal("覆盖式启用中，点击可切换计划表", result.ScheduleDetail);
        Assert.False(result.IsScheduleSecondary);
    }

    [Fact]
    public void 临时启用当天_表名加临时说明_非次级()
    {
        var config = NewConfig();
        config.Override.Enabled = true;
        config.Override.ScheduleId = "s1";
        config.Override.TemporaryDate = DateTime.Now.ToString("yyyy-MM-dd");

        var result = LoadStateTextResolver.Resolve(config, null, "", "数学复习", "数学复习");

        Assert.Equal("数学复习", result.ScheduleLabel);
        Assert.Equal("仅当天临时启用，次日自动恢复按表组轮换", result.ScheduleDetail);
        Assert.False(result.IsScheduleSecondary);
    }

    [Fact]
    public void 临时启用过期_回落表组_计划表按钮显示轮换表_次级样式()
    {
        var config = NewConfig();
        config.ActiveGroupId = "group_1";
        config.Override.Enabled = true;
        config.Override.ScheduleId = "s1";
        config.Override.TemporaryDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

        var result = LoadStateTextResolver.Resolve(config, "高三(1)", "", "数学复习", "语文");

        Assert.Equal("高三(1)", result.GroupLabel);
        Assert.Equal("语文", result.ScheduleLabel);
        Assert.True(result.IsScheduleSecondary);
    }

    [Fact]
    public void 未启用_双按钮均为动作文字_非次级()
    {
        var config = NewConfig();

        var result = LoadStateTextResolver.Resolve(config, null, "", null, null);

        Assert.Equal(LoadStateTextResolver.GroupActionText, result.GroupLabel);
        Assert.Equal("", result.GroupDetail);
        Assert.Equal(LoadStateTextResolver.ScheduleActionText, result.ScheduleLabel);
        Assert.Equal("", result.ScheduleDetail);
        Assert.False(result.IsScheduleSecondary);
    }

    [Fact]
    public void 覆盖指向已删除的表_计划表按钮回退动作文字_非次级()
    {
        var config = NewConfig();
        config.Override.Enabled = true;
        config.Override.ScheduleId = "deleted";

        var result = LoadStateTextResolver.Resolve(config, null, "", null, null);

        Assert.Equal(LoadStateTextResolver.ScheduleActionText, result.ScheduleLabel);
        Assert.Equal("", result.ScheduleDetail);
        Assert.False(result.IsScheduleSecondary);
    }

    [Fact]
    public void 临时启用过期且无表组_次日双按钮均为动作文字_两个均无()
    {
        // 当天启用表时没有表组：次日临时覆盖过期后回到"无表组与计划表"初始状态
        var config = NewConfig();
        config.Override.Enabled = true;
        config.Override.ScheduleId = "s1";
        config.Override.TemporaryDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

        var result = LoadStateTextResolver.Resolve(config, null, "", "数学复习", null);

        Assert.Equal(LoadStateTextResolver.GroupActionText, result.GroupLabel);
        Assert.Equal("", result.GroupDetail);
        Assert.Equal(LoadStateTextResolver.ScheduleActionText, result.ScheduleLabel);
        Assert.Equal("", result.ScheduleDetail);
        Assert.False(result.IsScheduleSecondary);
    }
}
