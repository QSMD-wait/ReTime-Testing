using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReTime_Testing.Views.TimeScheduleEditor;

/// <summary>
/// 计划表下拉选项（Id 为空字符串表示空选项：基础映射为"（未配置）"，轮转覆盖为"（继承基础映射）"）
/// </summary>
public class ScheduleOption
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";

    public override string ToString() => Name;
}

/// <summary>
/// 天→表映射的单行（星期几 → 计划表）
/// </summary>
public partial class DayMapRowItem : ObservableObject
{
    /// <summary>
    /// 星期索引（0=周日, 1=周一, ..., 6=周六）
    /// </summary>
    public int DayIndex { get; init; }

    /// <summary>
    /// 星期显示名（如"周日"）
    /// </summary>
    public string DayName { get; init; } = "";

    /// <summary>
    /// ComboBox 的空选项（ScheduleOptions 的第一项）
    /// </summary>
    public ScheduleOption? EmptyOption { get; init; }

    /// <summary>
    /// ComboBox 全部选项
    /// </summary>
    public List<ScheduleOption> ScheduleOptions { get; init; } = new();

    /// <summary>
    /// 选中的计划表ID（空字符串 = 未配置 / 继承基础映射）
    /// </summary>
    [ObservableProperty]
    private string scheduleId = "";
}

/// <summary>
/// 轮转周覆盖项（只保存与基础映射的差异）
/// </summary>
public partial class RotationOverrideItem : ObservableObject
{
    /// <summary>
    /// 显示用的轮转周序号（2 ~ N，第 1 周即基础周，无覆盖）
    /// </summary>
    public int DisplayWeek { get; init; }

    /// <summary>
    /// 写入 RotatedDayScheduleMaps 的键（解析逻辑中 0=基础周，故键 = 周序 - 1）
    /// </summary>
    public string MapKey => (DisplayWeek - 1).ToString();

    /// <summary>
    /// Expander 标题
    /// </summary>
    public string Title => $"第 {DisplayWeek} 周";

    /// <summary>
    /// 该周的差异行（7 天）
    /// </summary>
    public ObservableCollection<DayMapRowItem> Rows { get; } = new();

    /// <summary>
    /// Expander 是否展开
    /// </summary>
    [ObservableProperty]
    private bool isExpanded = true;

    /// <summary>
    /// 是否仍在当前轮换周期内（周期数调小后隐藏但保留数据）
    /// </summary>
    [ObservableProperty]
    private bool isWithinCycle = true;

    /// <summary>
    /// 差异摘要（如"2 天与基础映射不同"）
    /// </summary>
    [ObservableProperty]
    private string summary = "";

    /// <summary>
    /// 与基础映射不同的天数
    /// </summary>
    public int DiffCount => Rows.Count(r => !string.IsNullOrEmpty(r.ScheduleId));

    /// <summary>
    /// 重新计算摘要（行数据变化后调用）
    /// </summary>
    public void UpdateSummary()
    {
        var diffCount = DiffCount;
        Summary = diffCount == 0 ? "未覆盖任何天" : $"{diffCount} 天与基础映射不同";
        OnPropertyChanged(nameof(DiffCount));
    }
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
