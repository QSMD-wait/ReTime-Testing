using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReTime_Testing.ViewModels.Testing;

/// <summary>
/// 抽屉控件测试 ViewModel
/// 职责：控制 DrawerHost 的开关状态和抽屉内容
/// </summary>
public partial class DrawerTestViewModel : ObservableObject
{
    public string TabTitle => "抽屉";

    [ObservableProperty]
    private bool _isDrawerOpen;

    [ObservableProperty]
    private double _drawerWidth = 320;

    [ObservableProperty]
    private int _selectedContentIndex;

    public string[] ContentOptions { get; } = ["简单文本", "表单面板", "列表面板"];

    [RelayCommand]
    private void ToggleDrawer()
    {
        IsDrawerOpen = !IsDrawerOpen;
    }

    [RelayCommand]
    private void OpenDrawer()
    {
        IsDrawerOpen = true;
    }

    [RelayCommand]
    private void CloseDrawer()
    {
        IsDrawerOpen = false;
    }
}
