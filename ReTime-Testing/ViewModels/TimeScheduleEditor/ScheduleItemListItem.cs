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

    public bool HasChanges { get; private set; }

    [ObservableProperty]
    private string _startTime = "";

    [ObservableProperty]
    private string _endTime = "";

    public string TypeIcon { get; set; } = "\uE787";
    public ScheduleItemType ItemType { get; set; }
    public ProgressStateType ToState { get; set; }

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

    /// <summary>
    /// 背景色是否为原始数据（非默认值零），用于避免序列化污染
    /// </summary>
    public bool HasBackgroundColor { get; set; }

    public Color PreviewColor => Color.FromArgb((byte)(Opacity * 2.55), ForegroundR, ForegroundG, ForegroundB);

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

    partial void OnNameChanged(string value)
    {
        HasChanges = true;
    }

    partial void OnForegroundRChanged(byte value)
    {
        OnPropertyChanged(nameof(PreviewColor));
        OnPropertyChanged(nameof(HexColor));
    }

    partial void OnForegroundGChanged(byte value)
    {
        OnPropertyChanged(nameof(PreviewColor));
        OnPropertyChanged(nameof(HexColor));
    }

    partial void OnForegroundBChanged(byte value)
    {
        OnPropertyChanged(nameof(PreviewColor));
        OnPropertyChanged(nameof(HexColor));
    }

    partial void OnOpacityChanged(double value)
    {
        OnPropertyChanged(nameof(PreviewColor));
    }

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
        HasBackgroundColor = true;
    }

    partial void OnBackgroundGChanged(byte value)
    {
        HasBackgroundColor = true;
    }

    partial void OnBackgroundBChanged(byte value)
    {
        HasBackgroundColor = true;
    }
}