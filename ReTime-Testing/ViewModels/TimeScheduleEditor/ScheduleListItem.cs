using CommunityToolkit.Mvvm.ComponentModel;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 计划表列表项（绑定到ScheduleInfo）
/// </summary>
public partial class ScheduleListItem : ObservableObject
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    [ObservableProperty]
    private bool _isActivated;
}