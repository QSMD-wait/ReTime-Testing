using Microsoft.Win32;
using ReTime_Testing.Models;
using System.Diagnostics;

namespace ReTime_Testing.Services;

/// <summary>
/// 绝对时间服务
/// 使用 Stopwatch 从基准时间单调递增计时，隔离系统时间篡改
/// </summary>
public class AbsoluteTimeService : ITimeService
{
    private readonly object _lock = new();
    private DateTime _baseTime;
    private long _baseTick;
    private DateTime _lastCalibrationTime;
    private bool _isCloudSynchronized;

    /// <summary>
    /// 时间跳跃事件
    /// </summary>
    public event EventHandler<TimeJumpedEventArgs>? TimeJumped;

    /// <summary>
    /// 是否云端同步
    /// </summary>
    public bool IsCloudSynchronized => _isCloudSynchronized;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AbsoluteTimeService()
    {
        // 初始化为系统时间
        _baseTime = DateTime.Now;
        _baseTick = Stopwatch.GetTimestamp();
        _lastCalibrationTime = _baseTime;
        _isCloudSynchronized = false;

        // 监听系统电源事件
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
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
            return _baseTime + elapsed;
        }
    }

    /// <summary>
    /// 从云端校准时间（硬跳，触发 TimeJumped 事件）
    /// </summary>
    /// <param name="cloudTime">云端时间</param>
    /// <param name="reason">跳跃原因</param>
    /// <param name="severity">跳跃严重程度</param>
    public void Calibrate(DateTime cloudTime,
        TimeJumpReason reason = TimeJumpReason.CloudCalibration,
        TimeJumpSeverity severity = TimeJumpSeverity.Major)
    {
        DateTime oldTime;
        DateTime newTime;

        lock (_lock)
        {
            oldTime = GetCurrentTime();

            // 重置基准
            _baseTime = cloudTime;
            _baseTick = Stopwatch.GetTimestamp();
            _lastCalibrationTime = cloudTime;
            _isCloudSynchronized = true;

            newTime = cloudTime;
        }

        // 锁外触发事件
        OnTimeJumped(new TimeJumpedEventArgs(oldTime, newTime, reason, severity));
    }

    /// <summary>
    /// 微调校准（仅调整偏移量，不触发 TimeJumped 事件）
    /// 适用于偏差较小的校准，避免不必要的状态重算
    /// </summary>
    /// <param name="cloudTime">云端时间</param>
    public void CalibrateMinor(DateTime cloudTime)
    {
        lock (_lock)
        {
            // 仅调整基准时间，不重置 Stopwatch 起点以保持单调性
            _baseTime = cloudTime;
            _baseTick = Stopwatch.GetTimestamp();
            _lastCalibrationTime = cloudTime;
            _isCloudSynchronized = true;
        }

        // 微调不触发 TimeJumped 事件
        Logger.Info("AbsoluteTimeService", $"微调校准: 新基准={cloudTime:HH:mm:ss.fff}");
    }

    /// <summary>
    /// 系统电源模式变化事件处理
    /// </summary>
    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            // 系统唤醒后，检查是否需要重新校准
            var now = DateTime.Now;
            TimeSpan sleepDuration;

            lock (_lock)
            {
                sleepDuration = now - _lastCalibrationTime;
            }

            // 如果休眠时间超过阈值（5分钟），则重新校准
            if (sleepDuration > TimeSpan.FromMinutes(5))
            {
                Calibrate(now, TimeJumpReason.SystemResume, TimeJumpSeverity.Major);
            }
        }
    }

    /// <summary>
    /// 触发时间跳跃事件
    /// </summary>
    protected virtual void OnTimeJumped(TimeJumpedEventArgs e)
    {
        TimeJumped?.Invoke(this, e);
    }

    /// <summary>
    /// 析构函数，取消事件订阅
    /// </summary>
    ~AbsoluteTimeService()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }
}