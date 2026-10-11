using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Tests.ViewModels;

/// <summary>
/// 引导期间配置不落盘守护测试（真实 SettingsService + 临时目录）
/// 不变量：引导全程不产生任何配置文件，Finish 是配置文件的唯一落盘点；
/// 该测试锁死"引导中断不残留半套配置、下次启动引导可重新出现"的可恢复性
/// </summary>
public class WelcomeOnboardingPersistenceGuardTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _settingsService;

    private string GlobalSettingPath => Path.Combine(_tempDir, "Setting.json");
    private string TimeTopSettingPath => Path.Combine(_tempDir, "TimeTopSetting.json");

    public WelcomeOnboardingPersistenceGuardTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "RTT_OnboardingGuardTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var mockConfigManager = new Mock<IConfigurationManager>();
        mockConfigManager.Setup(x => x.GlobalSettingFilePath).Returns(GlobalSettingPath);
        mockConfigManager.Setup(x => x.TimeTopSettingFilePath).Returns(TimeTopSettingPath);

        _settingsService = new SettingsService(
            mockConfigManager.Object,
            NullLogger<SettingsService>.Instance);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // 临时目录清理失败忽略
        }
    }

    [Fact]
    public void 引导全程配置仅驻内存_Finish后才创建配置文件()
    {
        // Act：模拟引导入口——挂起持久化（对应 AppBootstrapper.PrepareWelcomeEnvironment，
        // 真实流程中先于向导窗口/ViewModel 创建），随后构造向导（读取现有配置，此时文件不存在）
        _settingsService.SetPersistenceSuspended(true);
        var vm = new WelcomeViewModel(
            NullLogger<WelcomeViewModel>.Instance,
            _settingsService,
            new Mock<IThemeService>().Object,
            new Mock<IDesktopWindowManager>().Object,
            new Mock<IAutoStartService>().Object);

        // Assert：向导构造不创建配置文件（首启判定信号不被破坏）
        File.Exists(GlobalSettingPath).Should().BeFalse();
        File.Exists(TimeTopSettingPath).Should().BeFalse();

        // Act：走完引导中全部可调设置项（触发各类实时保存）
        vm.SelectedTheme = "dark";
        vm.SelectedPosition = "right";
        vm.EnableAutoStart = true;
        vm.EnableSmoothness = true;
        vm.ProgressScale = 2.0;
        vm.EnableProgressShadow = false;
        vm.EnableTextOverlay = false;
        vm.TextFontSize = 20;
        vm.SelectedTextEffect = "outline";
        vm.EnableCalibration = false;

        // Assert：引导全程两个配置文件都不应存在
        File.Exists(GlobalSettingPath).Should().BeFalse("引导期间配置仅驻内存，中断后不应残留");
        File.Exists(TimeTopSettingPath).Should().BeFalse("引导期间配置仅驻内存，中断后不应残留");

        // 实时预览依赖的缓存内容应已生效
        _settingsService.GetGlobalSetting().Basic.SmoothnessOptimization.Should().BeTrue();
        _settingsService.GetTimeTopSetting().ProgressBar.Scale.Should().Be(2.0);

        // Act：完成引导（Finish 内部恢复持久化并统一落盘）
        vm.FinishCommand.Execute(null);

        // Assert：Finish 是配置文件的唯一落盘点
        File.Exists(GlobalSettingPath).Should().BeTrue();
        File.Exists(TimeTopSettingPath).Should().BeTrue();

        var savedGlobal = _settingsService.GetGlobalSetting();
        savedGlobal.Basic.WelcomeShowed.Should().BeTrue();
        savedGlobal.Basic.Theme.Should().Be("dark");
        savedGlobal.Basic.SmoothnessOptimization.Should().BeTrue();

        var savedTimeTop = _settingsService.GetTimeTopSetting();
        savedTimeTop.ProgressBar.Position.Should().Be("right");
        savedTimeTop.ProgressBar.Scale.Should().Be(2.0);
        savedTimeTop.Calibration.Enabled.Should().BeFalse();

        vm.IsCompleted.Should().BeTrue();
    }
}
