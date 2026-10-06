namespace ReTime_Testing.Services;

/// <summary>
/// 托盘图标服务配置
/// </summary>
public class TrayIconConfig
{
    public string Title { get; set; } = "ReTime-Testing";
    public string? IconPath { get; set; }       // 外部文件路径
    public string? IconResource { get; set; }   // 内嵌资源名（如 Resources/app.ico）
    public bool ShowContextMenu { get; set; } = true;   // 是否显示右键菜单（引导模式关闭）
}
