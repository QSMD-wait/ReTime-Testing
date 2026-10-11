using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 设置配置服务接口
    /// 职责：配置的读取、保存、缓存、校验、变更通知、热重载分发
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// 全局配置变更事件
        /// </summary>
        event Action<GlobalSetting>? OnGlobalSettingChanged;

        /// <summary>
        /// TimeTop配置变更事件
        /// </summary>
        event Action<TimeTopSetting>? OnTimeTopSettingChanged;

        /// <summary>
        /// 获取全局配置（优先缓存，无缓存则从文件加载）
        /// </summary>
        GlobalSetting GetGlobalSetting();

        /// <summary>
        /// 保存全局配置（写入文件 + 更新缓存 + 触发变更通知 + 热重载分发）
        /// </summary>
        void SaveGlobalSetting(GlobalSetting setting);

        /// <summary>
        /// 重置全局配置为默认值
        /// </summary>
        void ResetGlobalSetting();

        /// <summary>
        /// 刷新全局配置缓存（下次 GetGlobalSetting 将从文件重新加载）
        /// </summary>
        void RefreshGlobalSettingCache();

        /// <summary>
        /// 获取TimeTop配置（优先缓存，无缓存则从文件加载）
        /// </summary>
        TimeTopSetting GetTimeTopSetting();

        /// <summary>
        /// 保存TimeTop配置（写入文件 + 更新缓存 + 触发变更通知 + 热重载分发）
        /// </summary>
        void SaveTimeTopSetting(TimeTopSetting setting);

        /// <summary>
        /// 刷新TimeTop配置缓存
        /// </summary>
        void RefreshTimeTopSettingCache();

        /// <summary>
        /// 设置持久化挂起（引导期间配置仅驻内存：Save 仍更新缓存并触发变更事件，但不写盘）
        /// 由引导入口开启、引导完成时关闭，保证引导中断不残留半套配置
        /// </summary>
        /// <param name="suspended">是否挂起持久化</param>
        void SetPersistenceSuspended(bool suspended);
    }
}