using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ReTime_Testing.Models;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 计划表组管理器实现
    /// 组是时间表集合，持有天→表映射、轮转配置和日期覆盖
    /// </summary>
    public class ScheduleGroupManager : IScheduleGroupManager
    {
        private readonly ILogger<ScheduleGroupManager> _logger;
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

        public event Action<ScheduleGroup>? OnGroupChanged;
        public event Action<string>? OnGroupDeleted;

        public ScheduleGroupManager(IConfigurationManager configManager, ISettingsService settingsService, ILogger<ScheduleGroupManager> logger)
        {
            _configManager = configManager;
            _settingsService = settingsService;
            _logger = logger;
            _scheduleGroupsDirectory = configManager.ScheduleGroupsDirectory;
        }

        public void Initialize()
        {
            EnsureDirectoryExists(_scheduleGroupsDirectory);
            EnsureDefaultGroupExists();
            _logger.LogInformation("初始化完成");
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void EnsureDefaultGroupExists()
        {
            if (!GroupExists(ScheduleGroup.DefaultGroupId))
            {
                CreateNewGroup(ScheduleGroup.DefaultGroupId, "默认");
                _logger.LogInformation("已创建默认组");
            }
        }

        #region 组 CRUD

        public List<ScheduleGroup> LoadAllGroups()
        {
            try
            {
                var groups = new List<ScheduleGroup>();
                if (!Directory.Exists(_scheduleGroupsDirectory))
                    return groups;

                foreach (var file in Directory.GetFiles(_scheduleGroupsDirectory, "*.json"))
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
                        _logger.LogError(ex, "读取文件失败: {File}, 错误: {Message}", file, ex.Message);
                    }
                }
                return groups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载所有计划表组失败: {Message}", ex.Message);
                return new List<ScheduleGroup>();
            }
        }

        public ScheduleGroup? LoadGroup(string id)
        {
            try
            {
                if (_groupCache.TryGetValue(id, out var cached))
                    return cached;

                var filePath = Path.Combine(_scheduleGroupsDirectory, $"{id}.json");
                if (!File.Exists(filePath))
                    return null;

                return LoadGroupFromFile(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载计划表组失败: {Id}, 错误: {Message}", id, ex.Message);
                return null;
            }
        }

        private ScheduleGroup? LoadGroupFromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var group = JsonSerializer.Deserialize<ScheduleGroup>(json, _jsonOptions);
                if (group != null)
                    group.Id = Path.GetFileNameWithoutExtension(filePath);
                return group;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON解析失败: {FilePath}, 错误: {Message}", filePath, ex.Message);
                return null;
            }
        }

        public void SaveGroup(ScheduleGroup group)
        {
            if (string.IsNullOrEmpty(group.Id))
                throw new ArgumentException("计划表组ID不能为空");

            group.Metadata.UpdatedAt = DateTime.UtcNow.ToString("o");
            var filePath = Path.Combine(_scheduleGroupsDirectory, $"{group.Id}.json");
            string json = JsonSerializer.Serialize(group, _jsonOptions);
            File.WriteAllText(filePath, json);
            _groupCache[group.Id] = group;
            OnGroupChanged?.Invoke(group);
        }

        public ScheduleGroup CreateNewGroup(string id, string name)
        {
            var group = new ScheduleGroup
            {
                Id = id,
                Version = "1.0.0",
                Metadata = new ScheduleGroupMetadata
                {
                    Name = name,
                    Description = "",
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    UpdatedAt = DateTime.UtcNow.ToString("o")
                },
                DayScheduleMap = new Dictionary<string, string>(),
                RotationCycleCount = 1,
                RotatedDayScheduleMaps = new Dictionary<string, Dictionary<string, string>>(),
                DateOverrides = new Dictionary<string, string>()
            };
            SaveGroup(group);
            return group;
        }

        public bool GroupExists(string id)
        {
            if (_groupCache.ContainsKey(id))
                return true;
            var filePath = Path.Combine(_scheduleGroupsDirectory, $"{id}.json");
            return File.Exists(filePath);
        }

        #endregion

        #region 组保护操作

        /// <summary>
        /// 解散组（组文件删除，不涉及表）
        /// </summary>
        public bool DisbandGroup(string groupId)
        {
            if (groupId == ScheduleGroup.DefaultGroupId)
            {
                _logger.LogWarning("默认组不可解散");
                return false;
            }

            try
            {
                var filePath = Path.Combine(_scheduleGroupsDirectory, $"{groupId}.json");
                if (File.Exists(filePath))
                    File.Delete(filePath);
                _groupCache.Remove(groupId);
                OnGroupDeleted?.Invoke(groupId);

                _logger.LogInformation("组已解散: {GroupId}", groupId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解散组失败: {GroupId}, 错误: {Message}", groupId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 重命名组
        /// </summary>
        public bool RenameGroup(string groupId, string newName)
        {
            if (groupId == ScheduleGroup.DefaultGroupId)
            {
                _logger.LogWarning("默认组不可重命名");
                return false;
            }

            var group = LoadGroup(groupId);
            if (group == null) return false;

            group.Metadata.Name = newName;
            SaveGroup(group);
            return true;
        }

        #endregion

        #region 轮换解析

        /// <summary>
        /// 计算指定日期在组的轮换周期中处于第几周
        /// 返回 0 表示基础周（不轮转），1~N 表示第 N 轮转周
        /// </summary>
        private int ResolveCurrentCycle(ScheduleGroup group, DateTime date)
        {
            if (group.RotationCycleCount <= 1)
                return 0;

            try
            {
                DateTime baseDate;
                if (!string.IsNullOrEmpty(group.RotationStartDate) && DateTime.TryParse(group.RotationStartDate, out var parsed))
                    baseDate = parsed.Date;
                else
                    baseDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);

                var totalElapsedWeeks = (int)Math.Floor((date.Date - baseDate).TotalDays / 7);
                var position = (totalElapsedWeeks + group.RotationOffset) % group.RotationCycleCount;
                if (position < 0)
                    position += group.RotationCycleCount;

                return position; // 0-based: 0=基础周, 1=第1轮转周, ...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算轮换周失败: {GroupId}, 错误: {Message}", group.Id, ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// 获取组在指定日期生效的天→表映射（合并基础映射和轮转覆盖）
        /// </summary>
        public Dictionary<string, string> GetEffectiveDayScheduleMap(ScheduleGroup group, DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Today;
            var effectiveMap = new Dictionary<string, string>(group.DayScheduleMap);

            if (group.RotationCycleCount > 1)
            {
                var currentWeek = ResolveCurrentCycle(group, targetDate);
                if (currentWeek > 0 && group.RotatedDayScheduleMaps.TryGetValue(currentWeek.ToString(), out var rotatedMap))
                {
                    foreach (var kv in rotatedMap)
                    {
                        if (string.IsNullOrEmpty(kv.Value)) continue;
                        effectiveMap[kv.Key] = kv.Value;
                    }
                }
            }

            // 过滤空值：未配置的天不应被解析成"生效计划表"
            foreach (var emptyKey in effectiveMap.Where(kv => string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Key).ToList())
                effectiveMap.Remove(emptyKey);

            return effectiveMap;
        }

        /// <summary>
        /// 获取当前生效的计划表ID（综合解析 ScheduleConfig + 激活组的天→表映射）
        /// 优先级：override.enabled > 日期覆盖 > 轮转覆盖 > 基础映射
        /// </summary>
        public string? GetEffectiveScheduleId()
        {
            try
            {
                var setting = _settingsService.GetTimeTopSetting();
                var config = setting.Schedule;

                if (!config.Enabled)
                    return null;

                // 1. 手动覆盖优先
                if (config.Override.Enabled)
                    return config.Override.ScheduleId;

                // 2. 激活组
                if (string.IsNullOrEmpty(config.ActiveGroupId))
                    return null;

                var group = LoadGroup(config.ActiveGroupId);
                if (group == null)
                {
                    _logger.LogWarning("激活组不存在: {GroupId}", config.ActiveGroupId);
                    return null;
                }

                var today = DateTime.Today;

                // 3. 检查日期覆盖（最高优先级）
                if (group.DateOverrides.TryGetValue(today.ToString("yyyy-MM-dd"), out var overrideId) &&
                    !string.IsNullOrEmpty(overrideId))
                    return overrideId;

                // 4. 获取天→表映射（基础 + 轮转覆盖合并）
                var effectiveMap = GetEffectiveDayScheduleMap(group, today);

                // 5. 查找今天的表
                var dayKey = ((int)today.DayOfWeek).ToString();
                if (effectiveMap.TryGetValue(dayKey, out var scheduleId) && !string.IsNullOrEmpty(scheduleId))
                    return scheduleId;

                // 6. 今日无映射
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取生效计划表ID失败: {Message}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 获取组的轮换周描述信息
        /// </summary>
        public string GetRotationInfo(string groupId, DateTime? date = null)
        {
            try
            {
                var group = LoadGroup(groupId);
                if (group == null)
                    return "每周";

                if (group.RotationCycleCount <= 1)
                    return "每周";

                var targetDate = date ?? DateTime.Today;
                var currentCycle = ResolveCurrentCycle(group, targetDate);
                return $"第{currentCycle + 1}/{group.RotationCycleCount}周";
            }
            catch
            {
                return "每周";
            }
        }

        private void RefreshCache()
        {
            _groupCache.Clear();
        }

        private void ClearCache(string id)
        {
            _groupCache.Remove(id);
        }

        #endregion
    }
}
