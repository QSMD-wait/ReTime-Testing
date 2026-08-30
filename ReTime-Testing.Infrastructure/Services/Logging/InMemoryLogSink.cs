using System;
using System.Collections.Concurrent;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 内存日志 Sink
    /// 作为 Serilog 管道的输出端之一，所有日志（无论经 Logger 门面还是 ILogger&lt;T&gt;）都会流经此处，
    /// 供日志查看器实时展示
    /// </summary>
    public sealed class InMemoryLogSink : Serilog.Core.ILogEventSink
    {
        /// <summary>
        /// 内存缓冲最大条数
        /// </summary>
        public const int MaxLogBufferCount = 1000;

        private readonly ConcurrentQueue<LogEntryItem> _buffer = new();

        /// <summary>
        /// 新增日志条目事件（日志查看器订阅）
        /// </summary>
        public event Action<LogEntryItem>? LogEntryAdded;

        /// <summary>
        /// 获取内存缓冲中的日志条目快照
        /// </summary>
        public IReadOnlyList<LogEntryItem> GetRecentEntries() => _buffer.ToArray();

        /// <summary>
        /// 清空内存日志缓冲
        /// </summary>
        public void Clear()
        {
            while (_buffer.TryDequeue(out _)) { }
        }

        /// <inheritdoc />
        public void Emit(Serilog.Events.LogEvent logEvent)
        {
            var message = logEvent.RenderMessage();
            if (logEvent.Exception != null)
            {
                message = $"{message}{Environment.NewLine}{logEvent.Exception}";
            }

            var item = new LogEntryItem(
                logEvent.Timestamp,
                ToLogLevel(logEvent.Level),
                GetSourceContext(logEvent),
                message);

            _buffer.Enqueue(item);
            while (_buffer.Count > MaxLogBufferCount && _buffer.TryDequeue(out _)) { }

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
        /// 提取来源上下文（无则返回空字符串）
        /// </summary>
        private static string GetSourceContext(Serilog.Events.LogEvent logEvent)
        {
            if (logEvent.Properties.TryGetValue("SourceContext", out var value))
            {
                return value.ToString().Trim('"');
            }
            return string.Empty;
        }

        /// <summary>
        /// 将 Serilog 等级转换为应用内 LogLevel
        /// </summary>
        private static LogLevel ToLogLevel(Serilog.Events.LogEventLevel level)
        {
            return level switch
            {
                Serilog.Events.LogEventLevel.Verbose => LogLevel.TRC,
                Serilog.Events.LogEventLevel.Debug => LogLevel.DBG,
                Serilog.Events.LogEventLevel.Information => LogLevel.INF,
                Serilog.Events.LogEventLevel.Warning => LogLevel.WRN,
                Serilog.Events.LogEventLevel.Error => LogLevel.ERR,
                Serilog.Events.LogEventLevel.Fatal => LogLevel.ERR,
                _ => LogLevel.INF
            };
        }
    }
}
