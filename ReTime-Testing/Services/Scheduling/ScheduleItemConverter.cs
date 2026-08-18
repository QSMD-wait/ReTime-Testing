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

        // 加载行为配置
        if (item.Behavior != null && item.Behavior.HasAnyOverride)
        {
            result.HasBehavior = true;
            result.PollingIntervalMs = item.Behavior.PollingIntervalMs ?? ScheduleBehavior.DefaultPollingIntervalMs;
            if (item.Behavior.ReverseProgress.HasValue)
                result.ReverseProgress = item.Behavior.ReverseProgress.Value;
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

        // 读取类型列表
        bool hasStateChange = point.Types != null && point.Types.Contains(TimePointType.StateChange);
        bool hasStyleChange = point.Types != null && point.Types.Contains(TimePointType.StyleChange);
        result.HasStateChange = hasStateChange;
        result.HasStyleChange = hasStyleChange;

        // 尝试从 StateChange 中读取 ToState
        if (hasStateChange && point.StateChange != null && point.StateChange.ToState != default)
        {
            result.ToState = point.StateChange.ToState;
        }

        // 加载样式（从 StyleChange 中读取）
        bool hasStyle = false;
        if (hasStyleChange && point.StyleChange != null)
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
    /// 将列表项转换回时间段实体（仅用于新建项）
    /// </summary>
    public static TimeScheduleItem ToScheduleItem(ScheduleItemListItem item)
    {
        var result = new TimeScheduleItem
        {
            Id = item.Id,
            Name = item.Name,
            StartTime = item.StartTime,
            EndTime = item.EndTime
        };

        ApplySegmentStyles(result, item);
        ApplySegmentBehavior(result, item);

        return result;
    }

    /// <summary>
    /// 将列表项转换回时间点实体（仅用于新建项）
    /// </summary>
    public static CustomTimePoint ToTimePoint(ScheduleItemListItem item)
    {
        var types = new List<TimePointType>();
        if (item.HasStateChange) types.Add(TimePointType.StateChange);
        if (item.HasStyleChange) types.Add(TimePointType.StyleChange);
        if (types.Count == 0) types.Add(TimePointType.StateChange);

        var tp = new CustomTimePoint
        {
            Id = item.Id,
            Name = item.Name,
            Time = item.StartTime,
            Types = types
        };

        if (item.HasStateChange)
        {
            tp.StateChange = new StateChangeData
            {
                ToState = item.ToState
            };
        }

        if (item.HasStyleChange && item.HasCustomStyle)
        {
            tp.StyleChange = new StyleChangeData
            {
                ForegroundColor = $"#{item.ForegroundR:X2}{item.ForegroundG:X2}{item.ForegroundB:X2}",
                BackgroundColor = item.HasBackgroundColor ? $"#{item.BackgroundR:X2}{item.BackgroundG:X2}{item.BackgroundB:X2}" : null,
                Opacity = item.Opacity / 100.0
            };
        }

        return tp;
    }

    /// <summary>
    /// 增量更新已有时间段实体（只修改编辑器管理的字段，保留其他字段）
    /// </summary>
    public static void ApplyListItemToSegment(ScheduleItemListItem item, TimeScheduleItem target)
    {
        target.Name = item.Name;
        target.StartTime = item.StartTime;
        target.EndTime = item.EndTime;

        ApplySegmentStyles(target, item);
        ApplySegmentBehavior(target, item);
    }

    /// <summary>
    /// 增量更新已有时间点实体（只修改编辑器管理的字段，保留其他字段如 FromState 等）
    /// </summary>
    public static void ApplyListItemToTimePoint(ScheduleItemListItem item, CustomTimePoint target)
    {
        target.Name = item.Name;
        target.Time = item.StartTime;

        // 根据 HasStateChange 更新 Types 和 StateChange
        if (item.HasStateChange)
        {
            if (!target.Types.Contains(TimePointType.StateChange))
            {
                target.Types.Insert(0, TimePointType.StateChange);
            }
            target.StateChange ??= new StateChangeData();
            target.StateChange.ToState = item.ToState;
        }
        else
        {
            target.Types.Remove(TimePointType.StateChange);
            target.StateChange = null;
        }

        // 根据 HasStyleChange 更新 Types 和 StyleChange
        if (item.HasStyleChange)
        {
            if (!target.Types.Contains(TimePointType.StyleChange))
            {
                target.Types.Add(TimePointType.StyleChange);
            }

            if (item.HasCustomStyle)
            {
                target.StyleChange = new StyleChangeData
                {
                    ForegroundColor = $"#{item.ForegroundR:X2}{item.ForegroundG:X2}{item.ForegroundB:X2}",
                    BackgroundColor = item.HasBackgroundColor ? $"#{item.BackgroundR:X2}{item.BackgroundG:X2}{item.BackgroundB:X2}" : null,
                    Opacity = item.Opacity / 100.0
                };
            }
            else
            {
                target.StyleChange = null;
            }
        }
        else
        {
            target.Types.Remove(TimePointType.StyleChange);
            target.StyleChange = null;
        }
    }

    /// <summary>
    /// 应用样式到时间段
    /// </summary>
    private static void ApplySegmentStyles(TimeScheduleItem segment, ScheduleItemListItem item)
    {
        segment.Styles ??= new StyleOverridesData();

        if (item.HasCustomStyle)
        {
            segment.Styles.Enabled = true;
            segment.Styles.ForegroundColor = $"#{item.ForegroundR:X2}{item.ForegroundG:X2}{item.ForegroundB:X2}";
            segment.Styles.BackgroundColor = item.HasBackgroundColor
                ? $"#{item.BackgroundR:X2}{item.BackgroundG:X2}{item.BackgroundB:X2}"
                : null;
            segment.Styles.Opacity = item.Opacity / 100.0;
        }
        else
        {
            segment.Styles.Enabled = false;
        }
    }

    /// <summary>
    /// 应用行为配置到时间段
    /// </summary>
    private static void ApplySegmentBehavior(TimeScheduleItem segment, ScheduleItemListItem item)
    {
        if (item.HasBehavior)
        {
            segment.Behavior ??= new ScheduleBehaviorData();
            segment.Behavior.PollingIntervalMs = item.PollingIntervalMs > 0 ? item.PollingIntervalMs : null;
            segment.Behavior.ReverseProgress = item.ReverseProgress;
        }
        else
        {
            segment.Behavior = null;
        }
    }
}