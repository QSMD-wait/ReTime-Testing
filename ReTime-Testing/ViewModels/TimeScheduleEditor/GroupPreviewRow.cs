namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 表组预览面板的天→表映射行（只读）
/// </summary>
public class GroupPreviewRow
{
    /// <summary>
    /// 星期显示名（如"周日"）
    /// </summary>
    public string DayName { get; init; } = "";

    /// <summary>
    /// 生效的计划表名称（未配置时为"（未配置）"）
    /// </summary>
    public string ScheduleName { get; init; } = "（未配置）";

    /// <summary>
    /// 是否已配置计划表
    /// </summary>
    public bool IsConfigured { get; init; }

    /// <summary>
    /// 本周是否因轮转覆盖而与基础映射不同
    /// </summary>
    public bool IsRotationApplied { get; init; }
}
