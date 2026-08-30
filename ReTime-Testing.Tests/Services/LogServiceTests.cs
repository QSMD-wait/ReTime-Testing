using System;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using Xunit;

namespace ReTime_Testing.Tests.Services
{
    /// <summary>
    /// LoggingSetup + InMemoryLogSink 测试
    /// 验证前置初始化、文件输出、句柄释放（原 P0）、模板转义安全与内存缓冲行为
    /// </summary>
    public class LogServiceTests : IDisposable
    {
        private readonly string _testLogDir;

        public LogServiceTests()
        {
            _testLogDir = Path.Combine(Path.GetTempPath(), $"RTT_LogTest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testLogDir);
        }

        public void Dispose()
        {
            LoggingSetup.Shutdown();
            try
            {
                Directory.Delete(_testLogDir, true);
            }
            catch
            {
                // 临时目录清理失败不影响测试结果
            }
        }

        private LogConfig CreateConfig(ReTime_Testing.Models.LogLevel minimumLevel = ReTime_Testing.Models.LogLevel.DBG, int retainedDays = 30)
        {
            return new LogConfig
            {
                EnableFileOutput = true,
                MinimumLevel = minimumLevel,
                RetainedDays = retainedDays,
                FileSizeLimitMB = 5
            };
        }

        private string GetSingleLogFile()
        {
            var files = Directory.GetFiles(_testLogDir, "RTT_log-*.log");
            files.Should().HaveCount(1, "单次初始化应只产生一个日志文件");
            return files[0];
        }

        // ==================== LoggingSetup 初始化 ====================

        [Fact]
        public void Initialize_应创建日志文件并写入内容()
        {
            LoggingSetup.Initialize(CreateConfig(), _testLogDir);

            Serilog.Log.Logger.Information("{Msg}", "初始化写入测试");

            LoggingSetup.Shutdown();

            var content = File.ReadAllText(GetSingleLogFile());
            content.Should().Contain("初始化写入测试");
        }

        [Fact]
        public void Initialize_重复初始化应释放旧文件句柄()
        {
            LoggingSetup.Initialize(CreateConfig(), _testLogDir);
            Serilog.Log.Logger.Information("{Msg}", "首次初始化写入");
            var firstLogFile = GetSingleLogFile();

            // 第二次初始化应先释放旧实例的文件句柄
            LoggingSetup.Initialize(CreateConfig(), _testLogDir);

            // 旧文件不再被锁定，可正常删除（原 P0：Dispose 释放目标错误导致句柄泄漏）
            var act = () => File.Delete(firstLogFile);
            act.Should().NotThrow("重复初始化后旧文件句柄应已释放");

            LoggingSetup.Shutdown();
        }

        [Fact]
        public void Shutdown_后日志文件应可正常读取()
        {
            LoggingSetup.Initialize(CreateConfig(), _testLogDir);
            Serilog.Log.Logger.Information("{Msg}", "关闭前写入");

            LoggingSetup.Shutdown();

            // 关闭后文件句柄应完全释放（原已知失败测试的核心场景）
            var act = () => File.ReadAllText(GetSingleLogFile());
            act.Should().NotThrow("Shutdown 后文件不应再被锁定");
        }

        [Fact]
        public void 日志消息含大括号应作为字面量安全输出()
        {
            LoggingSetup.Initialize(CreateConfig(), _testLogDir);

            // 消息中的非法占位符样式内容不应触发 FormatException（原 P0：模板注入）
            Serilog.Log.Logger.Information("{Msg}", "配置内容 { NotAPlaceholder } 与 {0}");

            LoggingSetup.Shutdown();

            var content = File.ReadAllText(GetSingleLogFile());
            content.Should().Contain("配置内容 { NotAPlaceholder } 与 {0}");
        }

        [Fact]
        public void Initialize_应清理过期日志文件()
        {
            // 创建一个远超保留期的旧日志文件
            var expiredFile = Path.Combine(_testLogDir, "RTT_log-2020-01-01-00-00-00.log");
            File.WriteAllText(expiredFile, "过期日志");

            LoggingSetup.Initialize(CreateConfig(retainedDays: 7), _testLogDir);

            File.Exists(expiredFile).Should().BeFalse("超过保留天数的日志文件应被清理");

            LoggingSetup.Shutdown();
        }

        [Fact]
        public void AppLog_For返回的日志器应输出到当前管道()
        {
            LoggingSetup.Initialize(CreateConfig(), _testLogDir);

            AppLog.For<LogServiceTests>().LogInformation("非DI场景日志输出");

            LoggingSetup.Shutdown();

            var content = File.ReadAllText(GetSingleLogFile());
            content.Should().Contain("非DI场景日志输出");
            content.Should().Contain(nameof(LogServiceTests), "来源应自动取泛型类名");
        }

        // ==================== InMemoryLogSink 内存缓冲 ====================

        [Fact]
        public void 内存缓冲应记录流经管道的日志()
        {
            LoggingSetup.Initialize(CreateConfig(), _testLogDir);

            LoggingSetup.InMemorySink.Clear();
            Serilog.Log.Logger.Information("{Msg}", "内存缓冲测试");

            var entries = LoggingSetup.InMemorySink.GetRecentEntries();
            entries.Should().Contain(e => e.Message.Contains("内存缓冲测试"));

            LoggingSetup.Shutdown();
        }

        [Fact]
        public void 内存缓冲新增日志应触发事件()
        {
            LoggingSetup.Initialize(CreateConfig(), _testLogDir);
            LoggingSetup.InMemorySink.Clear();

            LogEntryItem? received = null;
            var resetEvent = new AutoResetEvent(false);
            void Handler(LogEntryItem entry)
            {
                received = entry;
                resetEvent.Set();
            }

            LoggingSetup.InMemorySink.LogEntryAdded += Handler;
            try
            {
                Serilog.Log.Logger.Warning("{Msg}", "事件触发测试");
                resetEvent.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue("应在新日志写入后触发事件");
                received.Should().NotBeNull();
                received!.Message.Should().Contain("事件触发测试");
                received.Level.Should().Be(ReTime_Testing.Models.LogLevel.WRN);
            }
            finally
            {
                LoggingSetup.InMemorySink.LogEntryAdded -= Handler;
            }

            LoggingSetup.Shutdown();
        }

        [Fact]
        public void 内存缓冲超出上限应自动裁剪()
        {
            LoggingSetup.Initialize(CreateConfig(), _testLogDir);
            LoggingSetup.InMemorySink.Clear();

            for (var i = 0; i < InMemoryLogSink.MaxLogBufferCount + 100; i++)
            {
                Serilog.Log.Logger.Information("{Msg}", i);
            }

            LoggingSetup.InMemorySink.GetRecentEntries().Should()
                .HaveCount(InMemoryLogSink.MaxLogBufferCount, "缓冲应保持在上限内");

            LoggingSetup.Shutdown();
        }

        [Fact]
        public void Clear应清空内存缓冲()
        {
            LoggingSetup.Initialize(CreateConfig(), _testLogDir);
            Serilog.Log.Logger.Information("{Msg}", "待清除的日志");

            LoggingSetup.InMemorySink.Clear();

            LoggingSetup.InMemorySink.GetRecentEntries().Should().BeEmpty("清空后缓冲应为空");

            LoggingSetup.Shutdown();
        }
    }
}
