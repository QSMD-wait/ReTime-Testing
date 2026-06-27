using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 计划表组管理器接口
/// 职责：管理计划表组配置文件的创建、读取、保存、删除，以及星期轮换解析
/// </summary>
public interface IScheduleGroupManager
{
    /// <summary>
    /// 获取计划表组的目录路径
    /// </summary>
    string ScheduleGroupsDirectory { get; }

    /// <summary>
    /// 计划表组变更事件
    /// </summary>
    event Action<ScheduleGroup>? OnGroupChanged;

    /// <summary>
    /// 计划表组删除事件
    /// </summary>
    event Action<string>? OnGroupDeleted;

    /// <summary>
    /// 初始化计划表组管理器
    /// </summary>
    void Initialize();

    /// <summary>
    /// 加载所有计划表组
    /// </summary>
    List<ScheduleGroup> LoadAllGroups();

    /// <summary>
    /// 根据指定ID加载计划表组
    /// </summary>
    ScheduleGroup? LoadGroup(string id);

    /// <summary>
    /// 保存计划表组
    /// </summary>
    void SaveGroup(ScheduleGroup group);

    /// <summary>
    /// 删除计划表组
    /// </summary>
    bool DeleteGroup(string id);

    /// <summary>
    /// 创建新计划表组（空白）
    /// </summary>
    ScheduleGroup CreateNewGroup(string id, string name);

    /// <summary>
    /// 检查计划表组是否存在
    /// </summary>
    bool GroupExists(string id);

    /// <summary>
    /// 根据指定日期解析当前应生效的计划表ID
    /// </summary>
    /// <param name="groupId">组ID</param>
    /// <param name="date">目标日期</param>
    /// <returns>计划表ID，null 表示该天没有计划表</returns>
    string? ResolveScheduleIdForDate(string groupId, DateTime date);

    /// <summary>
    /// 获取当前生效的计划表ID（综合解析 ScheduleConfig）
    /// 优先级：override.enabled > activeGroupId 轮换 > override.scheduleId 默认
    /// </summary>
    /// <returns>计划表ID，null 表示今天没有计划表</returns>
    string? GetEffectiveScheduleId();

    /// <summary>
    /// 刷新缓存
    /// </summary>
    void RefreshCache();

    /// <summary>
    /// 清除指定组的缓存
    /// </summary>
    void ClearCache(string id);
}