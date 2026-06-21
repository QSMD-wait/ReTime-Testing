using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 时间计划管理器接口
/// 职责：管理时间计划文件的创建、读取、保存、删除
/// </summary>
public interface ITimeScheduleManager
{
    /// <summary>
    /// 获取时间计划的目录路径
    /// </summary>
    string TimeSchedulesDirectory { get; }

    /// <summary>
    /// 时间计划变更事件
    /// </summary>
    event Action<TimeSchedule>? OnScheduleChanged;

    /// <summary>
    /// 时间计划删除事件
    /// </summary>
    event Action<string>? OnScheduleDeleted;

    /// <summary>
    /// 初始化所有时间计划
    /// </summary>
    void Initialize();

    /// <summary>
    /// 创建默认时间计划
    /// </summary>
    TimeSchedule CreateDefaultSchedule();

    /// <summary>
    /// 加载所有时间计划
    /// </summary>
    List<TimeSchedule> LoadAllSchedules();

    /// <summary>
    /// 根据指定ID加载时间计划
    /// </summary>
    TimeSchedule? LoadSchedule(string id);

    /// <summary>
    /// 保存时间计划
    /// </summary>
    void SaveSchedule(TimeSchedule schedule);

    /// <summary>
    /// 删除时间计划
    /// </summary>
    bool DeleteSchedule(string id);

    /// <summary>
    /// 获取计划表列表（简化信息）
    /// </summary>
    List<ScheduleInfo> GetScheduleList();

    /// <summary>
    /// 创建新计划表（空白）
    /// </summary>
    TimeSchedule CreateNewSchedule(string id, string name);

    /// <summary>
    /// 复制计划表
    /// </summary>
    TimeSchedule? CopySchedule(string sourceId, string newId);

    /// <summary>
    /// 重命名计划表
    /// </summary>
    bool RenameSchedule(string id, string newName);

    /// <summary>
    /// 检查计划表是否存在
    /// </summary>
    bool ScheduleExists(string id);

    /// <summary>
    /// 验证时间计划
    /// </summary>
    bool ValidateSchedule(TimeSchedule schedule);

    /// <summary>
    /// 刷新缓存
    /// </summary>
    void RefreshCache();

    /// <summary>
    /// 清除指定计划的缓存
    /// </summary>
    void ClearCache(string id);
}
