using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.Settings.Pages;

public partial class TextOverlayLayoutPage : SettingsPageBase
{
    public TextOverlayLayoutPage()
    {
        InitializeComponent();
    }

    private TextOverlayLayoutPageViewModel? ViewModel => DataContext as TextOverlayLayoutPageViewModel;

    #region 插槽组操作

    private void OnSlotDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || ViewModel == null) return;
        if (fe.DataContext is not TextSlotItemViewModel item) return;
        var groupIndex = ViewModel.SelectedGroupIndex;
        ViewModel.RemoveSlot(groupIndex, item);
    }

    #endregion

    #region 组件库操作

    private void OnCategoryChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || rb.Content is not string category || ViewModel == null) return;
        ViewModel.SelectedCategory = category;
    }

    #endregion

    #region 排序与删除

    private void OnMoveSelectedUp(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedSlot == null) return;
        ViewModel.MoveSlotUp(ViewModel.SelectedGroupIndex, ViewModel.SelectedSlot);
    }

    private void OnMoveSelectedDown(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedSlot == null) return;
        ViewModel.MoveSlotDown(ViewModel.SelectedGroupIndex, ViewModel.SelectedSlot);
    }

    private void OnRemoveSelectedSlot(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedSlot == null) return;
        ViewModel.RemoveSlot(ViewModel.SelectedGroupIndex, ViewModel.SelectedSlot);
    }

    #endregion
}