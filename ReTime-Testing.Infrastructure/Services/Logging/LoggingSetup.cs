using System;
using System.IO;
using System.Text.RegularExpressions;
using ReTime_Testing.Models;
using Serilog;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// Serilog 日志系统初始化器
    /// 在应用启动最早阶段调用 Initialize()，接管全量日志输出（控制台 + 文件 + 内存缓冲），
    /// 替代旧的"缓存-回放"机制
    /// </summary>
    public static class LoggingSetup
    {
        /// <summary>
        /// 内存日志 Sink（供日志查看器消费，始终存在以便事件订阅先于初始化）
        /// </summary>
        public static InMemoryLogSink InMemorySink { get; } = new();

        /// <summary>
        /// 根据日志配置初始化全局 Serilog 日志器（Log.Logger）
        /// 应在应用启动最早阶段调用；重复调用会先释放旧日志器
        /// </summary>
        public static void Initialize(LogConfig config, string logsDirectory)
        {
            var serilogLevel = ToSerilogLevel(config.MinimumLevel);
            var logDirectory = Path.GetFullPath(logsDirectory);

            var loggerConfig = new Serilog.LoggerConfiguration()
                .MinimumLevel.Is(serilogLevel)
                .Enrich.With<CustomLevelEnricher>()
                .WriteTo.Sink(InMemorySink);

            // 命令行输出：秒级时间
            var consoleTemplate = $"[{{Timestamp:yyyy-MM-dd HH:mm:ss}}][{{CustomLevel}}]{{FormattedSourceContext}} {{Message:lj}}{{NewLine}}{{Exception}}";
            loggerConfig = loggerConfig.WriteTo.Console(
                outputTemplate: consoleTemplate,
                restrictedToMinimumLevel: serilogLevel);

            // 文件输出：毫秒级时间，每次启动一个独立日志文件
            if (config.EnableFileOutput)
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                var logFilePath = GenerateLogFilePath(logDirectory);

                var fileTemplate = $"[{{Timestamp:yyyy-MM-dd HH:mm:ss.fff}}][{{CustomLevel}}]{{FormattedSourceContext}} {{Message:lj}}{{NewLine}}{{Exception}}";

                loggerConfig = loggerConfig.WriteTo.File(
                    path: logFilePath,
                    outputTemplate: fileTemplate,
                    rollingInterval: Serilog.RollingInterval.Infinite,
                    fileSizeLimitBytes: config.FileSizeLimitMB * 1024L * 1024L,
                    rollOnFileSizeLimit: true,
                    restrictedToMinimumLevel: serilogLevel,
                    encoding: System.Text.Encoding.UTF8);

                // 清理过期日志文件
                CleanExpiredLogs(logDirectory, config.RetainedDays);
            }

            // 替换全局日志器前，释放旧实例持有的文件流
            (Serilog.Log.Logger as IDisposable)?.Dispose();
            Serilog.Log.Logger = loggerConfig.CreateLogger();

            // AppLog 的工厂绑定旧日志器，需要重建
            AppLog.ResetFactory();
        }

        /// <summary>
        /// 关闭日志系统并确保缓冲落盘（应用退出时调用）
        /// </summary>
        public static void Shutdown()
        {
            Serilog.Log.CloseAndFlush();
        }

        /// <summary>
        /// 生成唯一的日志文件路径。
        /// 格式：RTT_log-YYYY-MM-DD-HH-MM-SS.log
        /// 如果同秒内已存在文件，则追加序列号：RTT_log-YYYY-MM-DD-HH-MM-SS-1.log
        /// </summary>
        private static string GenerateLogFilePath(string logDirectory)
        {
            var now = DateTime.Now;
            var baseName = $"RTT_log-{now:yyyy-MM-dd-HH-mm-ss}";
            var basePath = Path.Combine(logDirectory, baseName);

            if (!File.Exists(basePath + ".log"))
            {
                return basePath + ".log";
            }

            var sequence = 1;
            while (File.Exists($"{basePath}-{sequence}.log"))
            {
                sequence++;
            }

            return $"{basePath}-{sequence}.log";
        }

        /// <summary>
        /// 清理过期的日志文件（超过保留天数的 RTT_log-*.log）
        /// </summary>
        private static readonly Regex _datePattern = new(@"RTT_log-(\d{4}-\d{2}-\d{2})", RegexOptions.Compiled);

        private static void CleanExpiredLogs(string logDirectory, int retainedDays)
        {
            if (retainedDays <= 0) return;

            try
            {
                var cutoff = DateTime.Now.AddDays(-retainedDays);
                var logFiles = Directory.GetFiles(logDirectory, "RTT_log-*.log");

                foreach (var file in logFiles)
                {
                    var match = _datePattern.Match(Path.GetFileName(file));
                    if (!match.Success) continue;

                    if (DateTime.TryParse(match.Groups[1].Value, out var fileDate) && fileDate < cutoff)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // 文件可能正在使用中，忽略删除失败
                        }
                    }
                }
            }
            catch
            {
                // 清理失败不影响日志系统正常运行
            }
        }

        /// <summary>
        /// 将 LogLevel 转换为 Serilog LogEventLevel
        /// </summary>
        internal static Serilog.Events.LogEventLevel ToSerilogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.TRC => Serilog.Events.LogEventLevel.Verbose,
                LogLevel.DBG => Serilog.Events.LogEventLevel.Debug,
                LogLevel.INF => Serilog.Events.LogEventLevel.Information,
                LogLevel.WRN => Serilog.Events.LogEventLevel.Warning,
                LogLevel.ERR => Serilog.Events.LogEventLevel.Error,
                _ => Serilog.Events.LogEventLevel.Information
            };
        }

        /// <summary>
        /// 自定义等级富化器，将 Serilog 等级映射为三字母等级名并格式化来源
        /// </summary>
        private class CustomLevelEnricher : Serilog.Core.ILogEventEnricher
        {
            public void Enrich(Serilog.Events.LogEvent logEvent, Serilog.Core.ILogEventPropertyFactory propertyFactory)
            {
                var levelStr = logEvent.Level switch
                {
                    Serilog.Events.LogEventLevel.Verbose => "TRC",
                    Serilog.Events.LogEventLevel.Debug => "DBG",
                    Serilog.Events.LogEventLevel.Information => "INF",
                    Serilog.Events.LogEventLevel.Warning => "WRN",
                    Serilog.Events.LogEventLevel.Error => "ERR",
                    Serilog.Events.LogEventLevel.Fatal => "FTL",
                    _ => "???"
                };

                var sourceContext = "";
                if (logEvent.Properties.TryGetValue("SourceContext", out var scValue))
                {
                    var scStr = scValue.ToString().Trim('"');
                    sourceContext = $"[{scStr}]";
                }

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CustomLevel", levelStr));
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("FormattedSourceContext", sourceContext));
            }
        }
    }
}
