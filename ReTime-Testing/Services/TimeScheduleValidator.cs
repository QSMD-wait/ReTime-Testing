using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 时间计划配置验证器
/// </summary>
public class TimeScheduleValidator
{
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// 验证时间计划配置
    /// </summary>
    /// <param name="schedule">时间计划</param>
    /// <returns>验证结果</returns>
    public ValidationResult Validate(TimeSchedule schedule)
    {
        var result = new ValidationResult { IsValid = true };

        // 1. 验证时间段
        ValidateSchedules(schedule.Schedules, result);

        // 2. 验证时间点
        ValidateTimePoints(schedule.TimePoints, schedule.Schedules, result);

        // 3. 如果有错误，标记为无效
        if (result.Errors.Count > 0)
        {
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// 验证时间段配置
    /// </summary>
    private void ValidateSchedules(List<TimeScheduleItem> schedules, ValidationResult result)
    {
        var scheduleIds = new HashSet<string>();
        var scheduleTimes = new List<(string Id, DateTime StartTime, DateTime EndTime)>();

        for (int i = 0; i < schedules.Count; i++)
        {
            var item = schedules[i];

            // 验证 ID 唯一性
            if (string.IsNullOrEmpty(item.Id))
            {
                result.Errors.Add($"时间段索引 {i} 缺少 ID");
            }
            else if (scheduleIds.Contains(item.Id))
            {
                result.Errors.Add($"时间段 ID 重复: {item.Id}");
            }
            else
            {
                scheduleIds.Add(item.Id);
            }

            // 验证时间格式和有效性
            DateTime startTime, endTime;
            bool startTimeValid = TryParseTime(item.StartTime, out startTime);
            bool endTimeValid = TryParseTime(item.EndTime, out endTime);

            if (!startTimeValid)
            {
                result.Errors.Add($"时间段 {item.Id} 开始时间格式无效: {item.StartTime}");
                continue;
            }

            if (!endTimeValid)
            {
                result.Errors.Add($"时间段 {item.Id} 结束时间格式无效: {item.EndTime}");
                continue;
            }

            // 验证行为配置
            ValidateBehavior(item.Id, item.Behavior, result);

            // 处理跨午夜的情况
            if (endTime < startTime)
            {
                endTime = endTime.AddDays(1);
            }

            // 验证开始时间小于结束时间
            if (startTime >= endTime)
            {
                result.Errors.Add($"时间段 {item.Id} 开始时间不能大于等于结束时间");
                continue;
            }

            scheduleTimes.Add((item.Id, startTime, endTime));
        }

        // 验证时间段不重叠
        for (int i = 0; i < scheduleTimes.Count; i++)
        {
            for (int j = i + 1; j < scheduleTimes.Count; j++)
            {
                var a = scheduleTimes[i];
                var b = scheduleTimes[j];

                if (IsOverlap(a.StartTime, a.EndTime, b.StartTime, b.EndTime))
                {
                    result.Errors.Add($"时间段 {a.Id} 与 {b.Id} 时间重叠");
                }
            }
        }
    }

    /// <summary>
    /// 验证时间点配置
    /// </summary>
    private void ValidateTimePoints(List<CustomTimePoint> timePoints, List<TimeScheduleItem> schedules, ValidationResult result)
    {
        var timePointIds = new HashSet<string>();

        // 构建时间段列表用于检查位置
        var scheduleTimes = schedules
            .Where(s => TryParseTime(s.StartTime, out _) && TryParseTime(s.EndTime, out _))
            .Select(s =>
            {
                var startTime = ParseTime(s.StartTime);
                var endTime = ParseTime(s.EndTime);
                if (endTime < startTime) endTime = endTime.AddDays(1);
                return (s.Id, StartTime: startTime, EndTime: endTime);
            })
            .ToList();

        for (int i = 0; i < timePoints.Count; i++)
        {
            var tp = timePoints[i];

            // 验证 ID 唯一性
            if (string.IsNullOrEmpty(tp.Id))
            {
                result.Errors.Add($"时间点索引 {i} 缺少 ID");
            }
            else if (timePointIds.Contains(tp.Id))
            {
                result.Errors.Add($"时间点 ID 重复: {tp.Id}");
            }
            else
            {
                timePointIds.Add(tp.Id);
            }

            // 验证 toState 不为 Progress
            if (tp.ToState == ProgressStateType.Progress)
            {
                result.Errors.Add($"时间点 {tp.Id} 不能设置 Progress 状态，时间段固定为 Progress");
            }

            // 验证时间格式
            if (!TryParseTime(tp.Time, out var tpTime))
            {
                result.Errors.Add($"时间点 {tp.Id} 时间格式无效: {tp.Time}");
                continue;
            }

            // 验证时间点不在时间段内部
            foreach (var schedule in scheduleTimes)
            {
                if (tpTime > schedule.StartTime && tpTime < schedule.EndTime)
                {
                    result.Errors.Add($"时间点 {tp.Id} ({tp.Time}) 位于时间段 {schedule.Id} 内部，不允许");
                }
            }

            // 检查时间点是否等于时间段开始时间（警告，会被忽略）
            foreach (var schedule in scheduleTimes)
            {
                if (tpTime == schedule.StartTime)
                {
                    result.Warnings.Add($"时间点 {tp.Id} ({tp.Time}) 与时间段 {schedule.Id} 开始时间相同，将被忽略");
                }
            }
        }
    }

    /// <summary>
    /// 验证行为配置
    /// </summary>
    private void ValidateBehavior(string itemId, ScheduleBehaviorData? behavior, ValidationResult result)
    {
        if (behavior == null) return;

        if (behavior.PollingIntervalMs.HasValue)
        {
            var interval = behavior.PollingIntervalMs.Value;
            if (interval < 100 || interval > 10000)
            {
                result.Errors.Add($"时间段 {itemId} 的 pollingIntervalMs 超出合法范围 (100–10000): {interval}");
            }
        }
    }

    /// <summary>
    /// 尝试解析时间字符串
    /// </summary>
    private bool TryParseTime(string timeString, out DateTime result)
    {
        result = DateTime.MinValue;
        if (string.IsNullOrEmpty(timeString)) return false;

        try
        {
            var time = TimeSpan.Parse(timeString);
            result = DateTime.Today.Add(time);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 解析时间字符串
    /// </summary>
    private DateTime ParseTime(string timeString)
    {
        var time = TimeSpan.Parse(timeString);
        return DateTime.Today.Add(time);
    }

    /// <summary>
    /// 检查两个时间段是否重叠
    /// </summary>
    private bool IsOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
        return start1 < end2 && start2 < end1;
    }
}
