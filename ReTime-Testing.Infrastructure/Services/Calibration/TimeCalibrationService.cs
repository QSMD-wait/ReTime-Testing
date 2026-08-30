using Microsoft.Win32;
using ReTime_Testing.Models;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.Services;

/// <summary>
/// 时间校准服务
/// 统一管理校准源选择（系统/云端）、校准策略（微调/跳跃）、定时调度、休眠恢复
/// 连接单调时钟与实际时间源
/// </summary>
public class TimeCalibrationService : ITimeCalibrationService, IDisposable
{
        private readonly ILogger<TimeCalibrationService> _logger;
    private readonly ITimeService _timeService;
    private readonly ICloudCalibrationService _cloudCalibrationService;
    private readonly Timer _calibrationTimer;
    private readonly object _lock = new();

    // 运行时状态
    private int _failureCount;
    private int _currentInterval;
    private DateTime _lastCalibrationTime;
    private bool _isRunning;
    private bool _disposed;

    // 配置缓存
    private CalibrationConfig _config = new();

    /// <summary>
    /// 是否启用校准
    /// </summary>
    public bool IsEnabled => _config.Enabled;

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// 当前校准源类型
    /// </summary>
    public CalibrationSource CurrentSource => _config.Source;

    /// <summary>
    /// 校准失败次数
    /// </summary>
    public int FailureCount => _failureCount;

    /// <summary>
    /// 上次校准时间
    /// </summary>
    public DateTime LastCalibrationTime => _lastCalibrationTime;

    /// <summary>
    /// 当前校准间隔（秒）
    /// </summary>
    public int CurrentInterval => _currentInterval;

    /// <summary>
    /// 上次校准的RTT（毫秒），仅云端源有效
    /// </summary>
    public double LastRttMs => _cloudCalibrationService.LastRttMs;

    /// <summary>
    /// 当前使用的时间提供者名称
    /// </summary>
    public string CurrentProviderName => _config.Source == CalibrationSource.Cloud
        ? _cloudCalibrationService.CurrentProviderName
        : "系统时间";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="timeService">单调时钟服务</param>
    /// <param name="cloudCalibrationService">云端校准数据源</param>
    public TimeCalibrationService(ITimeService timeService, ICloudCalibrationService cloudCalibrationService, ILogger<TimeCalibrationService> logger)
    {
        _logger = logger;
        _timeService = timeService;
        _cloudCalibrationService = cloudCalibrationService;
        _currentInterval = 300;
        _lastCalibrationTime = DateTime.MinValue;

        _calibrationTimer = new Timer(
            _ => OnTimerTick(),
            null,
            Timeout.Infinite,
            Timeout.Infinite
        );

        // 监听系统电源事件（休眠恢复）
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    /// <summary>
    /// 启动校准服务
    /// </summary>
    public void Start()
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("时间校准已禁用，无法启动");
            return;
        }

        _calibrationTimer.Change(TimeSpan.FromSeconds(_currentInterval), TimeSpan.FromSeconds(_currentInterval));
        _isRunning = true;

