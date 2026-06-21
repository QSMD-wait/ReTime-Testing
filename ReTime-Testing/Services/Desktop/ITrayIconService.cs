namespace ReTime_Testing.Services;

/// <summary>
/// 系统托盘图标服务接口
/// 管理应用程序的系统托盘图标和右键菜单
/// </summary>
public interface ITrayIconService : IDisposable
{
    /// <summary>
    /// 打开设置请求事件
    /// </summary>
    event Action? OpenSettingRequested;

    /// <summary>
    /// 打开调试请求事件
    /// </summary>
    event Action? OpenDebugRequested;

    /// <summary>
    /// 打开时间计划编辑器请求事件
    /// </summary>
    event Action? OpenTimeScheduleEditorRequested;

    /// <summary>
    /// 关于请求事件
    /// </summary>
    event Action? AboutRequested;

    /// <summary>
    /// 退出请求事件
    /// </summary>
    event Action? ExitRequested;

    /// <summary>
    /// 重启请求事件
    /// </summary>
    event Action? RestartRequested;

    /// <summary>
    /// 初始化托盘图标
    /// </summary>
    void Initialize(TrayIconService.TrayIconConfig? config = null);

    /// <summary>
    /// 显示气泡通知
    /// </summary>
    void ShowBalloon(string title, string message);
}
