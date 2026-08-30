using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Tests.ViewModels;

/// <summary>
/// TimeTopDesktopViewModel 阴影逻辑单元测试（流畅优化）
/// </summary>
public class TimeTopDesktopViewModelTests
{
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly GlobalSetting _globalSetting;
    private readonly TimeTopSetting _timeTopSetting;
    private readonly ProgressStateManager _stateManager;
    private readonly GlobalTimeTopDesktopService _globalService;

    public TimeTopDesktopViewModelTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();

        _globalSetting = new GlobalSetting();
        _globalSetting.Basic.SmoothnessOptimization = false;

        _timeTopSetting = new TimeTopSetting();
        _timeTopSetting.ProgressBar.EnableShadow = true;

        _mockSettingsService.Setup(x => x.GetGlobalSetting()).Returns(_globalSetting);
        _mockSettingsService.Setup(x => x.GetTimeTopSetting()).Returns(_timeTopSetting);

        _stateManager = new ProgressStateManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<ProgressStateManager>.Instance);
        _globalService = new GlobalTimeTopDesktopService(_stateManager, Mock.Of<ILogger<GlobalTimeTopDesktopService>>());
    }

    private TimeTopDesktopViewModel CreateViewModel()
    {
        return new TimeTopDesktopViewModel(Microsoft.Extensions.Logging.Abstractions.NullLogger<TimeTopDesktopViewModel>.Instance, _globalService, _mockSettingsService.Object);
    }

    [Fact]
    public void SmoothnessOptimization_默认应关闭()
    {
        // Assert
        new GlobalSetting().Basic.SmoothnessOptimization.Should().BeFalse();
    }

    [Fact]
    public void 流畅优化开启_Loading状态应禁用阴影()
    {
        // Arrange
        _globalSetting.Basic.SmoothnessOptimization = true;
        _timeTopSetting.ProgressBar.EnableShadow = true;
        var vm = CreateViewModel();

        // Act
        _globalService.SetLoading();

        // Assert
        vm.EnableShadow.Should().BeFalse();
    }

    [Fact]
    public void 流畅优化关闭_Loading状态应遵循全局配置()
    {
        // Arrange
        _timeTopSetting.ProgressBar.EnableShadow = true;
        var vm = CreateViewModel();

        // Act
        _globalService.SetLoading();

        // Assert
        vm.EnableShadow.Should().BeTrue();
    }

    [Fact]
    public void 流畅优化关闭_全局阴影关闭_Loading状态应无阴影()
    {
        // Arrange
        _timeTopSetting.ProgressBar.EnableShadow = false;
        var vm = CreateViewModel();

        // Act
        _globalService.SetLoading();

        // Assert
        vm.EnableShadow.Should().BeFalse();
    }

    [Fact]
    public void 流畅优化开启_非Loading状态应遵循全局配置()
    {
        // Arrange
        _globalSetting.Basic.SmoothnessOptimization = true;
        _timeTopSetting.ProgressBar.EnableShadow = true;
        var vm = CreateViewModel();

        // Act
        _globalService.SetProgress(50);

        // Assert
        vm.EnableShadow.Should().BeTrue();
    }

    [Fact]
    public void 引导强制开启_无视配置文件禁用Loading阴影()
    {
        // Arrange
        _globalService.ForceSmoothnessOptimization = true;
        _globalSetting.Basic.SmoothnessOptimization = false;
        _timeTopSetting.ProgressBar.EnableShadow = true;
        var vm = CreateViewModel();

        // Act
        _globalService.SetLoading();

        // Assert
        vm.EnableShadow.Should().BeFalse();
    }

    [Fact]
    public void 全局配置变更_应即时重算阴影()
    {
        // Arrange
        var vm = CreateViewModel();
        _globalService.SetLoading();
        vm.EnableShadow.Should().BeTrue();

        // Act（模拟设置页开启流畅优化并保存）
        _globalSetting.Basic.SmoothnessOptimization = true;
        _mockSettingsService.Raise(x => x.OnGlobalSettingChanged += null, _globalSetting);

        // Assert
        vm.EnableShadow.Should().BeFalse();
    }
}