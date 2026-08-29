using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 计划表列表项（绑定到ScheduleInfo）
/// </summary>
public partial class ScheduleListItem : ObservableObject
{
    public string Id { get; set; } = "";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isActivated;

    /// <summary>
    /// 所属计划表组ID
    /// </summary>
    [ObservableProperty]
    private string _associatedGroupId = "default";

    /// <summary>
    /// 是否自动启用
    /// </summary>
    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>
    /// 星期几（0=周日, 1=周一, ..., 6=周六）
    /// </summary>
    [ObservableProperty]
    private int _dayOfWeek;

    /// <summary>
    /// 轮换周数（1=每周, 2=双周, ..., 4=四周）
    /// </summary>
    [ObservableProperty]
    private int _rotationCycleCount = 1;

    /// <summary>
    /// 轮换周索引（0=每周, 1~N=第N轮换周）
    /// </summary>
    [ObservableProperty]
    private int _rotationWeekIndex;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
