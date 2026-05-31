using System.Diagnostics;
using System.Threading;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 云端校准服务
/// 定期从NTP服务器获取时间进行校准，支持RTT补偿和微调/跳跃区分
/// </summary>
public class CloudCalibrationService : ICloudCalibrationService, IDisposable
{
    private readonly ITimeService _timeService;
    private readonly Timer? _calibrationTimer;
    private int _failureCount;
    private int _currentInterval;
    private DateTime _lastCalibrationTime;
    private ITimeProvider? _currentTimeProvider;
    private List<string> _ntpServers = new();
    private int _selectedNtpServerIndex = 0;
    private int _maxRetryCount;
    private double _backoffMultiplier;

    /// <summary>
    /// 微调阈值（秒）：偏差小于此值时使用微调校准，不触发 TimeJumped 事件
    /// </summary>
    private const int MinorCalibrationThresholdSeconds = 30;

    private const int DefaultCalibrationTimeout = 3;
    private const int DefaultMaxRetryCount = 3;
    private const double DefaultBackoffMultiplier = 2.0;

    /// <summary>
    /// 是否启用云端校准
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// 校准间隔（秒）
    /// </summary>
    public int CalibrationInterval { get; private set; }

    /// <summary>
    /// 校准超时（秒）
    /// </summary>
    public int CalibrationTimeout { get; private set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetryCount => _maxRetryCount;

    /// <summary>
    /// 退避乘数
    /// </summary>
    public double BackoffMultiplier => _backoffMultiplier;

    /// <summary>
    /// 触发校准的偏差阈值（秒）
    /// </summary>
    public int CalibrationTriggerThreshold { get; private set; }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 失败次数
    /// </summary>
    public int FailureCount => _failureCount;

    /// <summary>
    /// 当前校准间隔（秒）
    /// </summary>
    public int CurrentInterval => _currentInterval;

    /// <summary>
    /// 最后一次校准时间
    /// </summary>
    public DateTime LastCalibrationTime => _lastCalibrationTime;

    /// <summary>
    /// 当前使用的时间提供者
    /// </summary>
    public string CurrentProviderName => _currentTimeProvider?.Name ?? "未初始化";

    /// <summary>
    /// 上次校准的RTT（毫秒）
    /// </summary>
    public double LastRttMs { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="timeService">时间服务</param>
    public CloudCalibrationService(ITimeService timeService)
    {
        _timeService = timeService;
        _failureCount = 0;
        _currentInterval = 300;
        _maxRetryCount = DefaultMaxRetryCount;
        _backoffMultiplier = DefaultBackoffMultiplier;

        IsEnabled = true;
        CalibrationInterval = 300;
        CalibrationTimeout = DefaultCalibrationTimeout;
        CalibrationTriggerThreshold = 5;

        _calibrationTimer = new Timer(
            _ => OnTimerTick(),
            null,
            Timeout.Infinite,  // 不立即启动，等待 Start() 调用
            Timeout.Infinite
        );
    }

    /// <summary>
    /// 初始化NTP时间提供者
    /// </summary>
    private void InitializeTimeProvider()
    {
        var selectedServers = new List<string>();
        if (_selectedNtpServerIndex >= 0 && _selectedNtpServerIndex < _ntpServers.Count)
        {
            selectedServers.Add(_ntpServers[_selectedNtpServerIndex]);
        }
        else
        {
            selectedServers.AddRange(_ntpServers);
        }
        _currentTimeProvider = new NtpTimeProvider(selectedServers.ToArray());

        Logger.Info("CloudCalibrationService",
            $"NTP时间提供者已初始化: 提供者={_currentTimeProvider.Name}, 服务器={string.Join(", ", selectedServers)}");
    }

    /// <summary>
    /// 启动云端校准
    /// </summary>
    public void Start()
    {
        if (!IsEnabled)
        {
            Logger.Info("CloudCalibrationService", "云端校准已禁用，无法启动");
            return;
        }

        _calibrationTimer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_currentInterval));
        IsRunning = true;

        Logger.Info("CloudCalibrationService", $"云端校准已启动，间隔: {_currentInterval}秒");
    }

