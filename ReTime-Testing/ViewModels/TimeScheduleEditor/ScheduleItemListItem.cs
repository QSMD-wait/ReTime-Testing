using System;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ReTime_Testing.Models;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 统一列表项（时间段+时间点）
/// </summary>
public partial class ScheduleItemListItem : ObservableObject
{
    public string Id { get; set; } = "";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _startTime = "";

    [ObservableProperty]
    private string _endTime = "";

    public string TypeIcon { get; set; } = "\uE787";
    public ScheduleItemType ItemType { get; set; }

    [ObservableProperty]
    private ProgressStateType _toState;

    [ObservableProperty]
    private string _startTimeError = "";

    public bool HasStartTimeError => !string.IsNullOrEmpty(StartTimeError);

    [ObservableProperty]
    private string _endTimeError = "";

    public bool HasEndTimeError => !string.IsNullOrEmpty(EndTimeError);

    [ObservableProperty]
    private byte _foregroundR = 0x00;

    [ObservableProperty]
    private byte _foregroundG = 0x67;

    [ObservableProperty]
    private byte _foregroundB = 0xC0;

    [ObservableProperty]
    private byte _backgroundR = 0;

    [ObservableProperty]
    private byte _backgroundG = 0;

    [ObservableProperty]
    private byte _backgroundB = 0;

    [ObservableProperty]
    private double _opacity = 100;

    [ObservableProperty]
    private bool _hasCustomStyle = false;

    [ObservableProperty]
    private bool _hasBackgroundColor;

    public Color PreviewColor => Color.FromArgb((byte)(Opacity * 2.55), ForegroundR, ForegroundG, ForegroundB);

    public Color PreviewBackgroundColor => Color.FromArgb((byte)(Opacity * 2.55), BackgroundR, BackgroundG, BackgroundB);

    public string Duration
    {
        get
        {
            if (ItemType != ScheduleItemType.Segment) return "";
            if (!TimeSpan.TryParse(StartTime, out var start) || !TimeSpan.TryParse(EndTime, out var end))
                return "";
            var diff = end - start;
            if (diff < TimeSpan.Zero) diff = diff.Add(TimeSpan.FromDays(1));
            if (diff.TotalHours >= 1)
                return $"{(int)diff.TotalHours}时{diff.Minutes}分{diff.Seconds}秒";
            if (diff.TotalMinutes >= 1)
                return $"{diff.Minutes}分{diff.Seconds}秒";
            return $"{diff.Seconds}秒";
        }
    }

    public string HexColor
    {
        get => $"#{ForegroundR:X2}{ForegroundG:X2}{ForegroundB:X2}";
        set
        {
            if (string.IsNullOrEmpty(value)) return;
            try
            {
                var v = value.StartsWith("#") ? value.Substring(1) : value;
                if (v.Length == 6)
                {
                    ForegroundR = Convert.ToByte(v.Substring(0, 2), 16);
                    ForegroundG = Convert.ToByte(v.Substring(2, 2), 16);
                    ForegroundB = Convert.ToByte(v.Substring(4, 2), 16);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("ScheduleItemListItem", $"颜色格式解析失败: {value}, 错误: {ex.Message}");
            }
        }
    }

    public string BackgroundHexColor
    {
        get => $"#{BackgroundR:X2}{BackgroundG:X2}{BackgroundB:X2}";
        set
        {
            if (string.IsNullOrEmpty(value)) return;
            try
            {
                var v = value.StartsWith("#") ? value.Substring(1) : value;
                if (v.Length == 6)
                {
                    BackgroundR = Convert.ToByte(v.Substring(0, 2), 16);
                    BackgroundG = Convert.ToByte(v.Substring(2, 2), 16);
                    BackgroundB = Convert.ToByte(v.Substring(4, 2), 16);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("ScheduleItemListItem", $"背景色格式解析失败: {value}, 错误: {ex.Message}");
            }
        }
    }

    public event Action<ScheduleItemListItem>? ItemChanged;

    partial void OnNameChanged(string value) => ItemChanged?.Invoke(this);

    partial void OnStartTimeChanged(string value)
    {
        OnPropertyChanged(nameof(Duration));
        ItemChanged?.Invoke(this);
    }

    partial void OnEndTimeChanged(string value)
    {
        OnPropertyChanged(nameof(Duration));
        ItemChanged?.Invoke(this);
    }

    partial void OnToStateChanged(ProgressStateType value) => ItemChanged?.Invoke(this);

    partial void OnForegroundRChanged(byte value)
    {
        OnPropertyChanged(nameof(PreviewColor));
        OnPropertyChanged(nameof(HexColor));
        ItemChanged?.Invoke(this);
    }

    partial void OnForegroundGChanged(byte value)
    {
        OnPropertyChanged(nameof(PreviewColor));
        OnPropertyChanged(nameof(HexColor));
        ItemChanged?.Invoke(this);
    }

    partial void OnForegroundBChanged(byte value)
    {
        OnPropertyChanged(nameof(PreviewColor));
        OnPropertyChanged(nameof(HexColor));
        ItemChanged?.Invoke(this);
    }

    partial void OnOpacityChanged(double value)
    {
        OnPropertyChanged(nameof(PreviewColor));
        OnPropertyChanged(nameof(PreviewBackgroundColor));
        ItemChanged?.Invoke(this);
    }

    partial void OnHasCustomStyleChanged(bool value)
    {
        if (!value)
        {
            HasBackgroundColor = false;
        }
        ItemChanged?.Invoke(this);
    }

    partial void OnHasBackgroundColorChanged(bool value) => ItemChanged?.Invoke(this);

    partial void OnStartTimeErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasStartTimeError));
    }

    partial void OnEndTimeErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasEndTimeError));
    }

    partial void OnBackgroundRChanged(byte value)
    {
        OnPropertyChanged(nameof(PreviewBackgroundColor));
        OnPropertyChanged(nameof(BackgroundHexColor));
        HasBackgroundColor = true;
        ItemChanged?.Invoke(this);
    }

    partial void OnBackgroundGChanged(byte value)
    {
        OnPropertyChanged(nameof(PreviewBackgroundColor));
        OnPropertyChanged(nameof(BackgroundHexColor));
        HasBackgroundColor = true;
        ItemChanged?.Invoke(this);
    }

    partial void OnBackgroundBChanged(byte value)
    {
        OnPropertyChanged(nameof(PreviewBackgroundColor));
        OnPropertyChanged(nameof(BackgroundHexColor));
        HasBackgroundColor = true;
        ItemChanged?.Invoke(this);
    }
}