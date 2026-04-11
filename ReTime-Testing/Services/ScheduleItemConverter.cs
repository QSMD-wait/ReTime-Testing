using System.Collections.ObjectModel;
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
        return new ScheduleItemListItem
        {
            Id = item.Id,
            Name = item.Name,
            StartTime = item.StartTime,
            EndTime = item.EndTime,
            TypeIcon = TimeSegmentIcon,
            IsTimePoint = false
        };
    }

    /// <summary>
    /// 将时间点实体转换为列表项
    /// </summary>
    public static ScheduleItemListItem ToListItem(CustomTimePoint point)
    {
        return new ScheduleItemListItem
        {
            Id = point.Id,
            Name = point.Name,
            StartTime = point.Time,
            TypeIcon = TimePointIcon,
            IsTimePoint = true,
            ToState = point.ToState
        };
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