    /// <summary>
    /// 停止云端校准
    /// </summary>
    public void Stop()
    {
        _calibrationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        IsRunning = false;

        Logger.Info("CloudCalibrationService", "云端校准已停止");
    }

    /// <summary>
    /// 配置校准参数（高级）
    /// </summary>
    /// <param name="enabled">是否启用</param>
    /// <param name="interval">校准间隔（秒）</param>
    /// <param name="timeout">校准超时（秒）</param>
    /// <param name="maxRetryCount">最大重试次数</param>
    /// <param name="backoffMultiplier">退避乘数</param>
    /// <param name="triggerThreshold">触发校准的偏差阈值（秒）</param>
    public void Configure(
        bool enabled,
        int interval,
        int timeout,
        int maxRetryCount,
        double backoffMultiplier,
        int triggerThreshold)
    {
        IsEnabled = enabled;
        CalibrationInterval = interval;
        CalibrationTimeout = timeout;
        _maxRetryCount = maxRetryCount;
        _backoffMultiplier = backoffMultiplier;
        CalibrationTriggerThreshold = triggerThreshold;

        _currentInterval = interval;
        _failureCount = 0;

        Logger.Info("CloudCalibrationService",
            $"配置已更新: Enabled={enabled}, Interval={interval}s, Timeout={timeout}s, MaxRetryCount={maxRetryCount}, BackoffMultiplier={backoffMultiplier}, TriggerThreshold={triggerThreshold}s");

        if (IsRunning && enabled)
        {
            _calibrationTimer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_currentInterval));
        }
    }

    /// <summary>
    /// 配置校准参数（简化版）
    /// </summary>
    /// <param name="enabled">是否启用</param>
    /// <param name="interval">校准间隔（秒）</param>
    /// <param name="triggerThreshold">触发校准的偏差阈值（秒）</param>
    public void Configure(bool enabled, int interval = 300, int triggerThreshold = 5)
    {
        IsEnabled = enabled;
        CalibrationInterval = interval;
        CalibrationTriggerThreshold = triggerThreshold;

        _currentInterval = interval;
        _failureCount = 0;

        Logger.Info("CloudCalibrationService",
            $"配置已更新: Enabled={enabled}, Interval={interval}s, TriggerThreshold={triggerThreshold}s");

        if (IsRunning && enabled)
        {
            _calibrationTimer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_currentInterval));
        }
    }

    /// <summary>
    /// 配置NTP服务器
    /// </summary>
    /// <param name="ntpServers">NTP服务器列表</param>
    /// <param name="selectedNtpServerIndex">选中的NTP服务器索引</param>
    public void ConfigureNtpServers(List<string>? ntpServers = null, int selectedNtpServerIndex = 0)
    {
        _ntpServers = ntpServers ?? new List<string> { "ntp.aliyun.com", "ntp.ntsc.ac.cn", "time.windows.com" };
        _selectedNtpServerIndex = selectedNtpServerIndex;

        InitializeTimeProvider();

        Logger.Info("CloudCalibrationService",
            $"NTP服务器已配置: 服务器={string.Join(", ", _ntpServers)}, 选中索引={selectedNtpServerIndex}");
    }

    /// <summary>
    /// 手动触发校准
    /// </summary>
    public async Task<bool> CalibrateAsync()
    {
        return await PerformCalibration(TimeJumpReason.ManualCalibration);
    }

    /// <summary>
    /// 校准定时器回调
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
        if (!IsEnabled || !IsRunning)
        {
            return false;
        }

        try
        {
            var result = await GetCloudTimeAsync();

            if (result != null)
            {
                // 使用RTT补偿后的校准时间
                var calibratedTime = result.CalibratedTime;
                // 转换为北京时间
                var beijingTime = calibratedTime.AddHours(8);

                var localTime = _timeService.GetCurrentTime();
                var offset = (beijingTime - localTime).Duration();

                LastRttMs = result.RoundTripTime.TotalMilliseconds;

                if (offset.TotalSeconds > CalibrationTriggerThreshold)
                {
                    Logger.Info("CloudCalibrationService",
                        $"校准时间: 本地={localTime:HH:mm:ss.fff}, 云端(已补偿RTT)={beijingTime:HH:mm:ss.fff}, 偏差={offset.TotalSeconds:F2}秒, RTT={result.RoundTripTime.TotalMilliseconds:F1}ms");

                    // 区分微调校准和跳跃校准
                    if (offset.TotalSeconds <= MinorCalibrationThresholdSeconds)
                    {
                        // 微调校准：偏差较小，不触发 TimeJumped 事件
                        _timeService.CalibrateMinor(beijingTime);
                        Logger.Info("CloudCalibrationService",
                            $"微调校准: 偏差={offset.TotalSeconds:F2}秒 (阈值<={MinorCalibrationThresholdSeconds}秒)");
                    }
                    else
                    {
                        // 跳跃校准：偏差较大，触发 TimeJumped 事件
                        _timeService.Calibrate(beijingTime, reason, TimeJumpSeverity.Major);
                        Logger.Info("CloudCalibrationService",
                            $"跳跃校准: 偏差={offset.TotalSeconds:F2}秒 (阈值>{MinorCalibrationThresholdSeconds}秒)");
                    }

                    _lastCalibrationTime = DateTime.Now;
                    _failureCount = 0;
                    _currentInterval = CalibrationInterval;

                    _calibrationTimer?.Change(TimeSpan.FromSeconds(_currentInterval), TimeSpan.FromSeconds(_currentInterval));

                    return true;
                }
                else
                {
                    Logger.Info("CloudCalibrationService",
                        $"偏差在阈值内: {offset.TotalSeconds:F2}秒 (阈值<={CalibrationTriggerThreshold}秒)，无需校准, RTT={result.RoundTripTime.TotalMilliseconds:F1}ms");
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _failureCount++;

            Logger.Warn("CloudCalibrationService",
                $"云端校准失败: {ex.Message} (失败次数: {_failureCount}/{MaxRetryCount})");

            var newInterval = (int)(_currentInterval * BackoffMultiplier);
            newInterval = Math.Min(newInterval, 1800);
            _currentInterval = newInterval;

            _calibrationTimer?.Change(TimeSpan.FromSeconds(_currentInterval), TimeSpan.FromSeconds(_currentInterval));

            Logger.Info("CloudCalibrationService",
                $"校准间隔已调整为: {_currentInterval}秒");

            if (_failureCount >= MaxRetryCount)
            {
                Logger.Error("CloudCalibrationService",
                    $"云端校准连续失败 {_failureCount} 次，停止校准");

                Stop();
            }

            return false;
        }
    }

    /// <summary>
    /// 获取云端时间（含RTT信息）
    /// </summary>
    /// <returns>时间提供结果（含RTT），失败返回null</returns>
    private async Task<TimeProviderResult?> GetCloudTimeAsync()
    {
        if (_currentTimeProvider == null)
        {
            Logger.Error("CloudCalibrationService", "时间提供者未初始化");
            return null;
        }

        try
        {
            var result = await _currentTimeProvider.GetTimeAsync(TimeSpan.FromSeconds(CalibrationTimeout));

            if (result != null)
            {
                Logger.Info("CloudCalibrationService",
                    $"获取云端时间成功: UTC={result.UtcTime:yyyy-MM-dd HH:mm:ss.fff}, RTT={result.RoundTripTime.TotalMilliseconds:F1}ms");
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error("CloudCalibrationService", $"获取云端时间失败: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// 重置失败计数器和间隔
    /// </summary>
    public void Reset()
    {
        _failureCount = 0;
        _currentInterval = CalibrationInterval;

        Logger.Info("CloudCalibrationService", "已重置失败计数器和间隔");

        if (IsRunning && IsEnabled)
        {
            _calibrationTimer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_currentInterval));
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _calibrationTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}