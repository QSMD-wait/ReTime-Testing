using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 时间校准服务接口
/// 统一管理校准源选择（系统/云端）、校准策略（微调/跳跃）、定时调度、休眠恢复
/// 连接单调时钟与实际时间源
/// </summary>
public interface ITimeCalibrationService
{
    /// <summary>
    /// 是否启用校准
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 当前校准源类型
    /// </summary>
    CalibrationSource CurrentSource { get; }

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
    /// 上次校准的RTT（毫秒），仅云端源有效
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
    /// 应用校准配置
    /// </summary>
    /// <param name="config">校准配置</param>
    void ApplyConfig(CalibrationConfig config);

    /// <summary>
    /// 手动触发校准
    /// </summary>
    /// <returns>是否校准成功</returns>
    Task<bool> CalibrateAsync();

    /// <summary>
    /// 重置校准状态（失败计数器和间隔）
    /// </summary>
    void Reset();
}
