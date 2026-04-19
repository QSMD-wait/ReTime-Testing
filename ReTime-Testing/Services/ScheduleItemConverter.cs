using System.Collections.ObjectModel;
using System.Windows.Media;
using iNKORE.UI.WPF.Modern.Controls;
using ReTime_Testing.Models;
using ReTime_Testing.Views.TimeScheduleEditor;

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
            ToState = point.ToState
        };

        // 加载样式（只有 Enabled == true 时才启用自定义样式）
        if (point.Style != null && point.Style.Enabled == true)
        {
            result.HasCustomStyle = true;
            if (!string.IsNullOrEmpty(point.Style.ForegroundColor))
            {
                var color = ParseColor(point.Style.ForegroundColor);
                result.ForegroundR = color.R;
                result.ForegroundG = color.G;
                result.ForegroundB = color.B;
            }
            if (!string.IsNullOrEmpty(point.Style.BackgroundColor))
            {
                var color = ParseColor(point.Style.BackgroundColor);
                result.BackgroundR = color.R;
                result.BackgroundG = color.G;
                result.BackgroundB = color.B;
            }
            if (point.Style.Opacity.HasValue)
            {
                result.Opacity = point.Style.Opacity.Value * 100;
            }
        }
        else
        {
            result.HasCustomStyle = false;
        }

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
        return new CustomTimePoint
        {
            Id = item.Id,
            Name = item.Name,
            Time = item.StartTime,
            ToState = item.ToState
        };
    }
}