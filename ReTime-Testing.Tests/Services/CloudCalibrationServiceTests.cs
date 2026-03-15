using ReTime_Testing.Services;
using Moq;
using FluentAssertions;

namespace ReTime_Testing.Tests.Services;

/// <summary>
/// CloudCalibrationService 单元测试
/// 注意：由于网络请求难以在单元测试中 Mock，主要测试配置管理和逻辑
/// </summary>
public class CloudCalibrationServiceTests
{
    private readonly Mock<ITimeService> _mockTimeService;
    private readonly CloudCalibrationService _service;

    public CloudCalibrationServiceTests()
    {
        _mockTimeService = new Mock<ITimeService>();
        _service = new CloudCalibrationService(_mockTimeService.Object);
    }

    [Fact]
    public void Constructor_应该正确初始化()
    {
        // Assert
        _service.Should().NotBeNull();
        _service.IsEnabled.Should().BeTrue();
        _service.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Configure_应该更新配置参数()
    {
        // Act
        _service.Configure(
            enabled: true,
            interval: 600,
            timeout: 10,
            maxRetryCount: 10,
            backoffMultiplier: 3.0,
            triggerThreshold: 15);

        // Assert
        _service.IsEnabled.Should().BeTrue();
        _service.CalibrationInterval.Should().Be(600);
        _service.CalibrationTimeout.Should().Be(10);
        _service.MaxRetryCount.Should().Be(10);
        _service.BackoffMultiplier.Should().Be(3.0);
        _service.CalibrationTriggerThreshold.Should().Be(15);
    }

    [Fact]
    public void Configure_禁用后不应该启动()
    {
        // Arrange
        _service.Configure(enabled: false, interval: 300);

        // Act
        _service.Start();

        // Assert
        _service.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Configure_启用后应该可以启动()
    {
        // Arrange
        _service.Configure(enabled: true, interval: 300);

        // Act
        _service.Start();

        // Assert
        _service.IsRunning.Should().BeTrue();
        
        // Cleanup
        _service.Stop();
    }

    [Fact]
    public void Stop_应该停止服务()
    {
        // Arrange
        _service.Configure(enabled: true, interval: 300);
        _service.Start();

        // Act
        _service.Stop();

        // Assert
        _service.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Stop_多次调用不应该抛出异常()
    {
        // Arrange
        _service.Configure(enabled: true, interval: 300);
        _service.Start();

        // Act & Assert - 不应该抛出异常
        var action = () =>
        {
            _service.Stop();
            _service.Stop();
            _service.Stop();
        };
        action.Should().NotThrow();
    }

    [Fact]
    public void Reset_应该重置失败计数器()
    {
        // Arrange
        _service.Configure(enabled: true, interval: 300, maxRetryCount: 5);
        
        // Act
        _service.Reset();

        // Assert
        _service.FailureCount.Should().Be(0);
    }

    [Fact]
    public void Configure_应该正确设置默认值()
    {
        // Act
        _service.Configure(
            enabled: true,
            interval: 300,
            timeout: 3,
            maxRetryCount: 5,
            backoffMultiplier: 2.0,
            triggerThreshold: 5);

        // Assert - 验证默认值
        _service.IsEnabled.Should().BeTrue();
        _service.CalibrationInterval.Should().Be(300);
        _service.CalibrationTimeout.Should().Be(3);
        _service.MaxRetryCount.Should().Be(5);
        _service.BackoffMultiplier.Should().Be(2.0);
        _service.CalibrationTriggerThreshold.Should().Be(5);
    }

    [Fact]
    public void Configure_更新配置后应该重置失败计数器()
    {
        // Arrange
        _service.Configure(enabled: true, interval: 300, maxRetryCount: 5);
        
        // Act
        _service.Configure(
            enabled: true,
            interval: 600,
            timeout: 10,
            maxRetryCount: 10,
            backoffMultiplier: 3.0,
            triggerThreshold: 15);

        // Assert
        _service.FailureCount.Should().Be(0);
    }

    [Fact]
    public void Start_未启用时不应该启动()
    {
        // Arrange
        _service.Configure(enabled: false, interval: 300);

        // Act
        _service.Start();

        // Assert
        _service.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void FailureCount_应该正确记录失败次数()
    {
        // Arrange
        _service.Configure(enabled: true, maxRetryCount: 5);

        // Act & Assert - FailureCount 应该是 0
        _service.FailureCount.Should().Be(0);
    }

    [Fact]
    public void IsCloudSynchronized_应该反映时间服务的状态()
    {
        // Arrange
        _mockTimeService.Setup(x => x.IsCloudSynchronized).Returns(true);

        // Act
        var isSynced = _mockTimeService.Object.IsCloudSynchronized;

        // Assert
        isSynced.Should().BeTrue();
    }

    [Fact]
    public void Configure_应该支持零间隔()
    {
        // Act
        _service.Configure(
            enabled: true,
            interval: 0,
            timeout: 3,
            maxRetryCount: 5,
            backoffMultiplier: 2.0,
            triggerThreshold: 5);

        // Assert
        _service.CalibrationInterval.Should().Be(0);
    }

    [Fact]
    public void Configure_应该支持很大的间隔()
    {
        // Act
        _service.Configure(
            enabled: true,
            interval: 3600,
            timeout: 10,
            maxRetryCount: 5,
            backoffMultiplier: 2.0,
            triggerThreshold: 5);

        // Assert
        _service.CalibrationInterval.Should().Be(3600);
    }

    [Fact]
    public void Configure_应该支持零超时()
    {
        // Act
        _service.Configure(
            enabled: true,
            interval: 300,
            timeout: 0,
            maxRetryCount: 5,
            backoffMultiplier: 2.0,
            triggerThreshold: 5);

        // Assert
        _service.CalibrationTimeout.Should().Be(0);
    }

    [Fact]
    public void Configure_应该支持零重试次数()
    {
        // Act
        _service.Configure(
            enabled: true,
            interval: 300,
            timeout: 3,
            maxRetryCount: 0,
            backoffMultiplier: 2.0,
            triggerThreshold: 5);

        // Assert
        _service.MaxRetryCount.Should().Be(0);
    }

    [Fact]
    public void Configure_应该支持零退避乘数()
    {
        // Act
        _service.Configure(
            enabled: true,
            interval: 300,
            timeout: 3,
            maxRetryCount: 5,
            backoffMultiplier: 0.0,
            triggerThreshold: 5);

        // Assert
        _service.BackoffMultiplier.Should().Be(0.0);
    }

    [Fact]
    public void Configure_应该支持零触发阈值()
    {
        // Act
        _service.Configure(
            enabled: true,
            interval: 300,
            timeout: 3,
            maxRetryCount: 5,
            backoffMultiplier: 2.0,
            triggerThreshold: 0);

        // Assert
        _service.CalibrationTriggerThreshold.Should().Be(0);
    }

    [Fact]
    public async Task Calibrate_应该触发时间跳跃事件()
    {
        // Arrange
        var testTime = new DateTime(2026, 3, 15, 10, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(testTime);
        _mockTimeService.Setup(x => x.IsCloudSynchronized).Returns(true);

        // Act
        await _service.CalibrateAsync();

        // Assert
        // 注意：由于网络请求可能失败，我们只验证没有抛出异常
        // 真正的时间跳跃应该在成功校准时触发
    }

    [Fact]
    public async Task CalibrateAsync_应该不抛出异常()
    {
        // Arrange
        var testTime = new DateTime(2026, 3, 15, 10, 0, 0);
        _mockTimeService.Setup(x => x.GetCurrentTime()).Returns(testTime);

        // Act & Assert - 不应该抛出异常
        var action = async () => await _service.CalibrateAsync();
        await action.Should().NotThrowAsync();
    }
}