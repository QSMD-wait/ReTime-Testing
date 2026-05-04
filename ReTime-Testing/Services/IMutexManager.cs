using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 互斥锁管理服务接口
/// 负责应用程序实例的互斥控制，防止多实例同时运行
/// </summary>
public interface IMutexManager : IDisposable
{
    /// <summary>
    /// 当前配置
    /// </summary>
    MutexConfig Config { get; }

    /// <summary>
    /// 互斥锁是否已获取
    /// </summary>
    bool IsAcquired { get; }

    /// <summary>
    /// 互斥锁冲突事件
    /// </summary>
    event EventHandler<MutexConflictEventArgs>? OnConflictDetected;

    /// <summary>
    /// 互斥锁获取成功事件
    /// </summary>
    event EventHandler? OnMutexAcquired;

    /// <summary>
    /// 初始化互斥锁
    /// </summary>
    void Initialize(MutexConfig config);

    /// <summary>
    /// 尝试获取互斥锁
    /// </summary>
    bool TryAcquire();

    /// <summary>
    /// 释放互斥锁
    /// </summary>
    void Release();
}
