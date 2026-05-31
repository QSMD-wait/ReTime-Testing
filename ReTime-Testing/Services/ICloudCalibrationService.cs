namespace ReTime_Testing.Services;

/// <summary>
/// 云端校准服务接口
/// 定期从NTP服务器获取时间进行校准，支持RTT补偿和微调/跳跃区分
/// </summary>
public interface ICloudCalibrationService
{
    /// <summary>
    /// 是否启用云端校准
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 校准失败次数
    /// </summary>
    int FailureCount { get; }

    /// <summary>
    /// 上次校准时间
    /// </summary>
    DateTime LastCalibrationTime { get; }

    /// <summary>
    /// 当前校准间隔（秒）
    /// </summary>
    int CurrentInterval { get; }

    /// <summary>
    /// 上次校准的RTT（毫秒）
    /// </summary>
    double LastRttMs { get; }

    /// <summary>
    /// 当前使用的时间提供者名称
    /// </summary>
    string CurrentProviderName { get; }

    /// <summary>
    /// 启动校准服务
    /// </summary>
    void Start();

    /// <summary>
    /// 停止校准服务
    /// </summary>
    void Stop();

    /// <summary>
    /// 配置校准参数
    /// </summary>
    void Configure(bool enabled, int interval = 300, int triggerThreshold = 5);

    /// <summary>
    /// 配置校准参数（高级）
    /// </summary>
    void Configure(bool enabled, int interval, int timeout, int maxRetryCount, double backoffMultiplier, int triggerThreshold);

    /// <summary>
    /// 配置NTP服务器
    /// </summary>
    void ConfigureNtpServers(List<string>? ntpServers = null, int selectedNtpServerIndex = 0);

    /// <summary>
    /// 手动触发校准
    /// </summary>
    Task<bool> CalibrateAsync();

    /// <summary>
    /// 重置校准状态
    /// </summary>
    void Reset();
}