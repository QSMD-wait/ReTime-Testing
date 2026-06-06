using System.IO;
using ReTime_Testing.Models;

namespace ReTime_Testing.Tests.Services
{
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
            try
            {
                if (Directory.Exists(_testLogDir))
                    Directory.Delete(_testLogDir, true);
            }
            catch
            {
                // 清理失败不影响测试
            }
        }

        [Fact]
        public void Instance_在未初始化前应返回null()
        {
            // 反射重置单例状态（测试隔离）
            ResetSerilogInstance();

            var instance = SerilogLogService.Instance;

            instance.Should().BeNull();
        }

        [Fact]
        public void Initialize_应正确设置单例实例()
        {
            ResetSerilogInstance();

            var config = new LogServiceConfiguration(
                new LogConfig { EnableFileOutput = true, MinimumLevel = LogLevel.DBG },
                _testLogDir);

            SerilogLogService.Initialize(config);

            SerilogLogService.Instance.Should().NotBeNull();
        }

        [Fact]
        public void Initialize_重新初始化应覆盖旧实例()
        {
            ResetSerilogInstance();

            var config1 = new LogServiceConfiguration(
                new LogConfig { EnableFileOutput = true, MinimumLevel = LogLevel.INF },
                _testLogDir);
            SerilogLogService.Initialize(config1);
            var instance1 = SerilogLogService.Instance;

            var config2 = new LogServiceConfiguration(
                new LogConfig { EnableFileOutput = true, MinimumLevel = LogLevel.DBG },
                _testLogDir);
            SerilogLogService.Initialize(config2);
            var instance2 = SerilogLogService.Instance;

            instance1.Should().NotBeNull();
            instance2.Should().NotBeNull();
            instance1.Should().NotBeSameAs(instance2);
        }

        [Fact]
        public void LogServiceConfiguration_默认构造应使用绝对路径()
        {
            var config = new LogServiceConfiguration();

            config.EnableFileOutput.Should().BeTrue();
            config.MinimumLevel.Should().Be(LogLevel.INF);
            config.RetainedFileCountLimit.Should().Be(30);
            config.FileSizeLimitBytes.Should().Be(10 * 1024L * 1024L);
            Path.IsPathRooted(config.LogDirectory).Should().BeTrue();
        }

        [Fact]
        public void LogServiceConfiguration_自定义构造应正确应用参数()
        {
            var logConfig = new LogConfig
            {
                EnableFileOutput = false,
                MinimumLevel = LogLevel.DBG,
                RetainedDays = 7,
                FileSizeLimitMB = 5
            };

            var config = new LogServiceConfiguration(logConfig, _testLogDir);

            config.EnableFileOutput.Should().BeFalse();
            config.MinimumLevel.Should().Be(LogLevel.DBG);
            config.LogDirectory.Should().Be(_testLogDir);
            config.RetainedFileCountLimit.Should().Be(7);
            config.FileSizeLimitBytes.Should().Be(5 * 1024L * 1024L);
        }

        [Fact]
        public void LogServiceConfiguration_保留天数至少为1()
        {
            var logConfig = new LogConfig { RetainedDays = 0 };

            var config = new LogServiceConfiguration(logConfig, _testLogDir);

            config.RetainedFileCountLimit.Should().Be(1);
        }

        [Fact]
        public void LogServiceConfiguration_文件大小限制至少为1MB()
        {
            var logConfig = new LogConfig { FileSizeLimitMB = 0 };

            var config = new LogServiceConfiguration(logConfig, _testLogDir);

            config.FileSizeLimitBytes.Should().Be(1 * 1024L * 1024L);
        }

        [Fact]
        public void LogServiceConfiguration_null参数应抛出异常()
        {
            Action act = () => new LogServiceConfiguration(null!, _testLogDir);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void LogServiceConfiguration_空路径应抛出异常()
        {
            Action act = () => new LogServiceConfiguration(new LogConfig(), "");
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void LogLevel枚举值应包含五个等级()
        {
            Enum.GetValues<LogLevel>().Should().HaveCount(5);
            Enum.GetValues<LogLevel>().Should().Contain([LogLevel.TRC, LogLevel.DBG, LogLevel.INF, LogLevel.WRN, LogLevel.ERR]);
        }

        [Fact]
        public void LogConfig默认值应正确()
        {
            var config = new LogConfig();

            config.EnableFileOutput.Should().BeTrue();
            config.MinimumLevel.Should().Be(LogLevel.INF);
            config.RetainedDays.Should().Be(30);
            config.FileSizeLimitMB.Should().Be(10);
        }

        [Fact]
        public void SerilogLogService_应实现ILogService接口()
        {
            ResetSerilogInstance();

            var config = new LogServiceConfiguration(
                new LogConfig { EnableFileOutput = false },
                _testLogDir);
            SerilogLogService.Initialize(config);

            SerilogLogService.Instance.Should().BeAssignableTo<ILogService>();
        }

        [Fact]
        public void SerilogLogService_应实现IDisposable接口()
        {
            ResetSerilogInstance();

            var config = new LogServiceConfiguration(
                new LogConfig { EnableFileOutput = false },
                _testLogDir);
            SerilogLogService.Initialize(config);

            SerilogLogService.Instance.Should().BeAssignableTo<IDisposable>();
        }

        [Fact]
        public void Dispose_多次调用应安全()
        {
            ResetSerilogInstance();

            var config = new LogServiceConfiguration(
                new LogConfig { EnableFileOutput = false },
                _testLogDir);
            SerilogLogService.Initialize(config);

            var instance = SerilogLogService.Instance;
            instance.Should().NotBeNull();

            instance!.Dispose();
            instance.Dispose();
            instance.Dispose();
        }

        [Fact]
        public void 文件日志输出应创建日志文件()
        {
            ResetSerilogInstance();

            var config = new LogServiceConfiguration(
                new LogConfig { EnableFileOutput = true, MinimumLevel = LogLevel.INF },
                _testLogDir);
            SerilogLogService.Initialize(config);

            var instance = SerilogLogService.Instance;
            instance.Should().NotBeNull();

            instance!.Info("TestModule", "测试日志消息");

            var logFiles = Directory.GetFiles(_testLogDir, "RTT_log-*.log");
            logFiles.Should().NotBeEmpty();
        }

        [Fact]
        public void 禁用文件输出应不创建日志文件()
        {
            ResetSerilogInstance();

            var config = new LogServiceConfiguration(
                new LogConfig { EnableFileOutput = false, MinimumLevel = LogLevel.INF },
                _testLogDir);
            SerilogLogService.Initialize(config);

            var instance = SerilogLogService.Instance;
            instance.Should().NotBeNull();

            instance!.Info("TestModule", "测试日志消息");

            var logFiles = Directory.GetFiles(_testLogDir, "RTT_log-*.log");
            logFiles.Should().BeEmpty();
        }

        [Fact]
        public void Logger_Serilog就绪前应缓存日志()
        {
            ResetSerilogInstance();

            Logger.Info("TestModule", "就绪前日志1");
            Logger.Warn("TestModule", "就绪前日志2");
            Logger.Debug("TestModule", "就绪前日志3");

            var config = new LogServiceConfiguration(
                new LogConfig { EnableFileOutput = true, MinimumLevel = LogLevel.DBG },
                _testLogDir);
            SerilogLogService.Initialize(config);
            Logger.OnSerilogReady();

            var logFiles = Directory.GetFiles(_testLogDir, "RTT_log-*.log");
            logFiles.Should().NotBeEmpty();

            SerilogLogService.Instance?.Dispose();

            var content = File.ReadAllText(logFiles[0]);
            content.Should().Contain("就绪前日志1");
            content.Should().Contain("就绪前日志2");
            content.Should().Contain("就绪前日志3");
        }

        [Fact]
        public void Logger_Serilog就绪后应直接写入()
        {
            ResetSerilogInstance();

            var config = new LogServiceConfiguration(
                new LogConfig { EnableFileOutput = true, MinimumLevel = LogLevel.INF },
                _testLogDir);
            SerilogLogService.Initialize(config);
            Logger.OnSerilogReady();

            Logger.Info("TestModule", "就绪后日志");

            var logFiles = Directory.GetFiles(_testLogDir, "RTT_log-*.log");
            logFiles.Should().NotBeEmpty();

            SerilogLogService.Instance?.Dispose();

            var content = File.ReadAllText(logFiles[0]);
            content.Should().Contain("就绪后日志");
        }

        private static void ResetSerilogInstance()
        {
            var instance = SerilogLogService.Instance;
            instance?.Dispose();

            var field = typeof(SerilogLogService).GetField("_instance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }
    }
}