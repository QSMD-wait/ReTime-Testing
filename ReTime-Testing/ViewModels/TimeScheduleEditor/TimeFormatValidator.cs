using System.Text.RegularExpressions;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 时间格式验证器
/// </summary>
public static class TimeFormatValidator
{
    private static readonly Regex TimeFormatRegex =
        new(@"^(\d{1,2}):([0-5]?\d):([0-5]?\d)$", RegexOptions.Compiled);

    /// <summary>
    /// 验证时间格式是否为 HH:mm:ss
    /// </summary>
    public static bool IsValidFormat(string? timeString)
    {
        if (string.IsNullOrEmpty(timeString)) return false;
        return TimeFormatRegex.IsMatch(timeString);
    }
}