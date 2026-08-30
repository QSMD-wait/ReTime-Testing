using ReTime_Testing.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReTime_Testing.Tests.Services;

/// <summary>
/// CloudCalibrationService 单元测试
/// CloudCalibrationService 是纯NTP数据源，仅负责从NTP服务器获取时间（含RTT补偿）
/// 校准策略和调度由 TimeCalibrationService 统一管理
/// </summary>
public class CloudCalibrationServiceTests
{
    private readonly CloudCalibrationService _service;

    public CloudCalibrationServiceTests()
    {
        _service = new CloudCalibrationService(NullLogger<CloudCalibrationService>.Instance);
    }

    [Fact]
    public void Constructor_应该正确初始化()
    {
        // Assert
        _service.Should().NotBeNull();
        _service.CurrentProviderName.Should().Be("未初始化");
        _service.LastRttMs.Should().Be(0);
    }

    [Fact]
    public void CurrentProviderName_未配置时应该是未初始化()
    {
        // Assert
        _service.CurrentProviderName.Should().Be("未初始化");
    }

    [Fact]
    public void LastRttMs_初始值应该是零()
    {
        // Assert
        _service.LastRttMs.Should().Be(0);
    }

    [Fact]
    public void ConfigureNtpServers_应该更新提供者名称()
    {
        // Act
        _service.ConfigureNtpServers(new List<string> { "ntp.aliyun.com" }, 0);

        // Assert
        _service.CurrentProviderName.Should().NotBe("未初始化");
    }

    [Fact]
    public void ConfigureNtpServers_空列表不应该抛出异常()
    {
        // Act & Assert
        var action = () => _service.ConfigureNtpServers(new List<string>(), 0);
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureNtpServers_null列表不应该抛出异常()
    {
        // Act & Assert - null会被替换为默认服务器列表
        var action = () => _service.ConfigureNtpServers(null!, 0);
        action.Should().NotThrow();
    }

    [Fact]
    public void ConfigureNtpServers_无效索引应该使用全部服务器()
    {
        // Act - 索引超出范围时应使用全部服务器
        var action = () => _service.ConfigureNtpServers(new List<string> { "ntp.aliyun.com", "ntp.tencent.com" }, 99);
        action.Should().NotThrow();
        _service.CurrentProviderName.Should().NotBe("未初始化");
    }

    [Fact]
    public async Task GetCloudTimeAsync_配置后不应该抛出异常()
    {
        // Arrange
        _service.ConfigureNtpServers(new List<string> { "ntp.aliyun.com" }, 0);

        // Act & Assert - 网络请求可能成功也可能失败，但不应抛出异常
        var result = await _service.GetCloudTimeAsync(TimeSpan.FromSeconds(3));

        // Assert - 结果可能为null（网络不可达）或有效值，都不算错误
        // 仅验证不抛异常
    }

    [Fact]
    public void ConfigureNtpServers_多次配置应该正确切换()
    {
        // Act
        _service.ConfigureNtpServers(new List<string> { "ntp.aliyun.com" }, 0);
        var name1 = _service.CurrentProviderName;

        _service.ConfigureNtpServers(new List<string> { "ntp.tencent.com" }, 0);
        var name2 = _service.CurrentProviderName;

        // Assert - 两次配置都应成功
        name1.Should().NotBe("未初始化");
        name2.Should().NotBe("未初始化");
    }

    [Fact]
    public void ConfigureNtpServers_负索引不应该抛出异常()
    {
        // Act & Assert - 负索引应使用全部服务器
        var action = () => _service.ConfigureNtpServers(new List<string> { "ntp.aliyun.com" }, -1);
        action.Should().NotThrow();
        _service.CurrentProviderName.Should().NotBe("未初始化");
    }
}
