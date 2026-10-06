using System;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using iNKORE.UI.WPF.Modern;
using iNKORE.UI.WPF.Modern.Controls;
using Xunit;

namespace ReTime_Testing.Tests.Views;

/// <summary>
/// 「加载计划表」按钮次级文字差分样式冒烟：
/// BasedOn 隐式主题样式可被 StaticResource 解析（否则窗口加载即抛 XamlParseException），
/// 且 DataTrigger 能经模板级联到 Label 文字（前景色 + 字重）
/// </summary>
public class LoadButtonSecondaryStyleSmokeTests
{
    public sealed class StubViewModel
    {
        public bool IsScheduleLabelSecondary { get; set; } = true;
        public string ScheduleButtonLabel { get; set; } = "语文";
    }

    [Fact]
    public void 次级差分样式_隐式样式可解析且触发器级联到Label()
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                // 与 TimeScheduleEditor.xaml 中"加载计划表"按钮的 Style 片段一致
                const string xaml = """
<ui:AppBarButton
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:ui="http://schemas.inkore.net/lib/ui/wpf/modern"
    Label="{Binding ScheduleButtonLabel}">
    <ui:AppBarButton.Style>
        <Style TargetType="{x:Type ui:AppBarButton}" BasedOn="{StaticResource {x:Type ui:AppBarButton}}">
            <Style.Triggers>
                <DataTrigger Binding="{Binding IsScheduleLabelSecondary}" Value="True">
                    <Setter Property="Foreground" Value="{DynamicResource {x:Static ui:ThemeKeys.TextFillColorSecondaryBrushKey}}"/>
                    <Setter Property="FontWeight" Value="Light"/>
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </ui:AppBarButton.Style>
</ui:AppBarButton>
""";

                // StaticResource 若找不到隐式主题样式，解析在这里就会抛异常
                var button = (AppBarButton)XamlReader.Parse(xaml);

                // 提供主题画刷字典，使次级色 DynamicResource 可解析（模拟 App.xaml 的 ThemeResources）
                button.Resources.MergedDictionaries.Add(new ThemeResources());
                button.DataContext = new StubViewModel();

                // Style 触发器的绑定经 Dispatcher 传递，测试线程无消息泵需手动刷新
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

                button.ApplyTemplate();
                var label = (FrameworkElement)button.Template.FindName("TextLabel", button);
                Assert.NotNull(label);

                // DataTrigger 生效：按钮级字重/前景色改变
                Assert.Equal(FontWeights.Light, button.FontWeight);
                Assert.NotNull(button.Foreground);

                // 级联到模板内 Label 文字（FontWeight 继承、Foreground 经 TemplateBinding；
                // Label 是 ContentPresenterEx，字体与前景色属性经反射读取）
                var labelFontWeight = label.GetType().GetProperty("FontWeight")?.GetValue(label);
                Assert.NotNull(labelFontWeight);
                Assert.Equal(FontWeights.Light, labelFontWeight);

                var labelForeground = label.GetType().GetProperty("Foreground")?.GetValue(label);
                Assert.NotNull(labelForeground);
                Assert.Same(button.Foreground, labelForeground);
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
}
