using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 配置管理器接口
/// 职责：管理应用配置文件的创建、读取、更新、删除
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
    /// 获取时间计划表目录路径
    /// </summary>
    string TimeSchedulesDirectory { get; }

    /// <summary>
    /// 全局配置变更事件
    /// </summary>
    event Action<GlobalSetting>? OnGlobalSettingChanged;

    /// <summary>
    /// 初始化目录结构（启动时调用）
    /// </summary>
    void InitializeDirectories();

    /// <summary>
    /// 加载全局配置
    /// </summary>
    GlobalSetting LoadGlobalSetting();

    /// <summary>
    /// 保存全局配置
    /// </summary>
    void SaveGlobalSetting(GlobalSetting setting);

    /// <summary>
    /// 重置全局配置为默认值
    /// </summary>
    void ResetGlobalSetting();

    /// <summary>
    /// 刷新全局配置缓存
    /// </summary>
    void RefreshGlobalSettingCache();

    /// <summary>
    /// 获取缓存的全局配置（如果不存在则加载）
    /// </summary>
    GlobalSetting GetCachedGlobalSetting();

    /// <summary>
    /// 加载TimeTop设置
    /// </summary>
    TimeTopSetting LoadTimeTopSetting();

    /// <summary>
    /// 保存TimeTop设置
    /// </summary>
    void SaveTimeTopSetting(TimeTopSetting setting);
}
