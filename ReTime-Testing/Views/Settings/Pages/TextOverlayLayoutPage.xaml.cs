using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.Settings.Pages;

public partial class TextOverlayLayoutPage : SettingsPageBase
{
    public TextOverlayLayoutPage()
    {
        InitializeComponent();
        Fonts = BuildFontList();
    }

    private TextOverlayLayoutPageViewModel? ViewModel => DataContext as TextOverlayLayoutPageViewModel;

    /// <summary>
    /// 可选字体列表（显示文本, 字体系列名称），首项为「使用全局字体」
    /// </summary>
    public FontOption[] Fonts { get; }

    /// <summary>
    /// 构建字体选项列表：首项为全局默认，其余为系统已安装字体（按名称排序）
    /// </summary>
    private static FontOption[] BuildFontList()
    {
        var systemFonts = System.Windows.Media.Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Split(',')[0].Trim())
            .Distinct()
            .OrderBy(s => s)
            .Select(s => new FontOption(s, s))
            .ToList();

        systemFonts.Insert(0, new FontOption("（使用全局字体）", ""));
        return systemFonts.ToArray();
    }

    /// <summary>
    /// 字体 ComboBox 加载完成后，仅在绑定未正确选中时进行后备初始化
    /// </summary>
    private void FontCombo_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        if (combo.SelectedIndex != -1) return;

        if (combo.DataContext is not TextSlotItemViewModel slot) return;
        if (string.IsNullOrEmpty(slot.FontFamily))
            combo.SelectedIndex = 0;
    }

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

/// <summary>
/// 字体选项（用于 ComboBox 绑定），不可变 record 确保 WPF 属性路径可访问
/// </summary>
public record FontOption(string Display, string Name);