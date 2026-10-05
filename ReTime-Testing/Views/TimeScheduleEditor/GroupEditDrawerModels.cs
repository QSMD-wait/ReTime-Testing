using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReTime_Testing.Views.TimeScheduleEditor;

/// <summary>
/// 计划表下拉选项（Id 为空字符串表示空选项：基础周为"（未设置）"，轮转周为"（按第一周安排）"）
/// </summary>
public class ScheduleOption
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";

    /// <summary>
    /// 是否为空选项（选择器中以斜体显示）
    /// </summary>
    public bool IsPlaceholder => string.IsNullOrEmpty(Id);

    public override string ToString() => Name;
}

/// <summary>
/// 日期覆盖项（指定日期强制使用某张计划表）
/// </summary>
public partial class DateOverrideItem : ObservableObject
{
    /// <summary>
    /// 覆盖日期
    /// </summary>
    [ObservableProperty]
    private DateTime? date;

    /// <summary>
    /// 该日期使用的计划表ID
    /// </summary>
    [ObservableProperty]
    private string scheduleId = "";

    /// <summary>
    /// ComboBox 选项（不含空选项，删除通过行内删除按钮）
    /// </summary>
    public List<ScheduleOption> ScheduleOptions { get; init; } = new();
}
