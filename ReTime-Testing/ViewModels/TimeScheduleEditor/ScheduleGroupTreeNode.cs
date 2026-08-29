using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 计划表树节点（统一模型，参考 ClassIsland 的 ClassPlansTreeNode）
/// 表组节点：IsGroup=true，Children 包含该组下的计划表
/// 计划表节点：IsGroup=false，叶子节点
/// </summary>
public partial class ScheduleGroupTreeNode : ObservableObject
{
    /// <summary>
    /// 是否为表组节点（true=表组, false=计划表）
    /// </summary>
    public bool IsGroup { get; set; }

    /// <summary>
    /// 表组节点的组ID
    /// </summary>
    public string? GroupId { get; set; }

    /// <summary>
    /// 计划表节点的计划表ID
    /// </summary>
    public string? ScheduleId { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    [ObservableProperty]
    private string _name = "";

    /// <summary>
    /// 是否展开（仅表组节点使用）
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// 是否被选中
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 是否为活跃计划表（仅计划表节点使用）
    /// </summary>
    [ObservableProperty]
    private bool _isActivated;

    /// <summary>
    /// 轮换周期数（仅表组节点使用，1=不轮换）
    /// </summary>
    [ObservableProperty]
    private int _rotationCycleCount;

    /// <summary>
    /// 轮换信息描述（仅表组节点使用，如 "第1/2周"）
    /// </summary>
    [ObservableProperty]
    private string? _rotationInfo;

    /// <summary>
    /// 同日冲突提示（仅表组节点使用，如 "同日有2张表"）
    /// </summary>
    [ObservableProperty]
    private string? _duplicateDayWarning;

    /// <summary>
    /// 所属组名称（仅计划表节点使用）
    /// </summary>
    [ObservableProperty]
    private string? _groupName;

    /// <summary>
    /// 最后更新时间（仅计划表节点使用）
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 计划表是否启用（仅计划表节点使用，禁用时显示删除线）
    /// </summary>
    [ObservableProperty]
    private bool _isScheduleEnabled = true;

    /// <summary>
    /// 子节点集合（仅表组节点使用）
    /// </summary>
    public ObservableCollection<ScheduleGroupTreeNode> Children { get; } = new();
}
