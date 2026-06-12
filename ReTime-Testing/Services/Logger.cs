using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
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

            var serilog = SerilogLogService.Instance;
            if (serilog == null)
            {
                System.Diagnostics.Debug.WriteLine("[Logger] OnSerilogReady: SerilogLogService.Instance 为 null，无法回放早期日志");
                return;
            }

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
            var total = entries.Length;

            if (total == 0) return;

            System.Diagnostics.Debug.WriteLine($"[Logger] FlushEarlyCache: 开始回放 {total} 条早期日志");

            var successCount = 0;
            var failCount = 0;

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
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    System.Diagnostics.Debug.WriteLine($"[Logger] FlushEarlyCache: 回放失败 [{entry.Level}][{entry.Module}] {entry.Message} | 异常: {ex.Message}");
                }
            }

            _earlyCache.Clear();

            System.Diagnostics.Debug.WriteLine($"[Logger] FlushEarlyCache: 回放完成，成功 {successCount}/{total}，失败 {failCount}/{total}");
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

            // Serilog 尚未初始化：缓存 + Debug 输出
            _earlyCache.Enqueue(new CachedEntry(DateTimeOffset.Now, level, module, message, null));
            FallbackWrite(level, module, message);
        }

        /// <summary>
        /// 回退日志输出（Debug.WriteLine）
        /// </summary>
        private static void FallbackWrite(LogLevel level, string module, string message)
        {
            var now = DateTime.Now;
            var logMessage = $"[{now:yyyy-MM-dd HH:mm:ss}][{level}][{module}] {message}";

            System.Diagnostics.Debug.WriteLine(logMessage);

            if (level == LogLevel.ERR)
            {
                System.Diagnostics.Trace.TraceError(logMessage);
            }
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
            if (TryGetSerilog(out var serilog))
            {
                serilog.Error(module, message, exception);
                return;
            }

            // Serilog 尚未初始化：缓存 + Debug 输出
            _earlyCache.Enqueue(new CachedEntry(DateTimeOffset.Now, LogLevel.ERR, module, message, exception));
            var fullMessage = message + "\n异常类型: " + exception.GetType().Name +
                "\n异常信息: " + exception.Message +
                "\n堆栈跟踪: " + exception.StackTrace;
            FallbackWrite(LogLevel.ERR, module, fullMessage);
        }
    }
}