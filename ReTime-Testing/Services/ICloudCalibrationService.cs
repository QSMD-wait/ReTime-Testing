namespace ReTime_Testing.Services;

/// <summary>
/// 云端校准服务接口
/// 纯NTP数据源，仅负责从NTP服务器获取时间（含RTT补偿）
/// 校准策略和调度由 TimeCalibrationService 统一管理
/// </summary>
public interface ICloudCalibrationService
{
    /// <summary>
    /// 获取云端时间（含RTT补偿信息）
    /// </summary>
    /// <param name="timeout">请求超时时间</param>
    /// <returns>时间提供结果（含RTT），失败返回null</returns>
    Task<TimeProviderResult?> GetCloudTimeAsync(TimeSpan timeout);

    /// <summary>
    /// 当前使用的时间提供者名称
    /// </summary>
    string CurrentProviderName { get; }

    /// <summary>
    /// 上次请求的RTT（毫秒）
    /// </summary>
    double LastRttMs { get; }

    /// <summary>
    /// 配置NTP服务器
    /// </summary>
    /// <param name="ntpServers">NTP服务器列表</param>
    /// <param name="selectedNtpServerIndex">选中的NTP服务器索引</param>
    void ConfigureNtpServers(List<string> ntpServers, int selectedNtpServerIndex = 0);
}