        _logger.LogInformation("时间校准已启动: 校准源={Source}, 间隔={Interval}秒", _config.Source, _currentInterval);
    }

    /// <summary>
    /// 停止校准服务
    /// </summary>
    public void Stop()
    {
        _calibrationTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _isRunning = false;

        _logger.LogInformation("时间校准已停止");
    }

    /// <summary>
    /// 应用校准配置
    /// </summary>
    /// <param name="config">校准配置</param>
    public void ApplyConfig(CalibrationConfig config)
    {
        // 运行时值校验/钳位（防御性编程：不信任外部输入）
        config.IntervalSeconds = Math.Clamp(config.IntervalSeconds, 1, 86400);
        config.TriggerSeconds = Math.Max(1, config.TriggerSeconds);
        config.MinorThresholdSeconds = Math.Max(1, config.MinorThresholdSeconds);
        config.ResumeThresholdSeconds = Math.Max(60, config.ResumeThresholdSeconds);
        if (config.TriggerSeconds > config.MinorThresholdSeconds)
            config.MinorThresholdSeconds = config.TriggerSeconds;
        config.MaxRetryCount = Math.Max(0, config.MaxRetryCount);
        config.BackoffMultiplier = Math.Max(1.0, config.BackoffMultiplier);
        config.Cloud ??= new CloudCalibrationConfig();
        config.Cloud.TimeoutSeconds = Math.Max(1, config.Cloud.TimeoutSeconds);
        if (string.IsNullOrWhiteSpace(config.Cloud.SelectedServerAddress))
            config.Cloud.SelectedServerAddress = new CloudCalibrationConfig().SelectedServerAddress;

        lock (_lock)
        {
            _config = config;
            _currentInterval = config.IntervalSeconds;
            _failureCount = 0;
        }

        // 如果是云端源，配置NTP服务器
        if (config.Source == CalibrationSource.Cloud)
        {
            var ntpServers = NtpServerDefaults.Servers.ToList();
            var selectedIndex = NtpServerDefaults.IndexOf(config.Cloud.SelectedServerAddress);

            _cloudCalibrationService.ConfigureNtpServers(ntpServers, selectedIndex);
        }

        _logger.LogInformation("配置已应用: Enabled={Enabled}, Source={Source}, Interval={Interval}s, TriggerThreshold={Trigger}s, MinorThreshold={Minor}s",
            config.Enabled, config.Source, config.IntervalSeconds, config.TriggerSeconds, config.MinorThresholdSeconds);

        // 如果正在运行且启用，重启定时器以应用新间隔
        if (_isRunning && config.Enabled)
        {
            _calibrationTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_currentInterval));
        }
    }

    /// <summary>
    /// 手动触发校准
    /// </summary>
    /// <returns>是否校准成功</returns>
    public async Task<bool> CalibrateAsync()
    {
        return await PerformCalibration(TimeJumpReason.ManualCalibration);
    }

    /// <summary>
    /// 重置校准状态
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _currentInterval = _config.IntervalSeconds;
        }

        _logger.LogInformation("已重置失败计数器和间隔");

        if (_isRunning && _config.Enabled)
        {
            _calibrationTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_currentInterval));
        }
    }

    /// <summary>
    /// 定时器回调
    /// </summary>
    private async void OnTimerTick()
    {
        await PerformCalibration(TimeJumpReason.CloudCalibration);
    }

    /// <summary>
    /// 执行校准
    /// </summary>
    /// <param name="reason">校准原因</param>
    /// <returns>是否校准成功</returns>
    private async Task<bool> PerformCalibration(TimeJumpReason reason)
    {
        if (!_config.Enabled || !_isRunning)
        {
            return false;
        }

        try
        {
            DateTime? calibratedTime = null;

            switch (_config.Source)
            {
                case CalibrationSource.System:
                    calibratedTime = await GetSystemTimeAsync();
                    break;

                case CalibrationSource.Cloud:
                    calibratedTime = await GetCloudTimeAsync();
                    break;
            }

            if (calibratedTime.HasValue)
            {
                var localTime = _timeService.GetCurrentTime() - _timeService.CurrentUserOffset;
                var offset = calibratedTime.Value - localTime;
                var absOffset = offset.Duration();

                if (absOffset.TotalSeconds > _config.TriggerSeconds)
                {
                    _logger.LogDebug("校准时间: 本地={LocalTime:HH:mm:ss.fff}, 校准源={CalibratedTime:HH:mm:ss.fff}, 偏差={Offset:F2}秒, 源={Source}",
                        localTime, calibratedTime.Value, absOffset.TotalSeconds, _config.Source);

                    // 区分微调校准和跳跃校准
                    if (absOffset.TotalSeconds <= _config.MinorThresholdSeconds)
                    {
                        // 微调校准：偏差较小，仅应用偏移量，不触发 TimeJumped 事件
                        _timeService.ApplyOffset(offset);
                        _logger.LogDebug("微调校准: 偏差={Offset:F2}秒 (阈值<={Threshold}秒)", absOffset.TotalSeconds, _config.MinorThresholdSeconds);
                    }
                    else
                    {
                        // 跳跃校准：偏差较大，硬跳并触发 TimeJumped 事件
                        _timeService.Calibrate(calibratedTime.Value, reason, TimeJumpSeverity.Major);
                        _logger.LogDebug("跳跃校准: 偏差={Offset:F2}秒 (阈值>{Threshold}秒)", absOffset.TotalSeconds, _config.MinorThresholdSeconds);
                    }

                    _lastCalibrationTime = _timeService.GetCurrentTime();
                    _failureCount = 0;
                    _currentInterval = _config.IntervalSeconds;

                    _calibrationTimer.Change(
                        TimeSpan.FromSeconds(_currentInterval),
                        TimeSpan.FromSeconds(_currentInterval));

                    return true;
                }
                else
                {
                    _logger.LogDebug("偏差在阈值内: {Offset:F2}秒 (阈值<={Trigger}秒)，无需校准", absOffset.TotalSeconds, _config.TriggerSeconds);

                    _lastCalibrationTime = _timeService.GetCurrentTime();
                    _failureCount = 0;
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _failureCount++;

            _logger.LogWarning("时间校准失败: {Message} (失败次数: {FailureCount}/{MaxRetryCount})", ex.Message, _failureCount, _config.MaxRetryCount);

            // 退避策略：延长下次校准间隔
            var newInterval = (int)(_currentInterval * _config.BackoffMultiplier);
            newInterval = Math.Min(newInterval, 1800);
            _currentInterval = newInterval;

            _calibrationTimer.Change(
                TimeSpan.FromSeconds(_currentInterval),
                TimeSpan.FromSeconds(_currentInterval));

            _logger.LogInformation("校准间隔已调整为: {Interval}秒", _currentInterval);

            // 连续失败达上限，停止校准
            if (_failureCount >= _config.MaxRetryCount)
            {
                _logger.LogError("时间校准连续失败 {FailureCount} 次，停止校准", _failureCount);
                Stop();
            }

            return false;
        }
    }

    /// <summary>
    /// 从系统时间源获取校准时间
    /// </summary>
    private Task<DateTime?> GetSystemTimeAsync()
    {
        // 系统时间源直接返回当前系统本地时间
        return Task.FromResult<DateTime?>(DateTime.Now);
    }

    /// <summary>
    /// 从云端NTP源获取校准时间
    /// </summary>
    private async Task<DateTime?> GetCloudTimeAsync()
    {
        var timeout = TimeSpan.FromSeconds(_config.Cloud.TimeoutSeconds);
        var result = await _cloudCalibrationService.GetCloudTimeAsync(timeout);

        if (result == null)
        {
            return null;
        }

        // 使用RTT补偿后的校准时间，转换为本地时间
        var localTimeZone = TimeZoneInfo.Local;
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(result.CalibratedTime, localTimeZone);

        return localTime;
    }

    /// <summary>
    /// 系统电源模式变化事件处理
    /// </summary>
    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume && _config.Enabled && _isRunning)
        {
            // 系统唤醒后，检查是否需要重新校准
            var now = DateTime.Now;
            TimeSpan sleepDuration;

            lock (_lock)
            {
                sleepDuration = _lastCalibrationTime == DateTime.MinValue
                    ? TimeSpan.MaxValue
                    : now - _lastCalibrationTime;
            }

            // 如果休眠时间超过阈值，则立即触发校准
            var resumeThreshold = TimeSpan.FromSeconds(Math.Max(60, _config.ResumeThresholdSeconds));
            if (sleepDuration > resumeThreshold)
            {
                _logger.LogInformation("系统休眠恢复，休眠时长={Minutes:F1}分钟，触发重新校准", sleepDuration.TotalMinutes);

                // 使用系统校准原因
                _ = PerformCalibration(TimeJumpReason.SystemResume);
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _calibrationTimer.Dispose();

        GC.SuppressFinalize(this);
    }
}