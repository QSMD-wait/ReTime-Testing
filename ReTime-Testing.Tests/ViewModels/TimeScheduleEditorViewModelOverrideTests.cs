using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels.TimeScheduleEditor;
using Xunit;

namespace ReTime_Testing.Tests.ViewModels;

/// <summary>
/// 覆盖式/仅当天启用对表组设置的写入行为（永久覆盖清组、临时覆盖保组、设为活跃同语义清组）
/// 与对应的加载按钮显示确认
/// </summary>
public class TimeScheduleEditorViewModelOverrideTests
{
    private readonly TimeTopSetting _setting = new();
    private readonly Mock<ISettingsService> _settings = new();
    private readonly Mock<ITimeScheduleManager> _schedules = new();
    private readonly Mock<IScheduleGroupManager> _groups = new();

    public TimeScheduleEditorViewModelOverrideTests()
    {
        _setting.Schedule.ActiveGroupId = "group_1";

        _settings.Setup(s => s.GetTimeTopSetting()).Returns(_setting);
        _schedules.Setup(s => s.GetScheduleList()).Returns(new List<ScheduleInfo>());
        _groups.Setup(g => g.LoadAllGroups()).Returns(new List<ScheduleGroup>());

        // 手动指定的表：返回带名称的实例，供按钮显示断言
        var namedSchedule = new TimeSchedule();
        namedSchedule.Settings.Metadata.Name = "数学复习";
        _schedules.Setup(s => s.LoadSchedule(It.IsAny<string>())).Returns(namedSchedule);

        // 激活组：返回带名称的实例，供组按钮显示断言（仅当天保留组的场景）
        var namedGroup = new ScheduleGroup();
        namedGroup.Metadata.Name = "高三(1)";
        _groups.Setup(g => g.LoadGroup("group_1")).Returns(namedGroup);
    }

    private TimeScheduleEditorViewModel CreateViewModel() =>
        new(
            NullLogger<TimeScheduleEditorViewModel>.Instance,
            NullLogger<ExecutionPlanGenerator>.Instance,
            _schedules.Object,
            _groups.Object,
            _settings.Object);

    // VM 构造含 DispatcherTimer，沿用 STA 线程模式
    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure != null) Assert.Fail(failure.ToString());
    }

    [Fact]
    public void 覆盖式启用_清除表组设置_按钮显示加载表组与表名()
    {
        RunSta(() =>
        {
            var vm = CreateViewModel();

            vm.ApplyScheduleSelection(new ScheduleListItem { Id = "s1", Name = "数学复习" }, onlyToday: false);

            // 永久覆盖视为放弃组轮换：表组设置被清除
            Assert.Null(_setting.Schedule.ActiveGroupId);
            Assert.NotNull(_setting.Schedule.Manual);
            Assert.Equal("s1", _setting.Schedule.Manual!.ScheduleId);
            Assert.Equal(ScheduleManualMode.Permanent, _setting.Schedule.Manual.Mode);
            _settings.Verify(s => s.SaveTimeTopSetting(It.IsAny<TimeTopSetting>()), Times.Once);

            // 显示：组按钮回退"加载表组"，表按钮为主位表名
            Assert.Equal("加载表组", vm.GroupButtonLabel);
            Assert.False(vm.IsGroupLabelSecondary);
            Assert.Equal("数学复习", vm.ScheduleButtonLabel);
            Assert.False(vm.IsScheduleLabelSecondary);
        });
    }

    [Fact]
    public void 仅当天启用_保留表组设置供次日轮换_组按钮显示组名次级样式()
    {
        RunSta(() =>
        {
            var vm = CreateViewModel();

            vm.ApplyScheduleSelection(new ScheduleListItem { Id = "s1", Name = "数学复习" }, onlyToday: true);

            // 临时启用不清表组设置，次日过期后恢复轮换（回落显示见 LoadStateTextResolverTests）
            Assert.Equal("group_1", _setting.Schedule.ActiveGroupId);
            Assert.NotNull(_setting.Schedule.Manual);
            Assert.Equal(ScheduleManualMode.Today, _setting.Schedule.Manual!.Mode);
            Assert.Equal(DateTime.Now.ToString("yyyy-MM-dd"), _setting.Schedule.Manual.Date);

            // 生效期间：表按钮为主位表名；组配置仍保留，组按钮以次级样式显示组名
            Assert.Equal("数学复习", vm.ScheduleButtonLabel);
            Assert.False(vm.IsScheduleLabelSecondary);
            Assert.Equal("高三(1)", vm.GroupButtonLabel);
            Assert.True(vm.IsGroupLabelSecondary);
            Assert.Equal("次日恢复按表组轮换", vm.GroupButtonDetail);
        });
    }

    [Fact]
    public void 右键设为活跃_同为覆盖式启用_清除表组设置()
    {
        RunSta(() =>
        {
            var vm = CreateViewModel();

            vm.ActivateScheduleCommand.Execute("s1");

            Assert.Null(_setting.Schedule.ActiveGroupId);
            Assert.NotNull(_setting.Schedule.Manual);
            Assert.Equal("s1", _setting.Schedule.Manual!.ScheduleId);
            Assert.Equal(ScheduleManualMode.Permanent, _setting.Schedule.Manual.Mode);
        });
    }
}
