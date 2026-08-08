using System;
using CommunityToolkit.Mvvm.ComponentModel;
using iNKORE.UI.WPF.Modern.Common.IconKeys;
using ReTime_Testing.Models;

namespace ReTime_Testing.ViewModels;

public partial class TextSlotItemViewModel : ObservableObject
{
    private readonly Action? _saveCallback;
    private bool _isInitializing = true;

    public TextSlotConfig Config { get; }

    [ObservableProperty]
    private int _sourceTypeIndex;

    [ObservableProperty]
    private string _customText = "";

    [ObservableProperty]
    private string _format = "";

    [ObservableProperty]
    private bool _showSeconds = true;

    [ObservableProperty]
    private int _decimalPlaces = 1;

    [ObservableProperty]
    private string _fallback = "";

    [ObservableProperty]
    private bool _showTime;

    [ObservableProperty]
    private bool _visible = true;

    [ObservableProperty]
    private string _prefix = "";

    [ObservableProperty]
    private string _suffix = "";

    [ObservableProperty]
    private double? _fontSizeOverride;

    public double FontSizeOverrideValue
    {
        get => FontSizeOverride ?? 0;
        set
        {
            if (value <= 0)
                FontSizeOverride = null;
            else
                FontSizeOverride = value;
        }
    }

    [ObservableProperty]
    private string _colorOverride = "";

    [ObservableProperty]
    private string _fontFamily = "";

    private static readonly TextSourceType[] SourceTypeValues =
        Enum.GetValues<TextSourceType>();

    public TextSourceType CurrentSourceType
    {
        get => SourceTypeValues[SourceTypeIndex];
        set
        {
            var newIndex = Array.IndexOf(SourceTypeValues, value);
            if (newIndex >= 0 && newIndex != SourceTypeIndex)
                SourceTypeIndex = newIndex;
        }
    }

    public string DisplayName => CurrentSourceType == TextSourceType.CustomText
        ? (string.IsNullOrWhiteSpace(CustomText) ? "自定义文本" : CustomText)
        : CurrentSourceType == TextSourceType.None ? "（空）" : CurrentSourceType.GetDisplayName();

    public string DisplayDescription => CurrentSourceType switch
    {
        TextSourceType.None => "不显示任何内容",
        TextSourceType.CustomText => string.IsNullOrWhiteSpace(CustomText) ? "自定义文本" : CustomText,
        TextSourceType.SegmentName => "当前时间段名称",
        TextSourceType.NextSegment => "下一段名称",
        _ => CurrentSourceType.GetDisplayName()
    };

    public FontIconData? IconGlyph => CurrentSourceType switch
    {
        TextSourceType.None => FluentSystemIcons.Dismiss_24_Regular,
        TextSourceType.CustomText => FluentSystemIcons.TextT_24_Regular,
        TextSourceType.SegmentName => FluentSystemIcons.Tag_24_Regular,
        TextSourceType.RemainingTime => FluentSystemIcons.Hourglass_24_Regular,
        TextSourceType.ElapsedTime => FluentSystemIcons.History_24_Regular,
        TextSourceType.ProgressPercent => FluentSystemIcons.DataUsage_24_Regular,
        TextSourceType.CurrentTime => FluentSystemIcons.Clock_24_Regular,
        TextSourceType.NextSegment => FluentSystemIcons.FastForward_24_Regular,
        TextSourceType.CurrentDate => FluentSystemIcons.Calendar_24_Regular,
        TextSourceType.CurrentDayOfWeek => FluentSystemIcons.CalendarWeekNumbers_24_Regular,
        _ => FluentSystemIcons.QuestionCircle_24_Regular
    };

    public bool IsSourceConfigurable => CurrentSourceType != TextSourceType.None;

    public bool IsCustomTextRelevant => CurrentSourceType == TextSourceType.CustomText;

    public bool IsFormatRelevant => CurrentSourceType is TextSourceType.CurrentTime or TextSourceType.CurrentDate or TextSourceType.CurrentDayOfWeek;

    public bool IsShowSecondsRelevant => CurrentSourceType is TextSourceType.RemainingTime or TextSourceType.ElapsedTime;

    public bool IsDecimalPlacesRelevant => CurrentSourceType == TextSourceType.ProgressPercent;

    public bool IsFallbackRelevant => CurrentSourceType is TextSourceType.SegmentName or TextSourceType.NextSegment;

    public bool IsShowTimeRelevant => CurrentSourceType == TextSourceType.NextSegment;

