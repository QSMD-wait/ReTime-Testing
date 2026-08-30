using ReTime_Testing.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.Services;

/// <summary>
/// 绝对时间服务（单调时钟）
/// 使用 Stopwatch 从基准时间单调递增计时，隔离系统时间篡改
/// 仅负责计时和接受校准偏移，不负责校准策略和校准源选择
/// </summary>
public class AbsoluteTimeService : ITimeService, IDisposable
{
        private readonly ILogger<AbsoluteTimeService> _logger;
    private readonly object _lock = new();
    private DateTime _baseTime;
    private long _baseTick;
    private TimeSpan _userOffset;
    private bool _isCloudSynchronized;
    private bool _disposed;

    /// <summary>
    /// 时间跳跃事件
    /// </summary>
    public event EventHandler<TimeJumpedEventArgs>? TimeJumped;

    /// <summary>
    /// 是否云端同步
    /// </summary>
    public bool IsCloudSynchronized => _isCloudSynchronized;

    /// <summary>
    /// 当前用户时间偏移量
    /// </summary>
    public TimeSpan CurrentUserOffset => _userOffset;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AbsoluteTimeService(ILogger<AbsoluteTimeService> logger)
    {
        _logger = logger;
        _baseTime = DateTime.Now;
        _baseTick = Stopwatch.GetTimestamp();
        _isCloudSynchronized = false;
    }

    /// <summary>
    /// 获取当前绝对时间
    /// </summary>
    /// <returns>当前绝对时间</returns>
    public DateTime GetCurrentTime()
    {
        lock (_lock)
        {
            var elapsedTicks = Stopwatch.GetTimestamp() - _baseTick;
            var elapsed = TimeSpan.FromTicks(elapsedTicks);
            return _baseTime + elapsed + _userOffset;
        }
    }

    /// <summary>
    /// 校准时间（硬跳，重置基准并触发 TimeJumped 事件）
    /// 适用于偏差较大的校准，需要重新计算调度状态
    /// </summary>
    /// <param name="calibratedTime">校准后的时间</param>
    /// <param name="reason">跳跃原因</param>
    /// <param name="severity">跳跃严重程度</param>
    public void Calibrate(DateTime calibratedTime,
        TimeJumpReason reason = TimeJumpReason.CloudCalibration,
        TimeJumpSeverity severity = TimeJumpSeverity.Major)
    {
        DateTime oldTime;
        DateTime newTime;

        lock (_lock)
        {
            oldTime = GetCurrentTime();

            // 重置基准时间和 Stopwatch 起点
            _baseTime = calibratedTime;
            _baseTick = Stopwatch.GetTimestamp();
            _isCloudSynchronized = true;

            newTime = calibratedTime;
        }

        // 锁外触发事件
        OnTimeJumped(new TimeJumpedEventArgs(oldTime, newTime, reason, severity));
    }

    /// <summary>
    /// 应用偏移量（微调，不重置 Stopwatch 起点以保持单调性，不触发 TimeJumped 事件）
    /// 适用于偏差较小的校准，仅调整基准时间偏移
    /// </summary>
    /// <param name="offset">时间偏移量</param>
    public void ApplyOffset(TimeSpan offset)
    {
        lock (_lock)
        {
            // 仅调整基准时间，不重置 Stopwatch 起点以保持单调性
            _baseTime = _baseTime + offset;
            _isCloudSynchronized = true;
        }

        _logger.LogDebug("微调偏移: 偏移量={Offset:F2}秒", offset.TotalSeconds);
    }

    /// <summary>
    /// 应用用户时间偏移量（持久化偏移，与校准偏移独立）
    /// </summary>
    /// <param name="offset">用户偏移量</param>
    public void ApplyUserOffset(TimeSpan offset)
    {
        var clampedSeconds = Math.Clamp(offset.TotalSeconds, -86400, 86400);
        var clampedOffset = TimeSpan.FromSeconds(clampedSeconds);

        lock (_lock)
        {
            _userOffset = clampedOffset;
        }

        _logger.LogDebug("用户偏移: 偏移量={Offset:F2}秒", clampedOffset.TotalSeconds);
    }

    /// <summary>
    /// 触发时间跳跃事件
    /// </summary>
    protected virtual void OnTimeJumped(TimeJumpedEventArgs e)
    {
        TimeJumped?.Invoke(this, e);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}