using System.Net.Http;
using System.Threading;

namespace ReTime_Testing.Services;

/// <summary>
/// 云端校准服务
/// 定期从云端获取时间进行校准
/// </summary>
public class CloudCalibrationService : ICloudCalibrationService
{
    private readonly ITimeService _timeService;
    private readonly HttpClient _httpClient;
    private readonly Timer? _calibrationTimer;
    private int _failureCount;
    private int _currentInterval;
    private DateTime _lastCalibrationTime;
    private ITimeProvider? _currentTimeProvider;
    private string _timeSourceType = "http";
    private List<string> _ntpServers = new();
    private List<string> _httpServers = new();
    private int _selectedNtpServerIndex = 0;
    private int _selectedHttpServerIndex = 0;

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
    public int MaxRetryCount => DefaultMaxRetryCount;

    /// <summary>
    /// 退避乘数
    /// </summary>
    public double BackoffMultiplier => DefaultBackoffMultiplier;

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
    /// 当前时间源类型
    /// </summary>
    public string TimeSourceType => _timeSourceType;

    /// <summary>
    /// 当前使用的时间提供者
    /// </summary>
    public string CurrentProviderName => _currentTimeProvider?.Name ?? "未初始化";

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="timeService">时间服务</param>
    public CloudCalibrationService(ITimeService timeService)
    {
        _timeService = timeService;
        _httpClient = new HttpClient();
        _failureCount = 0;
        _currentInterval = 300;

        IsEnabled = true;
        CalibrationInterval = 300;
        CalibrationTimeout = DefaultCalibrationTimeout;
        CalibrationTriggerThreshold = 5;

        _calibrationTimer = new Timer(
            _ => OnTimerTick(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_currentInterval)
        );

        InitializeTimeProvider();
    }

    /// <summary>
    /// 初始化时间提供者
    /// </summary>
    private void InitializeTimeProvider()
    {
        if (_timeSourceType.Equals("ntp", StringComparison.OrdinalIgnoreCase))
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
        }
        else
        {
            var selectedServers = new List<string>();
            if (_selectedHttpServerIndex >= 0 && _selectedHttpServerIndex < _httpServers.Count)
            {
                selectedServers.Add(_httpServers[_selectedHttpServerIndex]);
            }
            else
            {
                selectedServers.AddRange(_httpServers);
            }
            _currentTimeProvider = new HttpTimeProvider(_httpClient, selectedServers.ToArray());
        }

        Logger.Info("CloudCalibrationService",
            $"时间提供者已初始化: 类型={_timeSourceType}, 提供者={_currentTimeProvider.Name}");
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
    /// <param name="triggerThreshold">触发校准的偏差阈值（秒）</param>
    public void Configure(
        bool enabled,
        int interval = 300,
        int triggerThreshold = 5)
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
    /// 配置时间源
    /// </summary>
    /// <param name="timeSourceType">时间源类型：http 或 ntp</param>
    /// <param name="ntpServers">NTP服务器列表</param>
    /// <param name="httpServers">HTTP服务器列表</param>
    /// <param name="selectedNtpServerIndex">选中的NTP服务器索引</param>
    /// <param name="selectedHttpServerIndex">选中的HTTP服务器索引</param>
    public void ConfigureTimeSource(
        string timeSourceType,
        List<string>? ntpServers = null,
        List<string>? httpServers = null,
        int selectedNtpServerIndex = 0,
        int selectedHttpServerIndex = 0)
    {
        _timeSourceType = timeSourceType;
        _ntpServers = ntpServers ?? new List<string> { "ntp.aliyun.com", "ntp.ntsc.ac.cn", "time.windows.com" };
        _httpServers = httpServers ?? new List<string>
        {
            "https://worldtimeapi.org/api/timezone/Etc/UTC",
            "https://timeapi.io/api/Time/current/zone?timeZone=UTC",
            "https://www.timeapi.io/api/Time/current/zone?timeZone=UTC"
        };
        _selectedNtpServerIndex = selectedNtpServerIndex;
        _selectedHttpServerIndex = selectedHttpServerIndex;

        InitializeTimeProvider();

        Logger.Info("CloudCalibrationService",
            $"时间源已配置: 类型={timeSourceType}, NTP服务器={string.Join(", ", _ntpServers)}, HTTP服务器={string.Join(", ", _httpServers)}");
    }

    /// <summary>
    /// 切换时间源
    /// </summary>
    /// <param name="timeSourceType">时间源类型：http 或 ntp</param>
    public void SwitchTimeSource(string timeSourceType)
    {
        if (_timeSourceType.Equals(timeSourceType, StringComparison.OrdinalIgnoreCase))
        {
            Logger.Info("CloudCalibrationService", $"时间源已经是 {timeSourceType}，无需切换");
            return;
        }

        _timeSourceType = timeSourceType;
        _failureCount = 0;
        _currentInterval = CalibrationInterval;

        InitializeTimeProvider();

        Logger.Info("CloudCalibrationService", $"已切换时间源为: {timeSourceType}");

        if (IsRunning && IsEnabled)
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
            var cloudTime = await GetCloudTimeAsync();

            if (cloudTime.HasValue)
            {
                var localTime = _timeService.GetCurrentTime();
                var offset = (cloudTime.Value - localTime).Duration();

                if (offset.TotalSeconds > CalibrationTriggerThreshold)
                {
                    Logger.Info("CloudCalibrationService",
                        $"校准时间: 本地={localTime:HH:mm:ss}, 云端={cloudTime.Value:HH:mm:ss}, 偏差={offset.TotalSeconds:F2}秒");

                    _timeService.Calibrate(cloudTime.Value);
                    _lastCalibrationTime = DateTime.Now;

                    _failureCount = 0;
                    _currentInterval = CalibrationInterval;

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
    /// 获取云端时间
    /// </summary>
    /// <returns>云端时间（北京时间）</returns>
    private async Task<DateTime?> GetCloudTimeAsync()
    {
        if (_currentTimeProvider == null)
        {
            Logger.Error("CloudCalibrationService", "时间提供者未初始化");
            return null;
        }

        try
        {
            var utcTime = await _currentTimeProvider.GetTimeAsync(TimeSpan.FromSeconds(CalibrationTimeout));

            if (utcTime.HasValue)
            {
                var beijingTime = utcTime.Value.AddHours(8);
                Logger.Info("CloudCalibrationService",
                    $"获取云端时间成功: UTC={utcTime.Value:yyyy-MM-dd HH:mm:ss}, 北京时间={beijingTime:yyyy-MM-dd HH:mm:ss}");
                return beijingTime;
            }

            return null;
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
    /// 析构函数，释放资源
    /// </summary>
    ~CloudCalibrationService()
    {
        _calibrationTimer?.Dispose();
        _httpClient?.Dispose();
    }
}