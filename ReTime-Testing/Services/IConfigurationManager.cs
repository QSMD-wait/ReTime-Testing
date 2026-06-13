using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 配置管理器接口
/// 职责：路径注册中心——管理应用所有文件和目录的路径
/// 配置的读取、保存、缓存、校验、通知由 ISettingsService 负责
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// 获取应用根目录
    /// </summary>
    string ApplicationRootDirectory { get; }

    /// <summary>
    /// 获取数据目录
    /// </summary>
    string DataDirectory { get; }

    /// <summary>
    /// 获取全局配置文件路径
    /// </summary>
    string GlobalSettingFilePath { get; }

    /// <summary>
    /// 获取配置文件存储目录路径
    /// </summary>
    string ConfigsDirectory { get; }

    /// <summary>
    /// 获取时间计划表目录路径
    /// </summary>
    string TimeSchedulesDirectory { get; }

    /// <summary>
    /// 获取TimeTop设置文件路径
    /// </summary>
    string TimeTopSettingFilePath { get; }

    /// <summary>
    /// 获取日志文件目录路径
    /// </summary>
    string LogsDirectory { get; }

    /// <summary>
    /// 初始化目录结构（启动时调用）
    /// </summary>
    void InitializeDirectories();

    /// <summary>
    /// 加载全局配置（委托给 SettingsService）
    /// </summary>
    GlobalSetting LoadGlobalSetting();

    /// <summary>
    /// 保存全局配置（委托给 SettingsService）
    /// </summary>
    void SaveGlobalSetting(GlobalSetting setting);

    /// <summary>
    /// 重置全局配置为默认值（委托给 SettingsService）
    /// </summary>
    void ResetGlobalSetting();

    /// <summary>
    /// 刷新全局配置缓存（委托给 SettingsService）
    /// </summary>
    void RefreshGlobalSettingCache();

    /// <summary>
    /// 获取缓存的全局配置（委托给 SettingsService）
    /// </summary>
    GlobalSetting GetCachedGlobalSetting();

    /// <summary>
    /// 加载TimeTop设置（委托给 SettingsService）
    /// </summary>
    TimeTopSetting LoadTimeTopSetting();

    /// <summary>
    /// 保存TimeTop设置（委托给 SettingsService）
    /// </summary>
    void SaveTimeTopSetting(TimeTopSetting setting);
}