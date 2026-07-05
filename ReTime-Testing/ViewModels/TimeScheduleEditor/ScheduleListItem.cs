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
    private bool _isActivated;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}