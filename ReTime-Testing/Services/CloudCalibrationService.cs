using System.Threading;

namespace ReTime_Testing.Services;

/// <summary>
/// 云端校准服务
/// 定期从云端获取时间进行校准
/// </summary>
public class CloudCalibrationService
{
    private readonly ITimeService _timeService;
    private readonly Timer? _calibrationTimer;
    private int _failureCount;
    private int _currentInterval;

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
    public int MaxRetryCount { get; private set; }

    /// <summary>
    /// 退避乘数
    /// </summary>
    public double BackoffMultiplier { get; private set; }

    /// <summary>
    /// 触发校准的偏差阈值（秒）
    /// </summary>
    public int CalibrationTriggerThreshold { get; private set; }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="timeService">时间服务</param>
    public CloudCalibrationService(ITimeService timeService)
    {
        _timeService = timeService;
        _failureCount = 0;
        _currentInterval = 300; // 默认 5 分钟

        // 默认配置
        IsEnabled = true;
        CalibrationInterval = 300; // 5 分钟
        CalibrationTimeout = 3; // 3 秒
        MaxRetryCount = 5;
        BackoffMultiplier = 2.0;
        CalibrationTriggerThreshold = 5; // 5 秒

        // 初始化校准定时器
        _calibrationTimer = new Timer(
            _ => OnTimerTick(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_currentInterval)
        );
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
    /// 配置云端校准参数
    /// </summary>
    /// <param name="enabled">是否启用</param>
    /// <param name="interval">校准间隔（秒）</param>
    /// <param name="timeout">校准超时（秒）</param>
    /// <param name="maxRetryCount">最大重试次数</param>
    /// <param name="backoffMultiplier">退避乘数</param>
    /// <param name="triggerThreshold">触发校准的偏差阈值（秒）</param>
    public void Configure(
        bool enabled,
        int interval = 300,
        int timeout = 3,
        int maxRetryCount = 5,
        double backoffMultiplier = 2.0,
        int triggerThreshold = 5)
    {
        IsEnabled = enabled;
        CalibrationInterval = interval;
        CalibrationTimeout = timeout;
        MaxRetryCount = maxRetryCount;
        BackoffMultiplier = backoffMultiplier;
        CalibrationTriggerThreshold = triggerThreshold;

        _currentInterval = interval;
        _failureCount = 0;

        Logger.Info("CloudCalibrationService",
            $"配置已更新: Enabled={enabled}, Interval={interval}s, Timeout={timeout}s, MaxRetry={maxRetryCount}");

        // 如果正在运行，重新启动定时器
        if (IsRunning && enabled)
        {
            _calibrationTimer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_currentInterval));
        }
    }

    /// <summary>
    /// 手动触发校准
    /// </summary>
    public async Task<bool> CalibrateAsync()
    {
        return await PerformCalibration();
    }

    /// <summary>
    /// 校准定时器回调
    /// </summary>
    private async void OnTimerTick()
    {
        await PerformCalibration();
    }

    /// <summary>
    /// 执行校准
    /// </summary>
    /// <returns>是否校准成功</returns>
    private async Task<bool> PerformCalibration()
    {
        if (!IsEnabled || !IsRunning)
        {
            return false;
        }

        try
        {
            // 尝试获取云端时间
            var cloudTime = await GetCloudTimeAsync();

            if (cloudTime.HasValue)
            {
                // 计算偏差
                var localTime = _timeService.GetCurrentTime();
                var offset = (cloudTime.Value - localTime).Duration();

                // 如果偏差超过阈值，则校准
                if (offset.TotalSeconds > CalibrationTriggerThreshold)
                {
                    Logger.Info("CloudCalibrationService",
                        $"校准时间: 本地={localTime:HH:mm:ss}, 云端={cloudTime.Value:HH:mm:ss}, 偏差={offset.TotalSeconds:F2}秒");

                    _timeService.Calibrate(cloudTime.Value);

                    // 重置失败计数器和间隔
                    _failureCount = 0;
                    _currentInterval = CalibrationInterval;

                    // 更新定时器间隔
                    _calibrationTimer?.Change(TimeSpan.FromSeconds(_currentInterval), TimeSpan.FromSeconds(_currentInterval));

                    return true;
                }
                else
                {
                    Logger.Info("CloudCalibrationService",
                        $"偏差在阈值内: {offset.TotalSeconds:F2}秒，无需校准");
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _failureCount++;

            Logger.Warn("CloudCalibrationService",
                $"云端校准失败: {ex.Message} (失败次数: {_failureCount}/{MaxRetryCount})");

            // 动态调整间隔
            var newInterval = (int)(_currentInterval * BackoffMultiplier);
            newInterval = Math.Min(newInterval, 1800); // 最大 30 分钟
            _currentInterval = newInterval;

            // 更新定时器间隔
            _calibrationTimer?.Change(TimeSpan.FromSeconds(_currentInterval), TimeSpan.FromSeconds(_currentInterval));

            Logger.Info("CloudCalibrationService",
                $"校准间隔已调整为: {_currentInterval}秒");

            // 连续失败超过阈值，停止校准
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
    /// 获取云端时间（预留）
    /// </summary>
    /// <returns>云端时间</returns>
    private async Task<DateTime?> GetCloudTimeAsync()
    {
        try
        {
            // TODO: 实现实际的云端时间获取
            // 这里可以调用 NTP 服务器或其他时间服务

            // 模拟网络延迟
            await Task.Delay(100);

            // 目前返回当前系统时间（模拟云端时间）
            return DateTime.Now;
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

        // 如果正在运行，重新启动定时器
        if (IsRunning && IsEnabled)
        {
            _calibrationTimer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(_currentInterval));
        }
    }

    /// <summary>
    /// 析构函数，释放资源
    /// </summary>
    ~CloudCalibrationService()
    {
        _calibrationTimer?.Dispose();
    }
}