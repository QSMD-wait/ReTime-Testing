using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using iNKORE.UI.WPF.Modern.Controls;
using ReTime_Testing.Models;
using ReTime_Testing.Views.TimeScheduleEditor;
using Xunit;

namespace ReTime_Testing.Tests.Views;

/// <summary>
/// GroupWeekArrangement 轻量离屏冒烟：表格组堆叠渲染 + Flyout 选择器装配与选中写回
/// </summary>
public class GroupWeekArrangementSmokeTests
{
    [Fact]
    public void 编排页_表格组堆叠渲染并可经Flyout选择计划表()
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var arrange = new GroupWeekArrangement { DataContext = null };
                arrange.LoadGroup(new ScheduleGroup
                {
                    Id = "smoke",
                    RotationCycleCount = 2,
                    RotationStartDate = "2026-09-27",
                    DayScheduleMap = new Dictionary<string, string> { ["1"] = "schedA" },
                    RotatedDayScheduleMaps = new Dictionary<string, Dictionary<string, string>>
                    {
                        ["1"] = new Dictionary<string, string> { ["2"] = "schedB" }
                    }
                });

                var host = new Border { Child = arrange, Width = 760, Height = 560 };
                host.Measure(new Size(760, 560));
                host.Arrange(new Rect(0, 0, 760, 560));
                host.UpdateLayout();

                var texts = CollectTexts(host).ToList();
                var text = string.Join("\n", texts);

                // 两个轮转周的表格组纵向堆叠
                Assert.Contains("第 1 周（基础映射）", text);
                Assert.Contains("第 2 周", text);
                Assert.DoesNotContain("第 3 周", text);
                Assert.Contains("周一", text);
                Assert.Contains("周日", text);
                Assert.Contains("继承", text);
                Assert.Contains("差异", text);
                Assert.Contains("添加计划表", text);
                Assert.Contains(texts, t => t.Contains('/')); // 表头日期 MM/dd

                // 指派行按钮已挂接 Flyout（FlyoutService）
                var button = FindButtons(host).FirstOrDefault(b =>
                    b.DataContext is DayCellItem { DisplayWeek: 1, DayIndex: 1 });
                Assert.NotNull(button);

                var flyout = FlyoutService.GetFlyout(button!);
                Assert.NotNull(flyout);
                Assert.Equal(FlyoutOpeningMode.Click, FlyoutService.GetFlyoutOpeningMode(button!));
                Assert.IsType<Flyout>(flyout);

                // 模拟点击装配选择器（不真正弹出，避免测试中创建弹窗）
                var onClick = typeof(GroupWeekArrangement).GetMethod(
                    "OnCellClick", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.NotNull(onClick);
                onClick!.Invoke(arrange, new object?[] { button!, new RoutedEventArgs() });

                var listBox = Assert.IsType<ListBox>(((Flyout)flyout!).Content);
                var cell = (DayCellItem)button!.DataContext;
                Assert.Equal(cell.ScheduleOptions.Count, listBox.Items.Count);
                Assert.Single(listBox.Items); // 无可用计划表 → 仅空选项

                // 选中空选项 → 写回天列并清空
                listBox.SelectedValue = "";
                Assert.Equal("", cell.ScheduleId);
                Assert.True(cell.IsEmpty);
                Assert.Equal("添加计划表", cell.ButtonText);
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

    private static IEnumerable<string> CollectTexts(DependencyObject node)
    {
        if (node is TextBlock tb && !string.IsNullOrWhiteSpace(tb.Text))
            yield return tb.Text;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            foreach (var t in CollectTexts(VisualTreeHelper.GetChild(node, i)))
                yield return t;
        }
    }

    private static IEnumerable<Button> FindButtons(DependencyObject node)
    {
        if (node is Button b) yield return b;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            foreach (var item in FindButtons(VisualTreeHelper.GetChild(node, i)))
                yield return item;
        }
    }
}
