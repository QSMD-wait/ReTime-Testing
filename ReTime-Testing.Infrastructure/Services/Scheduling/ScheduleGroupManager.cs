using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReTime_Testing.Models;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 计划表组管理器实现
    /// 职责：管理计划表组配置文件的创建、读取、保存、删除，以及星期轮换解析
    /// </summary>
    public class ScheduleGroupManager : IScheduleGroupManager
    {
        private const string LOG_MODULE = "ScheduleGroupManager";

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly Dictionary<string, ScheduleGroup> _groupCache = new();
        private readonly IConfigurationManager _configManager;
        private readonly ISettingsService _settingsService;
        private string _scheduleGroupsDirectory = string.Empty;

        /// <summary>
        /// 获取计划表组的目录路径
        /// </summary>
        public string ScheduleGroupsDirectory => _scheduleGroupsDirectory;

        /// <summary>
        /// 计划表组变更事件
        /// </summary>
        public event Action<ScheduleGroup>? OnGroupChanged;

        /// <summary>
        /// 计划表组删除事件
        /// </summary>
        public event Action<string>? OnGroupDeleted;

        /// <summary>
        /// 构造函数（支持 DI 注入）
        /// </summary>
        public ScheduleGroupManager(IConfigurationManager configManager, ISettingsService settingsService)
        {
            _configManager = configManager;
            _settingsService = settingsService;
            _scheduleGroupsDirectory = configManager.ScheduleGroupsDirectory;

            Logger.Info(LOG_MODULE,
                $"路径初始化完成: ScheduleGroupsDirectory={_scheduleGroupsDirectory}");
        }

        /// <summary>
        /// 初始化计划表组管理器
        /// </summary>
        public void Initialize()
        {
            try
            {
                EnsureDirectoryExists(_scheduleGroupsDirectory);

                Logger.Info(LOG_MODULE, "初始化完成");
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE,
                    $"初始化失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Logger.Info(LOG_MODULE, $"目录已创建: {path}");
            }
        }

        /// <summary>
        /// 加载所有计划表组
        /// </summary>
        public List<ScheduleGroup> LoadAllGroups()
        {
            try
            {
                var groups = new List<ScheduleGroup>();

                if (!Directory.Exists(_scheduleGroupsDirectory))
                {
                    return groups;
                }

                var files = Directory.GetFiles(_scheduleGroupsDirectory, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var group = LoadGroupFromFile(file);
                        if (group != null)
                        {
                            groups.Add(group);
                            _groupCache[group.Id] = group;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(LOG_MODULE,
                            $"读取文件失败: {file}, 错误: {ex.Message}");
                    }
                }

                return groups;
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE,
                    $"加载所有计划表组失败: {ex.Message}", ex);
                return new List<ScheduleGroup>();
            }
        }

        /// <summary>
        /// 根据指定ID加载计划表组
        /// </summary>
        public ScheduleGroup? LoadGroup(string id)
        {
            try
            {
                if (_groupCache.TryGetValue(id, out var cachedGroup))
                {
                    return cachedGroup;
                }

                var filePath = Path.Combine(_scheduleGroupsDirectory, $"{id}.json");

                if (!File.Exists(filePath))
                {
                    return null;
                }

                return LoadGroupFromFile(filePath);
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE,
                    $"加载计划表组失败: {id}, 错误: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 从文件加载计划表组
        /// </summary>
        private ScheduleGroup? LoadGroupFromFile(string filePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                var group = JsonSerializer.Deserialize<ScheduleGroup>(jsonContent, _jsonOptions);

                if (group != null)
                {
                    group.Id = Path.GetFileNameWithoutExtension(filePath);
                }

                return group;
            }
            catch (JsonException ex)
            {
                Logger.Error(LOG_MODULE,
                    $"JSON解析失败: {filePath}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存计划表组
        /// </summary>
        public void SaveGroup(ScheduleGroup group)
        {
            try
            {
                if (string.IsNullOrEmpty(group.Id))
                {
                    throw new ArgumentException("计划表组ID不能为空");
                }

                group.Metadata.UpdatedAt = DateTime.UtcNow.ToString("o");

                var filePath = Path.Combine(_scheduleGroupsDirectory, $"{group.Id}.json");
                SaveGroupToFile(group, filePath);

                _groupCache[group.Id] = group;

                OnGroupChanged?.Invoke(group);

                Logger.Info(LOG_MODULE, $"计划表组保存成功: {group.Id}");
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE,
                    $"保存计划表组失败: {group.Id}, 错误: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 保存计划表组到文件
        /// </summary>
        private void SaveGroupToFile(ScheduleGroup group, string filePath)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(group, _jsonOptions);
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE,
                    $"写入文件失败: {filePath}, 错误: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 删除计划表组
        /// </summary>
        public bool DeleteGroup(string id)
        {
            try
            {
                var filePath = Path.Combine(_scheduleGroupsDirectory, $"{id}.json");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _groupCache.Remove(id);
                    OnGroupDeleted?.Invoke(id);

                    Logger.Info(LOG_MODULE, $"计划表组删除成功: {id}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE,
                    $"删除计划表组失败: {id}, 错误: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 创建新计划表组（空白）
        /// </summary>
        public ScheduleGroup CreateNewGroup(string id, string name)
        {
            var now = DateTime.UtcNow;

            var group = new ScheduleGroup
            {
                Id = id,
                Version = "1.0.0",
                Metadata = new ScheduleGroupMetadata
                {
                    Name = name,
                    Description = "",
                    CreatedAt = now.ToString("o"),
                    UpdatedAt = now.ToString("o")
                },
                WeekSchedule = new List<WeekScheduleItem>()
            };

            SaveGroup(group);
            return group;
        }

        /// <summary>
        /// 检查计划表组是否存在
        /// </summary>
        public bool GroupExists(string id)
        {
            if (_groupCache.ContainsKey(id))
            {
                return true;
            }

            var filePath = Path.Combine(_scheduleGroupsDirectory, $"{id}.json");
            return File.Exists(filePath);
        }

        /// <summary>
        /// 根据指定日期解析当前应生效的计划表ID
        /// </summary>
        public string? ResolveScheduleIdForDate(string groupId, DateTime date)
        {
            try
            {
                var group = LoadGroup(groupId);
                if (group == null)
                {
                    Logger.Warn(LOG_MODULE, $"计划表组不存在: {groupId}");
                    return null;
                }

                var weekDay = (int)date.DayOfWeek;
                var mapping = group.WeekSchedule.FirstOrDefault(w => w.WeekDay == weekDay);

                return mapping?.ScheduleId;
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE,
                    $"解析计划表ID失败: groupId={groupId}, date={date:yyyy-MM-dd}, 错误: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取当前生效的计划表ID（综合解析 ScheduleConfig）
        /// 优先级：override.enabled > activeGroupId 轮换 > override.scheduleId 默认
        /// </summary>
        public string? GetEffectiveScheduleId()
        {
            try
            {
                var setting = _settingsService.GetTimeTopSetting();
                var scheduleConfig = setting.Schedule;

                if (!scheduleConfig.Enabled)
                {
                    return null;
                }

                if (scheduleConfig.Override.Enabled)
                {
                    return scheduleConfig.Override.ScheduleId;
                }

                if (!string.IsNullOrEmpty(scheduleConfig.ActiveGroupId))
                {
                    var resolvedId = ResolveScheduleIdForDate(scheduleConfig.ActiveGroupId, DateTime.Today);
                    if (resolvedId != null)
                    {
                        return resolvedId;
                    }

                    return null;
                }

                return scheduleConfig.Override.ScheduleId;
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE,
                    $"获取生效计划表ID失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 刷新缓存
        /// </summary>
        public void RefreshCache()
        {
            _groupCache.Clear();
            LoadAllGroups();

            Logger.Info(LOG_MODULE, "缓存已刷新");
        }

        /// <summary>
        /// 清除指定组的缓存
        /// </summary>
        public void ClearCache(string id)
        {
            _groupCache.Remove(id);
        }
    }
}