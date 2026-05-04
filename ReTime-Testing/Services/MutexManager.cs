using System;
using System.Threading;
using ReTime_Testing.Models;
using ReTime_Testing.Services;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 互斥锁管理服务
    /// 负责应用程序实例的互斥控制，防止多实例同时运行
    /// </summary>
    public class MutexManager : IMutexManager
    {
        private static readonly Lazy<MutexManager> _instance = new Lazy<MutexManager>(() => new MutexManager());

        private Mutex? _mutex;
        private MutexConfig _config;
        private bool _disposed = false;
        private bool _isAcquired = false;

        /// <summary>
        /// 获取 MutexManager 单例实例
        /// </summary>
        public static MutexManager Instance => _instance.Value;

        /// <summary>
        /// 当前配置
        /// </summary>
        public MutexConfig Config => _config;

        /// <summary>
        /// 互斥锁是否已获取
        /// </summary>
        public bool IsAcquired => _isAcquired;

        /// <summary>
        /// 互斥锁冲突事件
        /// 当检测到已有实例运行时触发
        /// </summary>
        public event EventHandler<MutexConflictEventArgs>? OnConflictDetected;

        /// <summary>
        /// 互斥锁获取成功事件
        /// 当成功获取互斥锁时触发
        /// </summary>
        public event EventHandler? OnMutexAcquired;

        /// <summary>
        /// 私有构造函数（单例模式）
        /// </summary>
        private MutexManager()
        {
            _config = MutexConfig.GetDefault();
        }

        /// <summary>
        /// 使用自定义配置初始化
        /// </summary>
        /// <param name="config">互斥锁配置</param>
        public void Initialize(MutexConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Logger.Info(GetType().FullName ?? "MutexManager", $"MutexManager 初始化完成，互斥锁ID: {_config.MutexId}");
        }

        /// <summary>
        /// 尝试获取互斥锁
        /// </summary>
        /// <returns>是否成功获取互斥锁</returns>
        public bool TryAcquire()
        {
            if (_disposed)
            {
                Logger.Error(GetType().FullName ?? "MutexManager", "MutexManager 已被释放，无法获取互斥锁");
                return false;
            }

            if (!_config.IsEnabled)
            {
                Logger.Info(GetType().FullName ?? "MutexManager", "互斥锁功能已禁用");
                _isAcquired = true;
                OnMutexAcquired?.Invoke(this, EventArgs.Empty);
                return true;
            }

            try
            {
                // 尝试创建或打开命名的互斥锁
                _mutex = new Mutex(false, _config.MutexId);

                // 尝试立即获取互斥锁所有权
                _isAcquired = _mutex.WaitOne(0, false);

                if (_isAcquired)
                {
                    Logger.Info(GetType().FullName ?? "MutexManager", $"成功获取互斥锁: {_config.MutexId}");
                    OnMutexAcquired?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Logger.Warn(GetType().FullName ?? "MutexManager", $"检测到互斥锁冲突: {_config.MutexId}");
                    OnConflictDetected?.Invoke(this, new MutexConflictEventArgs(
                        _config.MutexId,
                        _config.ConflictWindowAutoCloseTime
                    ));
                }

                return _isAcquired;
            }
            catch (Exception ex)
            {
                Logger.Error(GetType().FullName ?? "MutexManager", "获取互斥锁时发生异常", ex);
                return false;
            }
        }

        /// <summary>
        /// 释放互斥锁
        /// </summary>
        public void Release()
        {
            if (_mutex != null && _isAcquired)
            {
                try
                {
                    _mutex.ReleaseMutex();
                    _isAcquired = false;
                    Logger.Info(GetType().FullName ?? "MutexManager", $"互斥锁已释放: {_config.MutexId}");
                }
                catch (Exception ex)
                {
                    Logger.Error(GetType().FullName ?? "MutexManager", "释放互斥锁时发生异常", ex);
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实现
        /// </summary>
        /// <param name="disposing">是否正在释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Release();
                    _mutex?.Dispose();
                    _mutex = null;
                }

                _disposed = true;
                Logger.Info(GetType().FullName ?? "MutexManager", "MutexManager 已释放");
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~MutexManager()
        {
            Dispose(false);
        }
    }
}