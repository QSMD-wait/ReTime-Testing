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
        Fonts = BuildFontList();
    }

    private TextOverlayLayoutPageViewModel? ViewModel => DataContext as TextOverlayLayoutPageViewModel;

    public FontOption[] Fonts { get; }

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

    private void FontCombo_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        if (combo.SelectedIndex != -1) return;

        if (combo.DataContext is not TextSlotItemViewModel slot) return;
        if (string.IsNullOrEmpty(slot.FontFamily))
            combo.SelectedIndex = 0;
    }

    private void OnCategoryChecked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton rb || rb.Content is not string category || ViewModel == null) return;
        ViewModel.SelectedCategory = category;
    }
}

public record FontOption(string Display, string Name);