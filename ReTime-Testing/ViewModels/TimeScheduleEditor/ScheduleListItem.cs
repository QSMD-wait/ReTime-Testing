using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 计划表列表项（绑定到ScheduleInfo）
/// 天→表映射、轮转、组归属已移至 ScheduleGroup 层
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
    /// 是否启用
    /// </summary>
    [ObservableProperty]
    private bool _isEnabled = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
