using System.IO;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReTime_Testing.Tests.Services;

/// <summary>
/// SettingsService 单元测试（真实文件 I/O，临时目录）
/// 重点：全局配置加载绝不自动创建文件（文件存在性参与首启引导判定）；持久化挂起期间 Save 不落盘
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _settingsService;

    private string GlobalSettingPath => Path.Combine(_tempDir, "Setting.json");
    private string TimeTopSettingPath => Path.Combine(_tempDir, "TimeTopSetting.json");

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "RTT_SettingsTest_" + Guid.NewGuid().ToString("N"));
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
    public void GetGlobalSetting_文件不存在时应返回默认配置且不创建文件()
    {
        // Act
        var setting = _settingsService.GetGlobalSetting();

        // Assert
        setting.Basic.Theme.Should().Be("light");
        setting.Basic.WelcomeShowed.Should().BeFalse();
        File.Exists(GlobalSettingPath).Should().BeFalse("全局配置文件的创建是首次启动引导判定的信号，加载阶段绝不允许写盘");
    }

    [Fact]
    public void GetGlobalSetting_文件为空时应返回默认配置且不覆盖原文件()
    {
        // Arrange
        File.WriteAllText(GlobalSettingPath, string.Empty);

        // Act
        var setting = _settingsService.GetGlobalSetting();

        // Assert
        setting.Basic.Theme.Should().Be("light");
        File.ReadAllText(GlobalSettingPath).Should().BeEmpty("加载失败/为空时不覆盖原文件");
    }

    [Fact]
    public void SaveGlobalSetting_持久化挂起时应更新缓存与事件但不写盘()
    {
        // Arrange
        var eventRaised = false;
        _settingsService.OnGlobalSettingChanged += _ => eventRaised = true;
        _settingsService.SetPersistenceSuspended(true);

        // Act
        var setting = new GlobalSetting();
        setting.Basic.Theme = "dark";
        _settingsService.SaveGlobalSetting(setting);

        // Assert：仅内存生效，实时预览依赖的缓存与事件都不受影响
        File.Exists(GlobalSettingPath).Should().BeFalse();
        _settingsService.GetGlobalSetting().Basic.Theme.Should().Be("dark");
        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void SaveGlobalSetting_恢复持久化挂起后应正常落盘()
    {
        // Arrange
        _settingsService.SetPersistenceSuspended(true);
        _settingsService.SaveGlobalSetting(new GlobalSetting());
        File.Exists(GlobalSettingPath).Should().BeFalse();

        // Act
        _settingsService.SetPersistenceSuspended(false);
        var setting = new GlobalSetting();
        setting.Basic.Theme = "dark";
        _settingsService.SaveGlobalSetting(setting);

        // Assert
        File.Exists(GlobalSettingPath).Should().BeTrue();
    }

    [Fact]
    public void SaveTimeTopSetting_持久化挂起时应更新缓存但不写盘()
    {
        // Arrange
        _settingsService.SetPersistenceSuspended(true);

        // Act：挂起状态下加载（文件缺失触发的自动创建同样被挂起拦截）并保存
        var setting = _settingsService.GetTimeTopSetting();
        setting.ProgressBar.Scale = 2.5;
        _settingsService.SaveTimeTopSetting(setting);

        // Assert
        File.Exists(TimeTopSettingPath).Should().BeFalse();
        _settingsService.GetTimeTopSetting().ProgressBar.Scale.Should().Be(2.5);
    }

    [Fact]
    public void GetTimeTopSetting_文件不存在且未挂起时应自动创建配置文件()
    {
        // Arrange：TimeTopSetting.json 的存在性不参与引导判定，正常启动保留自动创建行为
        // （与全局配置的"绝不自动创建"刻意不对称）
        _settingsService.SetPersistenceSuspended(false);

        // Act
        _settingsService.GetTimeTopSetting();

        // Assert
        File.Exists(TimeTopSettingPath).Should().BeTrue();
    }
}
