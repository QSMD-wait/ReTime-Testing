namespace ReTime_Testing.Services;

/// <summary>
/// 时间提供者获取结果（包含RTT信息）
/// </summary>
public class TimeProviderResult
{
    /// <summary>
    /// 获取到的云端时间（UTC）
    /// </summary>
    public DateTime UtcTime { get; }

    /// <summary>
    /// 网络往返延迟（RTT）
    /// </summary>
    public TimeSpan RoundTripTime { get; }

    /// <summary>
    /// 补偿RTT后的校准时间（UtcTime + RTT/2）
    /// </summary>
    public DateTime CalibratedTime => UtcTime.Add(RoundTripTime / 2);

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="utcTime">UTC时间</param>
    /// <param name="roundTripTime">网络往返延迟</param>
    public TimeProviderResult(DateTime utcTime, TimeSpan roundTripTime)
    {
        UtcTime = utcTime;
        RoundTripTime = roundTripTime;
    }
}

/// <summary>
/// 时间提供者接口
/// 统一抽象不同时间源（NTP等）
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// 获取云端时间（包含RTT信息）
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <returns>时间提供结果（含RTT），失败返回null</returns>
    Task<TimeProviderResult?> GetTimeAsync(TimeSpan timeout);

    /// <summary>
    /// 提供者名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 提供者类型
    /// </summary>
    TimeProviderType Type { get; }
}

/// <summary>
/// 时间提供者类型
/// </summary>
public enum TimeProviderType
{
    /// <summary>
    /// NTP协议
    /// </summary>
    Ntp
}