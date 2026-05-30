namespace ReTime_Testing.Services;

/// <summary>
/// 时间提供者接口
/// 统一抽象不同时间源（HTTP API、NTP等）
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// 获取云端时间
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <returns>云端时间（UTC），失败返回null</returns>
    Task<DateTime?> GetTimeAsync(TimeSpan timeout);

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
    /// HTTP API
    /// </summary>
    Http,

    /// <summary>
    /// NTP协议
    /// </summary>
    Ntp
}