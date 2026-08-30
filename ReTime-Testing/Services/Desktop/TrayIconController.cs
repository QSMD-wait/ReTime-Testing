using Microsoft.Extensions.Logging;

namespace ReTime_Testing.Services;

/// <summary>
/// 系统托盘控制器
/// 封装托盘图标事件到窗口路由的订阅/退订与初始化，App 仅持有引用
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private readonly ITrayIconService _trayService;
    private readonly Action _onRestart;
    private readonly Action _onExit;
    private readonly ILogger<TrayIconController> _logger;

    public TrayIconController(ITrayIconService trayService, Action onRestart, Action onExit, ILogger<TrayIconController> logger)
    {
        _trayService = trayService;
        _onRestart = onRestart;
        _onExit = onExit;
        _logger = logger;

        _trayService.OpenSettingRequested += OnOpenSetting;
        _trayService.OpenDebugRequested += OnOpenDebugTest;
        _trayService.OpenLogViewerRequested += OnOpenLogViewer;
        _trayService.OpenTimeScheduleEditorRequested += OnOpenTimeScheduleEditor;
        _trayService.AboutRequested += OnOpenMainWindow;
        _trayService.RestartRequested += OnRestartRequested;
        _trayService.ExitRequested += OnExitRequested;
    }

    /// <summary>
    /// 初始化托盘图标
    /// </summary>
    /// <param name="showContextMenu">是否显示右键菜单（引导模式关闭）</param>
    public void Initialize(bool showContextMenu = true)
    {
        _trayService.Initialize(new TrayIconService.TrayIconConfig
        {
            Title = "ReTime - Testing",
            IconResource = "ReTime-Testing;component/Resources/app.ico",
            ShowContextMenu = showContextMenu
        });
    }

    private void OnOpenSetting()
    {
        try
        {
            WindowManager.ShowTimeTopSetting();
            _logger.LogInformation("设置窗口已打开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开设置窗口时发生异常");
        }
    }

    private void OnOpenMainWindow()
    {
        try
        {
            WindowManager.ShowMainWindow();
            _logger.LogInformation("主窗口已打开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开主窗口时发生异常");
        }
    }

    private void OnOpenDebugTest()
    {
        try
        {
            WindowManager.ShowDebugTest();
            _logger.LogInformation("调试测试窗口已打开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开调试测试窗口时发生异常");
        }
    }

    private void OnOpenLogViewer()
    {
        try
        {
            WindowManager.ShowLogViewer();
            _logger.LogInformation("日志查看器窗口已打开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开日志查看器窗口时发生异常");
        }
    }

    private void OnOpenTimeScheduleEditor()
    {
        try
        {
            WindowManager.ShowTimeScheduleEditor();
            _logger.LogInformation("时间计划编辑器已打开");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开时间计划编辑器时发生异常");
        }
    }

    private void OnRestartRequested()
    {
        try
        {
            _logger.LogInformation("应用程序重启请求");
            _onRestart();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重启应用程序时发生异常");
        }
    }

    private void OnExitRequested()
    {
        try
        {
            _logger.LogInformation("应用程序退出请求");
            _onExit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退出应用程序时发生异常");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _trayService.OpenSettingRequested -= OnOpenSetting;
        _trayService.OpenDebugRequested -= OnOpenDebugTest;
        _trayService.OpenLogViewerRequested -= OnOpenLogViewer;
        _trayService.OpenTimeScheduleEditorRequested -= OnOpenTimeScheduleEditor;
        _trayService.AboutRequested -= OnOpenMainWindow;
        _trayService.RestartRequested -= OnRestartRequested;
        _trayService.ExitRequested -= OnExitRequested;
    }
}
