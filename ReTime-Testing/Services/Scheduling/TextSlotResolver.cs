using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

public class TextSlotResolver : ITextSlotResolver
{
    private readonly IScheduleManager _scheduleManager;
    private readonly ITimeService _timeService;

    public TextSlotResolver(IScheduleManager scheduleManager, ITimeService timeService)
    {
        _scheduleManager = scheduleManager;
        _timeService = timeService;
    }

    public string Resolve(TextSourceType source, TextSlotSourceSettings? sourceSettings = null, TextSlotCommonSettings? commonSettings = null)
    {
        sourceSettings ??= new TextSlotSourceSettings();
        commonSettings ??= new TextSlotCommonSettings();

        if (!commonSettings.Visible)
            return string.Empty;

        string content;
        switch (source)
        {
            case TextSourceType.None: content = string.Empty; break;
            case TextSourceType.CustomText: content = ResolveCustomText(sourceSettings); break;
            case TextSourceType.SegmentName: content = ResolveSegmentName(sourceSettings); break;
            case TextSourceType.RemainingTime: content = ResolveRemainingTime(sourceSettings); break;
            case TextSourceType.ElapsedTime: content = ResolveElapsedTime(sourceSettings); break;
            case TextSourceType.ProgressPercent: content = ResolveProgressPercent(sourceSettings); break;
            case TextSourceType.CurrentTime: content = ResolveCurrentTime(sourceSettings); break;
            case TextSourceType.NextSegment: content = ResolveNextSegment(sourceSettings); break;
            case TextSourceType.CurrentDate: content = ResolveCurrentDate(sourceSettings); break;
            case TextSourceType.CurrentDayOfWeek: content = ResolveCurrentDayOfWeek(sourceSettings); break;
            default: content = string.Empty; break;
        }

        if (string.IsNullOrEmpty(content))
            return string.Empty;

        var prefix = commonSettings.Prefix ?? "";
        var suffix = commonSettings.Suffix ?? "";
        return $"{prefix}{content}{suffix}";
    }

    private static string ResolveCustomText(TextSlotSourceSettings settings)
    {
        return settings.Text ?? string.Empty;
    }

    private string ResolveSegmentName(TextSlotSourceSettings settings)
    {
        var name = _scheduleManager.CurrentPlan?.CurrentSegment?.Name;
        return !string.IsNullOrEmpty(name) ? name : (settings.Fallback ?? string.Empty);
    }

    private string ResolveRemainingTime(TextSlotSourceSettings settings)
    {
        var segment = _scheduleManager.CurrentPlan?.CurrentSegment;
        if (segment == null) return "";

        var now = _timeService.GetCurrentTime();
        var remaining = segment.EndTime - now;
        if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

        var showSeconds = settings.ShowSeconds ?? true;
        return FormatDuration(remaining, showSeconds);
    }

    private string ResolveElapsedTime(TextSlotSourceSettings settings)
    {
        var segment = _scheduleManager.CurrentPlan?.CurrentSegment;
        if (segment == null) return "";

        var now = _timeService.GetCurrentTime();
        var elapsed = now - segment.StartTime;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

        var showSeconds = settings.ShowSeconds ?? true;
        return FormatDuration(elapsed, showSeconds);
    }

    private string ResolveProgressPercent(TextSlotSourceSettings settings)
    {
        var segment = _scheduleManager.CurrentPlan?.CurrentSegment;
        if (segment == null || segment.Duration == TimeSpan.Zero) return "";

        var now = _timeService.GetCurrentTime();
        var elapsed = now - segment.StartTime;
        var percent = Math.Clamp(elapsed.TotalMilliseconds / segment.Duration.TotalMilliseconds * 100, 0, 100);

        var decimalPlaces = Math.Clamp(settings.DecimalPlaces ?? 1, 0, 3);
        var formatStr = "F" + decimalPlaces;
        return $"{Math.Round(percent, decimalPlaces).ToString(formatStr)}%";
    }

    private string ResolveCurrentTime(TextSlotSourceSettings settings)
    {
        var format = !string.IsNullOrWhiteSpace(settings.Format) ? settings.Format : "HH:mm:ss";
        try
        {
            return _timeService.GetCurrentTime().ToString(format);
        }
        catch (FormatException)
        {
            return _timeService.GetCurrentTime().ToString("HH:mm:ss");
        }
    }

    private string ResolveCurrentDate(TextSlotSourceSettings settings)
    {
        var format = !string.IsNullOrWhiteSpace(settings.Format) ? settings.Format : "yyyy/MM/dd";
        try
        {
            return _timeService.GetCurrentTime().ToString(format);
        }
        catch (FormatException)
        {
            return _timeService.GetCurrentTime().ToString("yyyy/MM/dd");
        }
    }

    private string ResolveCurrentDayOfWeek(TextSlotSourceSettings settings)
    {
        var format = settings.Format;
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var dayOfWeek = _timeService.GetCurrentTime().DayOfWeek;

        if (string.Equals(format, "short", StringComparison.OrdinalIgnoreCase))
            return culture.DateTimeFormat.GetAbbreviatedDayName(dayOfWeek);

        return culture.DateTimeFormat.GetDayName(dayOfWeek);
    }

    private string ResolveNextSegment(TextSlotSourceSettings settings)
    {
        var plan = _scheduleManager.CurrentPlan;
        if (plan == null) return settings.Fallback ?? string.Empty;

        var now = _timeService.GetCurrentTime();
        var nextSegment = plan.TimeSegments
            .Where(s => s.StartTime > now)
            .OrderBy(s => s.StartTime)
            .FirstOrDefault();

        if (nextSegment == null)
            return settings.Fallback ?? string.Empty;

        var name = nextSegment.Name ?? "";
        var showTime = settings.ShowTime ?? false;
        if (showTime)
        {
            name += $" ({nextSegment.StartTime:HH:mm})";
        }

        return name;
    }

    private static string FormatDuration(TimeSpan duration, bool showSeconds = true)
    {
        if (duration <= TimeSpan.Zero)
            return showSeconds ? "0s" : "0m";

        var parts = new List<string>();
        bool hasHours = duration.TotalHours >= 1;

        if (hasHours)
            parts.Add($"{(int)duration.TotalHours}h");

        if (duration.Minutes > 0 || hasHours)
        {
            if (hasHours)
                parts.Add($"{duration.Minutes:D2}m");
            else
                parts.Add($"{duration.Minutes}m");
        }

        if (showSeconds)
        {
            if (hasHours || duration.Minutes > 0)
                parts.Add($"{duration.Seconds:D2}s");
            else
                parts.Add($"{duration.Seconds}s");
        }

        return string.Join(" ", parts);
    }
}