using ReTime_Testing.Models;

namespace ReTime_Testing.ViewModels;

public static class TextSourceTypeExtensions
{
    public static string GetDisplayName(this TextSourceType source) => source switch
    {
        TextSourceType.None => "不显示",
        TextSourceType.CustomText => "自定义文本",
        TextSourceType.SegmentName => "当前段名",
        TextSourceType.RemainingTime => "剩余时间",
        TextSourceType.ElapsedTime => "已过时间",
        TextSourceType.ProgressPercent => "进度百分比",
        TextSourceType.CurrentTime => "当前时间",
        TextSourceType.NextSegment => "下一段名",
        TextSourceType.CurrentDate => "当前日期",
        TextSourceType.CurrentDayOfWeek => "当前星期",
        _ => "未知"
    };
}