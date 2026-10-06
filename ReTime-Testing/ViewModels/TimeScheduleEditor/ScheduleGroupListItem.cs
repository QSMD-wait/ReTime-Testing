using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 计划表组列表项（绑定到 ScheduleGroup）
/// </summary>
public partial class ScheduleGroupListItem : ObservableObject
{
    /// <summary>
    /// 组唯一标识
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// 组名称
    /// </summary>
    [ObservableProperty]
    private string _name = "";

    /// <summary>
    /// 组描述
    /// </summary>
    [ObservableProperty]
    private string? _description;

    /// <summary>
    /// 轮换周期数（1=不轮换, 2=双周, 3=三周, 4=四周）
    /// </summary>
    [ObservableProperty]
    private int _rotationCycleCount = 1;

    /// <summary>
    /// 组内计划表数量（基础映射 + 轮转映射 + 日期覆盖去重后的唯一表数）
    /// </summary>
    [ObservableProperty]
    private int _memberCount;

    /// <summary>
    /// 是否为当前生效组（激活且未被"单独启用的表"手动覆盖）
    /// </summary>
    [ObservableProperty]
    private bool _isActivated;

    /// <summary>
    /// 轮换信息描述（如 "第1/2周"，非轮换组为空）
    /// </summary>
    [ObservableProperty]
    private string? _rotationInfo;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
