using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Globalization;
using iNKORE.UI.WPF.Modern.Controls;
using ReTime_Testing.Models;
using ReTime_Testing.ViewModels.TimeScheduleEditor;

namespace ReTime_Testing.Services;

/// <summary>
/// 时间计划项转换工具类
/// </summary>
public static class ScheduleItemConverter
{
    // 时间点图标 (Segoe Fluent Icons)
    private const string TimePointIcon = "\uE823";
    // 时间段图标 (Segoe Fluent Icons)
    private const string TimeSegmentIcon = "\uE787";

    /// <summary>
    /// 将时间段实体转换为列表项
    /// </summary>
    public static ScheduleItemListItem ToListItem(TimeScheduleItem item)
    {
        var result = new ScheduleItemListItem
        {
            Id = item.Id,
            Name = item.Name,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            TypeIcon = TimeSegmentIcon,
            ItemType = ScheduleItemType.Segment
        };

        // 加载样式（只有 Enabled == true 时才启用自定义样式）
        if (item.Styles != null && item.Styles.Enabled == true)
        {
            result.HasCustomStyle = true;
            if (!string.IsNullOrEmpty(item.Styles.ForegroundColor))
            {
                var color = ParseColor(item.Styles.ForegroundColor);
                result.ForegroundR = color.R;
                result.ForegroundG = color.G;
                result.ForegroundB = color.B;
            }
            if (!string.IsNullOrEmpty(item.Styles.BackgroundColor))
            {
                var color = ParseColor(item.Styles.BackgroundColor);
                result.BackgroundR = color.R;
                result.BackgroundG = color.G;
                result.BackgroundB = color.B;
                result.HasBackgroundColor = true;
            }
            if (item.Styles.Opacity.HasValue)
            {
                result.Opacity = item.Styles.Opacity.Value * 100;
            }
        }
        else
        {
            result.HasCustomStyle = false;
        }

        return result;
    }

    /// <summary>
    /// 将时间点实体转换为列表项
    /// </summary>
    public static ScheduleItemListItem ToListItem(CustomTimePoint point)
    {
        var result = new ScheduleItemListItem
        {
            Id = point.Id,
            Name = point.Name,
            StartTime = point.Time,
            TypeIcon = TimePointIcon,
            ItemType = ScheduleItemType.TimePoint,
        };

        // 尝试从 StateChange 中读取 ToState
        if (point.StateChange != null && point.StateChange.ToState != default)
        {
            result.ToState = point.StateChange.ToState;
        }

        // 加载样式（从 StyleChange 中读取）
        bool hasStyle = false;
        if (point.StyleChange != null)
        {
            if (!string.IsNullOrEmpty(point.StyleChange.ForegroundColor))
            {
                var color = ParseColor(point.StyleChange.ForegroundColor);
                result.ForegroundR = color.R;
                result.ForegroundG = color.G;
                result.ForegroundB = color.B;
                hasStyle = true;
            }
            if (!string.IsNullOrEmpty(point.StyleChange.BackgroundColor))
            {
                var color = ParseColor(point.StyleChange.BackgroundColor);
                result.BackgroundR = color.R;
                result.BackgroundG = color.G;
                result.BackgroundB = color.B;
                result.HasBackgroundColor = true;
                hasStyle = true;
            }
            if (point.StyleChange.Opacity.HasValue)
            {
                result.Opacity = point.StyleChange.Opacity.Value * 100;
                hasStyle = true;
            }
        }
        result.HasCustomStyle = hasStyle;

        return result;
    }

    /// <summary>
    /// 解析颜色字符串为 Color
    /// </summary>
    private static Color ParseColor(string colorString)
    {
        try
        {
            if (colorString.StartsWith("#"))
            {
                if (colorString.Length == 7) // #RRGGBB
                {
                    return Color.FromRgb(
                        Convert.ToByte(colorString.Substring(1, 2), 16),
                        Convert.ToByte(colorString.Substring(3, 2), 16),
                        Convert.ToByte(colorString.Substring(5, 2), 16));
                }
                else if (colorString.Length == 9) // #AARRGGBB
                {
                    return Color.FromArgb(
                        Convert.ToByte(colorString.Substring(1, 2), 16),
                        Convert.ToByte(colorString.Substring(3, 2), 16),
                        Convert.ToByte(colorString.Substring(5, 2), 16),
                        Convert.ToByte(colorString.Substring(7, 2), 16));
                }
            }
            return Colors.White;
        }
        catch
        {
            return Colors.White;
        }
    }

    /// <summary>
    /// 将计划表实体集合转换为列表项集合
    /// </summary>
    public static ObservableCollection<ScheduleItemListItem> ToListItems(
        IEnumerable<TimeScheduleItem>? schedules,
        IEnumerable<CustomTimePoint>? timePoints)
    {
        var result = new ObservableCollection<ScheduleItemListItem>();

        if (schedules != null)
        {
            foreach (var item in schedules)
            {
                result.Add(ToListItem(item));
            }
        }

        if (timePoints != null)
        {
            foreach (var point in timePoints)
            {
                result.Add(ToListItem(point));
            }
        }

        return result;
    }

    /// <summary>
    /// 将列表项转换回时间段实体
    /// </summary>
    public static TimeScheduleItem ToScheduleItem(ScheduleItemListItem item)
    {
        return new TimeScheduleItem
        {
            Id = item.Id,
            Name = item.Name,
            StartTime = item.StartTime,
            EndTime = item.EndTime
        };
    }

    /// <summary>
    /// 将列表项转换回时间点实体
    /// </summary>
    public static CustomTimePoint ToTimePoint(ScheduleItemListItem item)
    {
        var tp = new CustomTimePoint
        {
            Id = item.Id,
            Name = item.Name,
            Time = item.StartTime,
            Type = TimePointType.StateChange,
            StateChange = new StateChangeData
            {
                ToState = item.ToState
            }
        };

        // 写入样式到 StyleChange（如果设置了自定义样式）
        if (item.HasCustomStyle)
        {
            tp.Type = TimePointType.StyleChange;
            tp.StyleChange = new StyleChangeData
            {
                ForegroundColor = $"#{item.ForegroundR:X2}{item.ForegroundG:X2}{item.ForegroundB:X2}",
                BackgroundColor = item.HasBackgroundColor ? $"#{item.BackgroundR:X2}{item.BackgroundG:X2}{item.BackgroundB:X2}" : null,
                Opacity = item.Opacity / 100.0
            };
        }

        return tp;
    }
}