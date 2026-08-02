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
        DataContextChanged += OnDataContextChanged;
    }

    private TextOverlayLayoutPageViewModel? ViewModel => DataContext as TextOverlayLayoutPageViewModel;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TextOverlayLayoutPageViewModel oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is TextOverlayLayoutPageViewModel newVm)
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        UpdateHintVisibility();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TextOverlayLayoutPageViewModel.SelectedSlot))
            UpdateHintVisibility();
    }

    private void UpdateHintVisibility()
    {
        if (HintText == null) return;
        HintText.Visibility = ViewModel?.SelectedSlot == null ? Visibility.Visible : Visibility.Collapsed;
    }

    #region 插槽组操作

    private void OnAddLeftSlot(object sender, RoutedEventArgs e) => ViewModel?.AddSlot(0);
    private void OnAddCenterSlot(object sender, RoutedEventArgs e) => ViewModel?.AddSlot(1);
    private void OnAddRightSlot(object sender, RoutedEventArgs e) => ViewModel?.AddSlot(2);

    private void OnSlotClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not TextSlotItemViewModel item || ViewModel == null) return;
        var groupIndex = DetermineGroupIndex(item);
        ViewModel.SelectSlot(groupIndex, item);
    }

    private int DetermineGroupIndex(TextSlotItemViewModel item)
    {
        if (ViewModel == null) return 0;
        if (ViewModel.LeftSlots.Contains(item)) return 0;
        if (ViewModel.CenterSlots.Contains(item)) return 1;
        if (ViewModel.RightSlots.Contains(item)) return 2;
        return ViewModel.SelectedGroupIndex;
    }

    private void OnSlotDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || ViewModel == null) return;
        if (fe.DataContext is not TextSlotItemViewModel item) return;
        var groupIndex = ViewModel.SelectedGroupIndex;
        ViewModel.RemoveSlot(groupIndex, item);
    }

    #endregion

    #region 组件库操作

    private void OnComponentClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not ComponentLibraryItem comp || ViewModel == null) return;
        var groupIndex = ViewModel.SelectedGroupIndex;
        ViewModel.AddSlotFromComponent(groupIndex, comp.SourceType);
    }

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