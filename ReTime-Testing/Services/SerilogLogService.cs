
using System;
using System.IO;
using Serilog;
using Serilog.Events;
using Serilog.Configuration;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 基于 Serilog 的日志服务实现
    /// </summary>
    public class SerilogLogService : ILogService
    {
        private readonly ILogger _logger;
        private static SerilogLogService? _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// 获取日志服务单例实例
        /// </summary>
        public static SerilogLogService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new SerilogLogService(new LogServiceConfiguration());
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 使用配置初始化日志服务单例
        /// </summary>
        public static void Initialize(LogServiceConfiguration configuration)
        {
            lock (_lock)
            {
                _instance = new SerilogLogService(configuration);
            }
        }

        /// <summary>
        /// 私有构造函数，根据配置创建 Serilog Logger
        /// </summary>
        private SerilogLogService(LogServiceConfiguration configuration)
        {
            var serilogLevel = ToSerilogLevel(configuration.MinimumLevel);
            var logDirectory = Path.GetFullPath(configuration.LogDirectory);

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(serilogLevel)
                .Enrich.With<CustomLevelEnricher>();

            // 命令行输出：秒级时间
            var consoleTemplate = $"[{{Timestamp:yyyy-MM-dd HH:mm:ss}}][{{CustomLevel}}]{{FormattedSourceContext}} {{Message:lj}}{{NewLine}}{{Exception}}";
            loggerConfig = loggerConfig.WriteTo.Console(
                outputTemplate: consoleTemplate,
                restrictedToMinimumLevel: serilogLevel);

            // 文件输出：毫秒级时间，每次启动一个独立日志文件
            if (configuration.EnableFileOutput)
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // 生成唯一日志文件名：RTT_log-YYYY-MM-DD-HH-MM-SS[-序列号].log
                var logFilePath = GenerateLogFilePath(logDirectory);

                var fileTemplate = $"[{{Timestamp:yyyy-MM-dd HH:mm:ss.fff}}][{{CustomLevel}}]{{FormattedSourceContext}} {{Message:lj}}{{NewLine}}{{Exception}}";

                loggerConfig = loggerConfig.WriteTo.File(
                    path: logFilePath,
                    outputTemplate: fileTemplate,
                    rollingInterval: RollingInterval.Infinite,
                    fileSizeLimitBytes: configuration.FileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    restrictedToMinimumLevel: serilogLevel);

                // 清理过期日志文件
                CleanExpiredLogs(logDirectory, configuration.RetainedFileCountLimit);
            }

            _logger = loggerConfig.CreateLogger();
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

            // 无序列号
            if (!File.Exists(basePath + ".log"))
            {
                return basePath + ".log";
            }

            // 同秒多实例，追加序列号
            var sequence = 1;
            while (File.Exists($"{basePath}-{sequence}.log"))
            {
                sequence++;
            }

            return $"{basePath}-{sequence}.log";
        }

        /// <summary>
        /// 清理过期的日志文件。
        /// 按文件创建时间，删除超过保留天数的 RTT_log-*.log 文件。
        /// </summary>
        private static void CleanExpiredLogs(string logDirectory, int retainedDays)
        {
            if (retainedDays <= 0) return;

            try
            {
                var cutoff = DateTime.Now.AddDays(-retainedDays);
                var logFiles = Directory.GetFiles(logDirectory, "RTT_log-*.log");

                foreach (var file in logFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoff)
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
                // 清理失败不影响日志服务正常运行
            }
        }

        /// <inheritdoc />
        public void Trace(string module, string message)
        {
            _logger.ForContext("SourceContext", module).Verbose(message);
        }

        /// <inheritdoc />
        public void Debug(string module, string message)
        {
            _logger.ForContext("SourceContext", module).Debug(message);
        }

        /// <inheritdoc />
        public void Info(string module, string message)
        {
            _logger.ForContext("SourceContext", module).Information(message);
        }

        /// <inheritdoc />
        public void Warn(string module, string message)
        {
            _logger.ForContext("SourceContext", module).Warning(message);
        }

        /// <inheritdoc />
        public void Error(string module, string message)
        {
            _logger.ForContext("SourceContext", module).Error(message);
        }

        /// <inheritdoc />
        public void Error(string module, string message, Exception exception)
        {
            _logger.ForContext("SourceContext", module).Error(exception, message);
        }

        /// <summary>
        /// 将 LogLevel 转换为 Serilog LogEventLevel
        /// </summary>
        private static LogEventLevel ToSerilogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.TRC => LogEventLevel.Verbose,
                LogLevel.DBG => LogEventLevel.Debug,
                LogLevel.INF => LogEventLevel.Information,
                LogLevel.WRN => LogEventLevel.Warning,
                LogLevel.ERR => LogEventLevel.Error,
                _ => LogEventLevel.Information
            };
        }

        /// <summary>
        /// 自定义等级富化器，将 Serilog 等级映射为三字母等级名
        /// </summary>
        private class CustomLevelEnricher : Serilog.Core.ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, Serilog.Core.ILogEventPropertyFactory propertyFactory)
            {
                var levelStr = logEvent.Level switch
                {
                    LogEventLevel.Verbose => "TRC",
                    LogEventLevel.Debug => "DBG",
                    LogEventLevel.Information => "INF",
                    LogEventLevel.Warning => "WRN",
                    LogEventLevel.Error => "ERR",
                    LogEventLevel.Fatal => "FTL",
                    _ => "???"
                };

                // SourceContext 格式化为 [Module]
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
