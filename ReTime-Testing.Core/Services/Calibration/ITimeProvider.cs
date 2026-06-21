namespace ReTime_Testing.Services;

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