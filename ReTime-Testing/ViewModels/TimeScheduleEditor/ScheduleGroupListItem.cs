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
    /// 成员计划表数量
    /// </summary>
    [ObservableProperty]
    private int _memberCount;

    /// <summary>
    /// 是否为当前激活组
    /// </summary>
    [ObservableProperty]
    private bool _isActivated;

    /// <summary>
    /// 轮换信息描述（如 "第1/2周"）
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
