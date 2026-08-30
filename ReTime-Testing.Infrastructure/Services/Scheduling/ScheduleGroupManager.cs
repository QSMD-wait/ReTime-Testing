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
    /// 计划表组管理器实现（对齐 ClassIsland 的 ClassPlanGroup 逻辑）
    /// 组仅作为归类容器，轮换配置在每个计划表上
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
        private readonly ITimeScheduleManager _scheduleManager;
        private string _scheduleGroupsDirectory = string.Empty;

        public event Action<ScheduleGroup>? OnGroupChanged;
        public event Action<string>? OnGroupDeleted;

        public ScheduleGroupManager(IConfigurationManager configManager, ISettingsService settingsService, ITimeScheduleManager scheduleManager, ILogger<ScheduleGroupManager> logger)
        {
            _configManager = configManager;
            _settingsService = settingsService;
            _scheduleManager = scheduleManager;
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
                }
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
        /// 解散组：组内表移到默认组，组文件删除
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
                // 将该组内所有表的 AssociatedGroupId 改为 default
                var schedules = _scheduleManager.GetScheduleList();
                foreach (var s in schedules.Where(s => s.AssociatedGroupId == groupId))
                {
                    var full = _scheduleManager.LoadSchedule(s.Id);
                    if (full?.Settings?.Metadata != null)
                    {
                        full.Settings.Metadata.AssociatedGroupId = ScheduleGroup.DefaultGroupId;
                        full.Settings.Metadata.UpdatedAt = DateTime.UtcNow.ToString("o");
                        _scheduleManager.SaveSchedule(full);
                    }
                }

                // 删除组文件
                var filePath = Path.Combine(_scheduleGroupsDirectory, $"{groupId}.json");
                if (File.Exists(filePath))
                    File.Delete(filePath);
                _groupCache.Remove(groupId);
                OnGroupDeleted?.Invoke(groupId);

                _logger.LogInformation("组已解散: {GroupId}，表已移至默认组", groupId);
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
        /// 计算当前日期在轮换周期中处于第几周（对齐 ClassIsland 的 GetCyclePositionsByDate）
        /// </summary>
        private int ResolveCurrentCycle(int cycleCount, DateTime date)
        {
            try
            {
                var setting = _settingsService.GetTimeTopSetting();
                var baseDateStr = setting.Schedule.RotationBaseDate;
                DateTime baseDate;

                if (!string.IsNullOrEmpty(baseDateStr) && DateTime.TryParse(baseDateStr, out var parsed))
                    baseDate = parsed.Date;
                else
                    baseDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);

                var totalElapsedWeeks = (int)Math.Floor((date.Date - baseDate).TotalDays / 7);

                var offsets = setting.Schedule.MultiWeekRotationOffset;
                int offset = 0;
                if (cycleCount >= 2 && cycleCount < offsets.Count)
                    offset = offsets[cycleCount];

                var position = (totalElapsedWeeks + offset) % cycleCount;
                if (position < 0)
                    position += cycleCount;

                return position + 1; // 1-based
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算轮换周失败: {Message}", ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// 检查单个计划表是否在指定日期启用（对齐 ClassIsland 的 CheckClassPlan）
        /// </summary>
        private bool CheckSchedule(ScheduleInfo schedule, DateTime date)
        {
            // 1. 未启用的表跳过
            if (!schedule.IsEnabled)
                return false;

            // 2. 星期几匹配
            if (schedule.DayOfWeek != (int)date.DayOfWeek)
                return false;

            // 3. 不轮换（RotationCycleCount <= 1）→ 仅当 RotationWeekIndex == 0 时启用
            if (schedule.RotationCycleCount <= 1)
                return schedule.RotationWeekIndex == 0;

            // 4. 轮换周索引为 0 → 每周启用
            if (schedule.RotationWeekIndex == 0)
                return true;

            // 5. 计算当前轮换周，与表的 RotationWeekIndex 比较
            var currentCycle = ResolveCurrentCycle(schedule.RotationCycleCount, date);
            return schedule.RotationWeekIndex == currentCycle;
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
                var config = setting.Schedule;

                if (!config.Enabled)
                    return null;

                // 1. 手动覆盖优先
                if (config.Override.Enabled)
                    return config.Override.ScheduleId;

                // 2. 激活组轮换
                if (!string.IsNullOrEmpty(config.ActiveGroupId))
                {
                    var allSchedules = _scheduleManager.GetScheduleList();
                    var candidates = allSchedules
                        .Where(s => s.AssociatedGroupId == config.ActiveGroupId)
                        .OrderByDescending(s => s.IsEnabled)
                        .ThenBy(s => s.DayOfWeek);

                    foreach (var candidate in candidates)
                    {
                        if (CheckSchedule(candidate, DateTime.Today))
                            return candidate.Id;
                    }

                    // 组已激活但今日无匹配 → 不生成计划
                    return null;
                }

                // 3. 无激活组且无覆盖 → 不生成计划
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
                var schedules = _scheduleManager.GetScheduleList();
                var groupSchedules = schedules.Where(s => s.AssociatedGroupId == groupId && s.IsEnabled && s.RotationCycleCount > 1).ToList();
                if (!groupSchedules.Any())
                    return "每周";

                var maxCycle = groupSchedules.Max(s => s.RotationCycleCount);
                var targetDate = date ?? DateTime.Today;
                var currentCycle = ResolveCurrentCycle(maxCycle, targetDate);
                return $"第{currentCycle}/{maxCycle}周";
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
