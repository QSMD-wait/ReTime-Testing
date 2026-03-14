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
    /// 从云端校准时间
    /// </summary>
    /// <param name="cloudTime">云端时间</param>
    public void Calibrate(DateTime cloudTime)
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
        OnTimeJumped(new TimeJumpedEventArgs(oldTime, newTime));
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
                Calibrate(now);
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