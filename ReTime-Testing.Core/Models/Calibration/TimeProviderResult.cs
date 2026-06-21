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