using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReTime_Testing.Views.TimeScheduleEditor;

/// <summary>
/// 轮转周表格组：一个轮转周对应一张「指派行表」（表头行 + 指派行），
/// 周期内的各周按周序纵向堆叠在编排页上
/// </summary>
public partial class WeekTableItem : ObservableObject
{
    /// <summary>
    /// 显示用周序号（1 = 基础映射，2..N = 轮转周）
    /// </summary>
    public int DisplayWeek { get; init; }

    /// <summary>
    /// 是否为基础周（第 1 周）
    /// </summary>
    public bool IsBase => DisplayWeek <= 1;

    /// <summary>
    /// 表格标题（"第 1 周（基础映射）" / "第 N 周"）
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    /// 今天是否落在该轮转周（标题行显示"本周"徽标）
    /// </summary>
    public bool IsCurrentWeek { get; init; }

    /// <summary>
    /// 标题行右侧摘要（"N 天与基础映射不同" / "全部继承基础映射" / "每周默认映射"）
    /// </summary>
    [ObservableProperty]
    private string summary = "";

    /// <summary>
    /// 该周的 7 个天列（周一 → 周日）
    /// </summary>
    public List<DayCellItem> Cells { get; init; } = new();
}

/// <summary>
/// 表格中的天列（星期几 → 计划表）：表头（星期 + 日期）+ 指派行（计划表选择按钮）
/// </summary>
public partial class DayCellItem : ObservableObject
{
    /// <summary>
    /// 星期索引（0=周日, 1=周一, ..., 6=周六）
    /// </summary>
    public int DayIndex { get; init; }

    /// <summary>
    /// 星期显示名（如"周一"）
    /// </summary>
    public string DayName { get; init; } = "";

    /// <summary>
    /// 所属轮转周序号（决定写入基础映射还是轮转差异映射）
    /// </summary>
    public int DisplayWeek { get; init; }

    /// <summary>
    /// 表头日期（该轮转周在当前轮换周期内的实际日期，格式 MM/dd）
    /// </summary>
    public string DateText { get; init; } = "";

    /// <summary>
    /// 是否为今天（天列强调高亮）
    /// </summary>
    public bool IsToday { get; init; }

    /// <summary>
    /// 是否为第一列（不显示左侧分隔线）
    /// </summary>
    public bool IsFirst { get; init; }

    /// <summary>
    /// 计划表选择器的选项（首项为空选项：基础周"（未配置）" / 轮转周"（继承基础映射）"）
    /// </summary>
    public List<ScheduleOption> ScheduleOptions { get; init; } = new();

    /// <summary>
    /// 当前编辑周的原始值（"" = 未配置 / 继承基础映射）
    /// </summary>
    [ObservableProperty]
    private string scheduleId = "";

    /// <summary>
    /// 生效的计划表名（轮转周未覆盖的天显示基础映射的名称）
    /// </summary>
    [ObservableProperty]
    private string displayName = "";

    /// <summary>
    /// 生效计划表是否为空
    /// </summary>
    [ObservableProperty]
    private bool isEmpty = true;

    /// <summary>
    /// 按钮文本（空时为"添加计划表"，否则为计划表名）
    /// </summary>
    [ObservableProperty]
    private string buttonText = "";

    /// <summary>
    /// 是否显示 继承/差异 标签（仅轮转周）
    /// </summary>
    [ObservableProperty]
    private bool showRotationTags;

    /// <summary>
    /// 该天是否继承自基础映射（轮转周）
    /// </summary>
    [ObservableProperty]
    private bool isInherited;

    /// <summary>
    /// 标签文本（"继承" / "差异"）
    /// </summary>
    [ObservableProperty]
    private string tagText = "";
}
