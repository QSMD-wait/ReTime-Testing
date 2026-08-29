using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 计划表组管理器接口
/// 职责：管理计划表组配置文件的创建、读取、保存、删除
/// 组仅作为归类容器，轮换配置在每个计划表上
/// </summary>
public interface IScheduleGroupManager
{
    /// <summary>
    /// 计划表组变更事件
    /// </summary>
    event Action<ScheduleGroup>? OnGroupChanged;

    /// <summary>
    /// 计划表组删除事件
    /// </summary>
    event Action<string>? OnGroupDeleted;

    /// <summary>
    /// 初始化计划表组管理器（确保默认组存在）
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
    /// 创建新计划表组
    /// </summary>
    ScheduleGroup CreateNewGroup(string id, string name);

    /// <summary>
    /// 解散计划表组（组内表移到默认组，组被删除）
    /// 默认组不可解散
    /// </summary>
    bool DisbandGroup(string groupId);

    /// <summary>
    /// 重命名计划表组
    /// 默认组不可重命名
    /// </summary>
    bool RenameGroup(string groupId, string newName);

    /// <summary>
    /// 检查计划表组是否存在
    /// </summary>
    bool GroupExists(string id);

    /// <summary>
    /// 获取当前生效的计划表ID（综合解析 ScheduleConfig）
    /// 优先级：override.enabled > activeGroupId 轮换 > override.scheduleId 默认
    /// </summary>
    string? GetEffectiveScheduleId();

    /// <summary>
    /// 获取组的轮换周描述信息
    /// </summary>
    string GetRotationInfo(string groupId, DateTime? date = null);
}
