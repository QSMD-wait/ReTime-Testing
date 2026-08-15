using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 内存日志条目（供日志查看器使用）
    /// </summary>
    public sealed record LogEntryItem(DateTimeOffset Timestamp, LogLevel Level, string Module, string Message);

    /// <summary>
    /// 日志记录器
    /// 优先委托给 SerilogLogService（控制台 + 文件输出），
    /// 若 SerilogLogService 尚未初始化则缓存日志消息，待初始化后回放写入同一文件
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// 早期日志缓存条目
        /// </summary>
        private record class CachedEntry(DateTimeOffset Timestamp, LogLevel Level, string Module, string? Message, Exception? Exception);

        /// <summary>
        /// 内存日志环形缓冲（供日志查看器展示）
        /// </summary>
        private static readonly ConcurrentQueue<LogEntryItem> _logBuffer = new();

        /// <summary>
        /// 内存缓冲最大条数
        /// </summary>
        public const int MaxLogBufferCount = 1000;

        /// <summary>
        /// 新增日志条目事件（日志查看器订阅）
        /// </summary>
        public static event Action<LogEntryItem>? LogEntryAdded;

        /// <summary>
        /// 记录到内存缓冲并触发新增事件
        /// </summary>
        private static void RecordToBuffer(LogLevel level, string module, string message)
        {
            var item = new LogEntryItem(DateTimeOffset.Now, level, module, message);
            _logBuffer.Enqueue(item);
            while (_logBuffer.Count > MaxLogBufferCount && _logBuffer.TryDequeue(out _)) { }

            try
            {
                LogEntryAdded?.Invoke(item);
            }
            catch
            {
                // 订阅者异常不影响日志记录本身
            }
        }

        /// <summary>
        /// 获取内存缓冲中的日志条目快照
        /// </summary>
        public static IReadOnlyList<LogEntryItem> GetRecentLogEntries() => _logBuffer.ToArray();

        /// <summary>
        /// 清空内存日志缓冲
        /// </summary>
        public static void ClearLogBuffer()
        {
            while (_logBuffer.TryDequeue(out _)) { }
        }

        /// <summary>
        /// Serilog 初始化前的日志缓存（使用 ConcurrentQueue 保证 FIFO 顺序和可预测的枚举行为）
        /// </summary>
        private static readonly ConcurrentQueue<CachedEntry> _earlyCache = new();

        /// <summary>
        /// Serilog 是否已初始化
        /// </summary>
        private static volatile bool _serilogReady;

        /// <summary>
        /// 标记 Serilog 已初始化，并回放缓存的早期日志
        /// 应在 SerilogLogService.Initialize() 之后立即调用
        /// </summary>
        public static void OnSerilogReady()
        {
            _serilogReady = true;

            if (SerilogLogService.Instance == null) return;

            FlushEarlyCache();
        }

        /// <summary>
        /// 将缓存的早期日志回放到 SerilogLogService
        /// </summary>
        private static void FlushEarlyCache()
        {
            var serilog = SerilogLogService.Instance;
            if (serilog == null) return;

            var entries = _earlyCache.ToArray();

            if (entries.Length == 0) return;

            System.Diagnostics.Debug.WriteLine($"[Logger] 开始回放 {entries.Length} 条早期缓存日志");

            foreach (var entry in entries)
            {
                try
                {
                    if (entry.Exception != null)
                    {
                        serilog.WriteWithTimestamp(LogLevel.ERR, entry.Module,
                            entry.Message ?? entry.Exception.Message, entry.Timestamp, entry.Exception);
                    }
                    else
                    {
                        serilog.WriteWithTimestamp(entry.Level, entry.Module,
                            entry.Message ?? "", entry.Timestamp);
                    }
                }
                catch
                {
                }
            }

            _earlyCache.Clear();

            System.Diagnostics.Debug.WriteLine($"[Logger] 早期缓存日志回放完成，共 {entries.Length} 条");
        }

        /// <summary>
        /// 尝试获取 SerilogLogService 实例（仅在就绪后有效）
        /// </summary>
        private static bool TryGetSerilog([NotNullWhen(true)] out SerilogLogService? serilog)
        {
            if (_serilogReady)
            {
                serilog = SerilogLogService.Instance;
                return serilog != null;
            }

            serilog = null;
            return false;
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        public static void Log(LogLevel level, string module, string message)
        {
            RecordToBuffer(level, module, message);

            if (TryGetSerilog(out var serilog))
            {
                switch (level)
                {
                    case LogLevel.TRC: serilog.Trace(module, message); break;
                    case LogLevel.DBG: serilog.Debug(module, message); break;
                    case LogLevel.INF: serilog.Info(module, message); break;
                    case LogLevel.WRN: serilog.Warn(module, message); break;
                    case LogLevel.ERR: serilog.Error(module, message); break;
                }
                return;
            }

            _earlyCache.Enqueue(new CachedEntry(DateTimeOffset.Now, level, module, message, null));
        }

        /// <summary>
        /// 记录跟踪日志
        /// </summary>
        public static void Trace(string module, string message)
        {
            Log(LogLevel.TRC, module, message);
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        public static void Debug(string module, string message)
        {
            Log(LogLevel.DBG, module, message);
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public static void Info(string module, string message)
        {
            Log(LogLevel.INF, module, message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public static void Warn(string module, string message)
        {
            Log(LogLevel.WRN, module, message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public static void Error(string module, string message)
        {
            Log(LogLevel.ERR, module, message);
        }

        /// <summary>
        /// 记录错误日志（带异常）
        /// </summary>
        public static void Error(string module, string message, Exception exception)
        {
            RecordToBuffer(LogLevel.ERR, module, $"{message}{Environment.NewLine}{exception}");

            if (TryGetSerilog(out var serilog))
            {
                serilog.Error(module, message, exception);
                return;
            }

            _earlyCache.Enqueue(new CachedEntry(DateTimeOffset.Now, LogLevel.ERR, module, message, exception));
        }
    }
}