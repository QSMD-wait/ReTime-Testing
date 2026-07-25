using ReTime_Testing.Models;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// ComboBox 选项项，用于显示枚举值的中文名称
/// </summary>
public class StateOptionItem
{
    /// <summary>
    /// 枚举值
    /// </summary>
    public ProgressStateType Value { get; init; }

    /// <summary>
    /// 中文显示名称
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;
}