    public FormatPresetOption[] FormatPresets => CurrentSourceType switch
    {
        TextSourceType.CurrentTime => [
            new("HH:mm", "HH:mm"),
            new("HH:mm:ss", "HH:mm:ss"),
            new("hh:mm tt", "hh:mm tt"),
            new("HH时mm分", "HH时mm分"),
        ],
        TextSourceType.CurrentDate => [
            new("yyyy-MM-dd", "yyyy-MM-dd"),
            new("yyyy/MM/dd", "yyyy/MM/dd"),
            new("yyyy年MM月dd日", "yyyy年MM月dd日"),
            new("MM月dd日", "MM月dd日"),
        ],
        TextSourceType.CurrentDayOfWeek => [
            new("星期X", "星期X"),
            new("周X", "周X"),
            new("dddd", "dddd"),
            new("ddd", "ddd"),
        ],
        _ => []
    };

    public TextSlotItemViewModel(TextSlotConfig config, Action? saveCallback)
    {
        Config = config;
        _saveCallback = saveCallback;

        SourceTypeIndex = Array.IndexOf(SourceTypeValues, config.Source);
        CustomText = config.SourceSettings.Text ?? "";
        Format = config.SourceSettings.Format ?? "";
        ShowSeconds = config.SourceSettings.ShowSeconds ?? true;
        DecimalPlaces = config.SourceSettings.DecimalPlaces ?? 1;
        Fallback = config.SourceSettings.Fallback ?? "";
        ShowTime = config.SourceSettings.ShowTime ?? false;

        Visible = config.CommonSettings.Visible;
        Prefix = config.CommonSettings.Prefix ?? "";
        Suffix = config.CommonSettings.Suffix ?? "";
        FontSizeOverride = config.CommonSettings.FontSizeOverride;
        ColorOverride = config.CommonSettings.ColorOverride ?? "";
        FontFamily = config.CommonSettings.FontFamily ?? "";

        EnsureValidFormat();
        _isInitializing = false;
    }

    public void WriteBack()
    {
        Config.Source = SourceTypeValues[SourceTypeIndex];
        Config.SourceSettings.Text = string.IsNullOrWhiteSpace(CustomText) ? null : CustomText;
        Config.SourceSettings.Format = string.IsNullOrWhiteSpace(Format) ? null : Format;
        Config.SourceSettings.ShowSeconds = ShowSeconds;
        Config.SourceSettings.DecimalPlaces = DecimalPlaces;
        Config.SourceSettings.Fallback = string.IsNullOrWhiteSpace(Fallback) ? null : Fallback;
        Config.SourceSettings.ShowTime = ShowTime;

        Config.CommonSettings.Visible = Visible;
        Config.CommonSettings.Prefix = string.IsNullOrWhiteSpace(Prefix) ? null : Prefix;
        Config.CommonSettings.Suffix = string.IsNullOrWhiteSpace(Suffix) ? null : Suffix;
        Config.CommonSettings.FontSizeOverride = FontSizeOverride;
        Config.CommonSettings.ColorOverride = string.IsNullOrWhiteSpace(ColorOverride) ? null : ColorOverride;
        Config.CommonSettings.FontFamily = string.IsNullOrWhiteSpace(FontFamily) ? null : FontFamily;
    }

    private void OnSave()
    {
        if (_isInitializing) return;
        WriteBack();
        _saveCallback?.Invoke();
    }

    partial void OnSourceTypeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentSourceType));
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DisplayDescription));
        OnPropertyChanged(nameof(IconGlyph));
        OnPropertyChanged(nameof(IsSourceConfigurable));
        OnPropertyChanged(nameof(IsCustomTextRelevant));
        OnPropertyChanged(nameof(IsFormatRelevant));
        OnPropertyChanged(nameof(FormatPresets));
        OnPropertyChanged(nameof(IsShowSecondsRelevant));
        OnPropertyChanged(nameof(IsDecimalPlacesRelevant));
        OnPropertyChanged(nameof(IsFallbackRelevant));
        OnPropertyChanged(nameof(IsShowTimeRelevant));

        EnsureValidFormat();

        OnSave();
    }
    partial void OnCustomTextChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnSave();
    }
    partial void OnFormatChanged(string value) => OnSave();
    partial void OnShowSecondsChanged(bool value) => OnSave();
    partial void OnDecimalPlacesChanged(int value) => OnSave();
    partial void OnFallbackChanged(string value) => OnSave();
    partial void OnShowTimeChanged(bool value) => OnSave();
    partial void OnVisibleChanged(bool value) => OnSave();
    partial void OnPrefixChanged(string value) => OnSave();
    partial void OnSuffixChanged(string value) => OnSave();
    partial void OnFontSizeOverrideChanged(double? value)
    {
        OnPropertyChanged(nameof(FontSizeOverrideValue));
        OnSave();
    }
    partial void OnColorOverrideChanged(string value) => OnSave();
    partial void OnFontFamilyChanged(string value) => OnSave();

    private void EnsureValidFormat()
    {
        var presets = FormatPresets;
        if (presets.Length > 0 && !presets.Any(p => p.Format == Format))
            Format = presets[0].Format;
    }
}