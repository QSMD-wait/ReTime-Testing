using System.IO;
using ReTime_Testing.Core.Models.Theme;
using ReTime_Testing.Core.Services;
using ReTime_Testing.Models;
using ReTime_Testing.Services.Onboarding;

namespace ReTime_Testing.Services;

/// <summary>
/// 应用启动结果
/// </summary>
/// <param name="NeedsWelcomeFlow">是否需要进入欢迎引导模式</param>
/// <param name="TimeTopSetting">当前 TimeTop 配置（仅正常启动有效）</param>
/// <param name="ScheduleValidationError">需向用户展示的调度验证错误（无则为 null）</param>
public sealed record AppStartupResult(
    bool NeedsWelcomeFlow,
    TimeTopSetting? TimeTopSetting,
    string? ScheduleValidationError);

/// <summary>
/// 应用启动编排器
/// 封装正常启动与欢迎引导模式的非 UI 初始化序列，App 仅负责生命周期与 UI 交互
/// </summary>
public class AppBootstrapper
{
    private const string Source = nameof(AppBootstrapper);

    private readonly IConfigurationManager _configManager;
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IProgressBarThemeService _progressBarThemeService;
    private readonly IAutoStartService _autoStartService;
    private readonly ITimeScheduleManager _timeScheduleManager;
    private readonly ITimeService _timeService;
    private readonly ITimeCalibrationService _timeCalibrationService;
    private readonly IScheduleOrchestrator _scheduleOrchestrator;
    private readonly IDesktopWindowManager _desktopWindowManager;
    private readonly IGlobalTimeTopDesktopService _globalDesktopService;

    public AppBootstrapper(
        IConfigurationManager configManager,
        ISettingsService settingsService,
        IThemeService themeService,
        IProgressBarThemeService progressBarThemeService,
        IAutoStartService autoStartService,
        ITimeScheduleManager timeScheduleManager,
        ITimeService timeService,
        ITimeCalibrationService timeCalibrationService,
        IScheduleOrchestrator scheduleOrchestrator,
        IDesktopWindowManager desktopWindowManager,
        IGlobalTimeTopDesktopService globalDesktopService)
    {
        _configManager = configManager;
        _settingsService = settingsService;
        _themeService = themeService;
        _progressBarThemeService = progressBarThemeService;
        _autoStartService = autoStartService;
        _timeScheduleManager = timeScheduleManager;
        _timeService = timeService;
        _timeCalibrationService = timeCalibrationService;
        _scheduleOrchestrator = scheduleOrchestrator;
        _desktopWindowManager = desktopWindowManager;
        _globalDesktopService = globalDesktopService;
    }

    /// <summary>
    /// 执行正常启动的完整初始化序列
    /// </summary>
    /// <returns>启动结果（含欢迎引导判定与调度错误信息）</returns>
    public AppStartupResult RunStartup()
    {
        // 初始化目录结构
        _configManager.InitializeDirectories();

        // 记录全局配置文件是否存在（必须在加载自动创建前捕获，用于首次启动判定）
        var settingFileExisted = File.Exists(_configManager.GlobalSettingFilePath);

        var globalSetting = _settingsService.GetGlobalSetting();

        // 初始化 Serilog 日志服务
        var logConfig = new LogServiceConfiguration(globalSetting.Basic.Log, _configManager.LogsDirectory);
        SerilogLogService.Initialize(logConfig);
        Logger.OnSerilogReady();
        Logger.Info(Source, "Serilog 日志服务已初始化");

        // 首次启动：进入欢迎引导模式
        if (OnboardingFlow.ShouldShowWelcome(settingFileExisted,
                globalSetting.Basic.WelcomeShowed, globalSetting.Basic.ForceShowWelcome))
        {
            return new AppStartupResult(NeedsWelcomeFlow: true, TimeTopSetting: null, ScheduleValidationError: null);
        }

        // 应用主题 + 打开进度条窗口
        ApplyThemeAndDesktopPosition(globalSetting.Basic.Theme);

        // 初始化进度条主题服务
        _progressBarThemeService.LoadAllThemes();
        _progressBarThemeService.ApplyTheme(ProgressBarThemeManifest.DefaultId);
        Logger.Info(Source, "进度条主题服务已初始化");

        // 应用自启动配置
        _autoStartService.InitializeFromConfig(globalSetting.Basic.AutoStart);

        // 初始化时间计划管理器
        _timeScheduleManager.Initialize();

        Logger.Info(Source, "单调时钟服务已初始化");

        // 初始化时间校准服务
        var timeTopSetting = _settingsService.GetTimeTopSetting();
        _timeCalibrationService.ApplyConfig(timeTopSetting.Calibration);
        Logger.Info(Source, "时间校准服务已初始化");

        // 恢复用户时间偏移
        var userOffsetSeconds = timeTopSetting.Calibration.UserOffsetSeconds;
        if (double.IsNaN(userOffsetSeconds) || double.IsInfinity(userOffsetSeconds))
            userOffsetSeconds = 0;
        if (userOffsetSeconds != 0)
        {
            _timeService.ApplyUserOffset(TimeSpan.FromSeconds(userOffsetSeconds));
        }

        // 初始化调度（含表组管理器初始化）
        var scheduleResult = _scheduleOrchestrator.InitializeOnStartup(timeTopSetting.Schedule.Enabled);
        var scheduleError = scheduleResult.Status is ScheduleStartupStatus.InvalidScheduleId
                or ScheduleStartupStatus.InvalidPlan
            ? scheduleResult.Message
            : null;

        // 启动时间校准服务
        _timeCalibrationService.Start();
        Logger.Info(Source, "时间校准服务已启动");

        Logger.Info(Source, "应用程序启动成功");

        return new AppStartupResult(NeedsWelcomeFlow: false, TimeTopSetting: timeTopSetting, ScheduleValidationError: scheduleError);
    }

    /// <summary>
    /// 准备欢迎引导模式的最小化环境（主题、进度条窗口位置、流畅优化）
    /// </summary>
    public void PrepareWelcomeEnvironment()
    {
        var globalSetting = _settingsService.GetGlobalSetting();

        // 应用主题（引导窗口正常显示）+ 打开进度条窗口，供引导中的位置步骤实时预览
        ApplyThemeAndDesktopPosition(globalSetting.Basic.Theme);

        // 流畅优化：引导期间强制开启（无视配置文件），正常启动后由配置文件决定
        _globalDesktopService.ForceSmoothnessOptimization = true;
    }

    /// <summary>
    /// 应用主题并按配置设置进度条窗口初始位置
    /// </summary>
    private void ApplyThemeAndDesktopPosition(string theme)
    {
        _themeService.ApplyTheme(theme);

        var timeTopSetting = _settingsService.GetTimeTopSetting();
        var initialPosition = ParsePosition(timeTopSetting.ProgressBar.Position);
        _desktopWindowManager.SetPosition(initialPosition);
    }

    /// <summary>
    /// 将配置字符串解析为 ProgressBarPosition 枚举
    /// </summary>
    private static ProgressBarPosition ParsePosition(string position)
    {
        return position?.ToLowerInvariant() switch
        {
            "bottom" => ProgressBarPosition.Bottom,
            "left" => ProgressBarPosition.Left,
            "right" => ProgressBarPosition.Right,
            _ => ProgressBarPosition.Top
        };
    }
}
