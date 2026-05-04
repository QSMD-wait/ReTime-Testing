using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 文字插槽解析器
/// 根据数据源类型和调度上下文解析出显示文本
/// </summary>
public class TextSlotResolver : ITextSlotResolver
{
    private readonly IScheduleManager _scheduleManager;
    private readonly ITimeService _timeService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TextSlotResolver(IScheduleManager scheduleManager, ITimeService timeService)
    {
        _scheduleManager = scheduleManager;
        _timeService = timeService;
    }

    /// <inheritdoc/>
    public string Resolve(TextSourceType source, string? customText = null)
    {
        return source switch
        {
            TextSourceType.None => string.Empty,
            TextSourceType.CustomText => customText ?? string.Empty,
            TextSourceType.SegmentName => ResolveSegmentName(),
            TextSourceType.RemainingTime => ResolveRemainingTime(),
            TextSourceType.ElapsedTime => ResolveElapsedTime(),
            TextSourceType.ProgressPercent => ResolveProgressPercent(),
            TextSourceType.CurrentTime => ResolveCurrentTime(),
            TextSourceType.NextSegment => ResolveNextSegment(),
            _ => string.Empty
        };
    }

    private string ResolveSegmentName()
    {
        return _scheduleManager.CurrentPlan?.CurrentSegment?.Name ?? "";
    }

    private string ResolveRemainingTime()
    {
        var segment = _scheduleManager.CurrentPlan?.CurrentSegment;
        if (segment == null) return "";

        var now = _timeService.GetCurrentTime();
        var remaining = segment.EndTime - now;
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

        return FormatDuration(remaining);
    }

    private string ResolveElapsedTime()
    {
        var segment = _scheduleManager.CurrentPlan?.CurrentSegment;
        if (segment == null) return "";

        var now = _timeService.GetCurrentTime();
        var elapsed = now - segment.StartTime;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

        return FormatDuration(elapsed);
    }

    private string ResolveProgressPercent()
    {
        var segment = _scheduleManager.CurrentPlan?.CurrentSegment;
        if (segment == null || segment.Duration == TimeSpan.Zero) return "";

        var now = _timeService.GetCurrentTime();
        var elapsed = now - segment.StartTime;
        var percent = Math.Clamp(elapsed.TotalMilliseconds / segment.Duration.TotalMilliseconds * 100, 0, 100);

        return $"{percent:F1}%";
    }

    private string ResolveCurrentTime()
    {
        return _timeService.GetCurrentTime().ToString("HH:mm:ss");
    }

    private string ResolveNextSegment()
    {
        var plan = _scheduleManager.CurrentPlan;
        if (plan == null) return "";

        var now = _timeService.GetCurrentTime();
        var nextSegment = plan.TimeSegments
            .Where(s => s.StartTime > now)
            .OrderBy(s => s.StartTime)
            .FirstOrDefault();

        return nextSegment?.Name ?? "";
    }

    /// <summary>
    /// 格式化时长为 Xh Xm Xs 格式
    /// </summary>
    private static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return "0s";

        var parts = new List<string>();

        if (duration.TotalHours >= 1)
            parts.Add($"{(int)duration.TotalHours}h");

        if (duration.Minutes > 0 || duration.TotalHours >= 1)
            parts.Add($"{duration.Minutes}m");

        parts.Add($"{duration.Seconds}s");

        return string.Join(" ", parts);
    }
}
