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
using IListView = iNKORE.UI.WPF.Modern.Controls.ListView;

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

                // 两个轮转周的表格组纵向堆叠（周序号用中文数字）
                Assert.Contains("第一周（基础）", text);
                Assert.Contains("第二周", text);
                Assert.DoesNotContain("第三周", text);
                Assert.Contains("周一", text);
                Assert.Contains("周日", text);
                Assert.Contains("相同", text);   // 轮转周标签
                Assert.Contains("不同", text);   // 轮转周标签 + 摘要
                Assert.Contains("选择计划表", text); // 空位按钮文案
                Assert.DoesNotContain("2026", text); // 已去掉表头日期
                Assert.DoesNotContain("沿用", text);  // 旧文案已废弃

                // 天列状态：轮转标签仅在轮转周且有生效计划表时显示
                var cells = CollectCells(host).Distinct().ToList();
                Assert.Equal(14, cells.Count); // 2 周 × 7 天

                // 第 1 周不显示轮转标签；第 2 周仅"有生效计划表"的天显示（空位不显示）
                Assert.All(cells.Where(c => c.DisplayWeek == 1), c => Assert.False(c.ShowRotationTags));
                var week2Cells = cells.Where(c => c.DisplayWeek == 2).ToList();
                Assert.All(week2Cells.Where(c => c.ShowRotationTags), c => Assert.False(c.IsEmpty));
                Assert.Equal(2, week2Cells.Count(c => c.ShowRotationTags)); // 周一继承 schedA + 周二覆盖 schedB
                Assert.Contains(week2Cells, c => c.TagText == "不同" && c.ShowRotationTags); // 周二覆盖 schedB
                Assert.Contains(week2Cells, c => c.TagText == "相同" && c.ShowRotationTags); // 周一继承
                // 轮转周沿用第一周 → 按钮文字灰色斜体（IsInherited）
                Assert.True(cells.Single(c => c.DisplayWeek == 2 && c.DayIndex == 1).IsInherited);  // 周一沿用
                Assert.False(cells.Single(c => c.DisplayWeek == 2 && c.DayIndex == 2).IsInherited); // 周二单独设置
                Assert.All(cells.Where(c => c.DisplayWeek == 1), c => Assert.False(c.IsInherited)); // 基础周无沿用
                // 轮转周空选项文案（带括号）
                Assert.All(week2Cells, c => Assert.Equal("（按第一周安排）", c.ScheduleOptions[0].Name));

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

                var listView = Assert.IsType<IListView>(((Flyout)flyout!).Content);
                var cell = (DayCellItem)button!.DataContext;
                Assert.Equal(cell.ScheduleOptions.Count, listView.Items.Count);
                Assert.Single(listView.Items); // 无可用计划表 → 仅空选项
                // 空选项带括号（斜体样式在 XAML 容器样式中）
                Assert.Equal("（未设置）", cell.ScheduleOptions[0].Name);
                Assert.True(cell.ScheduleOptions[0].IsPlaceholder);

                // 选中空选项 → 写回天列并清空
                listView.SelectedValue = "";
                Assert.Equal("", cell.ScheduleId);
                Assert.True(cell.IsEmpty);
                Assert.Equal("选择计划表", cell.DisplayName);
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

    private static IEnumerable<DayCellItem> CollectCells(DependencyObject node)
    {
        if (node is FrameworkElement { DataContext: DayCellItem cell }) yield return cell;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            foreach (var item in CollectCells(VisualTreeHelper.GetChild(node, i)))
                yield return item;
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
