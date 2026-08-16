using ReTime_Testing.Services.Onboarding;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Tests.ViewModels;

/// <summary>
/// OnboardingFlow 首次启动判定单元测试
/// </summary>
public class OnboardingFlowTests
{
    [Fact]
    public void ShouldShowWelcome_配置文件存在时应无视WelcomeShowed不显示()
    {
        // Arrange / Act / Assert
        OnboardingFlow.ShouldShowWelcome(true, false, false).Should().BeFalse();
        OnboardingFlow.ShouldShowWelcome(true, true, false).Should().BeFalse();
    }

    [Fact]
    public void ShouldShowWelcome_配置文件不存在且未完成时应显示()
    {
        OnboardingFlow.ShouldShowWelcome(false, false, false).Should().BeTrue();
    }

    [Fact]
    public void ShouldShowWelcome_配置文件不存在但已完成时不应显示()
    {
        OnboardingFlow.ShouldShowWelcome(false, true, false).Should().BeFalse();
    }

    [Fact]
    public void ShouldShowWelcome_强制显示开关应无视其他条件()
    {
        OnboardingFlow.ShouldShowWelcome(true, true, true).Should().BeTrue();
        OnboardingFlow.ShouldShowWelcome(false, true, true).Should().BeTrue();
        OnboardingFlow.ShouldShowWelcome(true, false, true).Should().BeTrue();
    }
}

/// <summary>
/// WelcomeViewModel 单元测试
/// </summary>
public class WelcomeViewModelTests
{
    private readonly Mock<ISettingsService> _mockSettingsService;
    private readonly Mock<IThemeService> _mockThemeService;
    private readonly Mock<IDesktopWindowManager> _mockDesktopWindowManager;
    private readonly Mock<IAutoStartService> _mockAutoStartService;
    private readonly GlobalSetting _globalSetting;
    private readonly TimeTopSetting _timeTopSetting;

    public WelcomeViewModelTests()
    {
        _mockSettingsService = new Mock<ISettingsService>();
        _mockThemeService = new Mock<IThemeService>();
        _mockDesktopWindowManager = new Mock<IDesktopWindowManager>();
        _mockAutoStartService = new Mock<IAutoStartService>();

        _globalSetting = new GlobalSetting();
        _globalSetting.Basic.Theme = "light";
        _globalSetting.Basic.AutoStart.Enabled = false;
        _globalSetting.Basic.WelcomeShowed = false;
        _globalSetting.Basic.ForceShowWelcome = false;

        _timeTopSetting = new TimeTopSetting();
        _timeTopSetting.ProgressBar.Position = "top";
        _timeTopSetting.Calibration.Enabled = true;

        _mockSettingsService.Setup(x => x.GetGlobalSetting()).Returns(_globalSetting);
        _mockSettingsService.Setup(x => x.GetTimeTopSetting()).Returns(_timeTopSetting);
    }

    private WelcomeViewModel CreateViewModel()
    {
        return new WelcomeViewModel(
            _mockSettingsService.Object,
            _mockThemeService.Object,
            _mockDesktopWindowManager.Object,
            _mockAutoStartService.Object);
    }

    [Fact]
    public void 构造函数_应从现有配置初始化默认值()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.SelectedTheme.Should().Be("light");
        vm.SelectedPosition.Should().Be("top");
        vm.EnableAutoStart.Should().BeFalse();
        vm.EnableCalibration.Should().BeTrue();
        vm.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void 初始状态_应在第一页且不可上一步()
    {
        // Act
        var vm = CreateViewModel();

        // Assert
        vm.CurrentIndex.Should().Be(0);
        vm.CanGoBack.Should().BeFalse();
        vm.CanGoNext.Should().BeTrue();
        vm.IsLastPage.Should().BeFalse();
        vm.StepText.Should().Be("步骤 1 / 6");
    }

    [Fact]
    public void 下一步到最后一步_应不可继续前进()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        for (int i = 0; i < 5; i++)
            vm.NextCommand.Execute(null);

        // Assert
        vm.CurrentIndex.Should().Be(5);
        vm.CanGoNext.Should().BeFalse();
        vm.IsLastPage.Should().BeTrue();
        vm.StepText.Should().Be("步骤 6 / 6");
    }

    [Fact]
    public void 最后一步继续下一步_应保持不动()
    {
        // Arrange
        var vm = CreateViewModel();
        for (int i = 0; i < 5; i++)
            vm.NextCommand.Execute(null);

        // Act
        vm.NextCommand.Execute(null);

        // Assert
        vm.CurrentIndex.Should().Be(5);
    }

    [Fact]
    public void 上一步_应回到上一页()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.NextCommand.Execute(null);

        // Act
        vm.BackCommand.Execute(null);

        // Assert
        vm.CurrentIndex.Should().Be(0);
        vm.CanGoBack.Should().BeFalse();
    }

    [Fact]
    public void 第一页上一步_应保持不动()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.BackCommand.Execute(null);

        // Assert
        vm.CurrentIndex.Should().Be(0);
    }

    [Fact]
    public void 切换主题_应即时应用主题并更新显示文本()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SelectedTheme = "dark";

        // Assert
        _mockThemeService.Verify(x => x.ApplyTheme("dark"), Times.Once);
        vm.SelectedThemeText.Should().Be("暗黑");
    }

    [Fact]
    public void 切换位置_应即时移动进度条窗口()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.SelectedPosition = "bottom";

        // Assert
        _mockDesktopWindowManager.Verify(x => x.SetPosition(ProgressBarPosition.Bottom), Times.Once);
        vm.SelectedPositionText.Should().Be("底部");
    }

    [Fact]
    public void 切换自启动_应即时启用或禁用()
    {
        // Arrange
        var vm = CreateViewModel();

        // Act
        vm.EnableAutoStart = true;

        // Assert
        _mockAutoStartService.Verify(x => x.Enable("registry"), Times.Once);
        vm.AutoStartText.Should().Be("开启");

        // Act
        vm.EnableAutoStart = false;

        // Assert
        _mockAutoStartService.Verify(x => x.Disable(), Times.Once);
        vm.AutoStartText.Should().Be("关闭");
    }

    [Fact]
    public void 完成引导_应保存全部设置并标记完成()
    {
        // Arrange
        var vm = CreateViewModel();
        vm.SelectedTheme = "dark";
        vm.SelectedPosition = "right";
        vm.EnableAutoStart = true;
        vm.EnableCalibration = false;

        GlobalSetting? savedGlobal = null;
        TimeTopSetting? savedTimeTop = null;
        _mockSettingsService.Setup(x => x.SaveGlobalSetting(It.IsAny<GlobalSetting>()))
            .Callback<GlobalSetting>(g => savedGlobal = g);
        _mockSettingsService.Setup(x => x.SaveTimeTopSetting(It.IsAny<TimeTopSetting>()))
            .Callback<TimeTopSetting>(t => savedTimeTop = t);

        // Act
        vm.FinishCommand.Execute(null);

        // Assert
        savedGlobal.Should().NotBeNull();
        savedGlobal!.Basic.Theme.Should().Be("dark");
        savedGlobal.Basic.AutoStart.Enabled.Should().BeTrue();
        savedGlobal.Basic.WelcomeShowed.Should().BeTrue();
        savedGlobal.Basic.ForceShowWelcome.Should().BeFalse();

        savedTimeTop.Should().NotBeNull();
        savedTimeTop!.ProgressBar.Position.Should().Be("right");
        savedTimeTop.Calibration.Enabled.Should().BeFalse();

        vm.IsCompleted.Should().BeTrue();
    }
}