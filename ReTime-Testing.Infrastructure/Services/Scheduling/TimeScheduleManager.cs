using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ReTime_Testing.Models;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 时间计划管理器的主要实现
    /// 职责：管理时间计划文件的创建、读取、保存、删除
    /// </summary>
    public class TimeScheduleManager : ITimeScheduleManager
    {
        private readonly ILogger<TimeScheduleManager> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly Dictionary<string, TimeSchedule> _scheduleCache = new();
        private string _timeSchedulesDirectory = string.Empty;

        private static readonly Regex ScheduleIdPattern = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

        /// <summary>
        /// 获取时间计划的目录路径
        /// </summary>
        public string TimeSchedulesDirectory => _timeSchedulesDirectory;

        /// <summary>
        /// 时间计划添加事件
        /// </summary>
        public event Action<TimeSchedule>? OnScheduleChanged;

        /// <summary>
        /// 时间计划删除事件
        /// </summary>
        public event Action<string>? OnScheduleDeleted;

        /// <summary>
        /// 构造函数（支持 DI 注入）
        /// </summary>
        /// <param name="configManager">配置管理器</param>
        public TimeScheduleManager(IConfigurationManager configManager, ILogger<TimeScheduleManager> logger)
        {
            _logger = logger;
            _timeSchedulesDirectory = configManager.TimeSchedulesDirectory;

            _logger.LogInformation("路径初始化完成: TimeSchedulesDirectory={Directory}", _timeSchedulesDirectory);
        }

        /// <summary>
        /// 初始化所有时间计划
        /// </summary>
        public void Initialize()
        {
            try
            {
                EnsureDirectoryExists(_timeSchedulesDirectory);
                EnsureInitialScheduleExists();

                _logger.LogInformation("初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化失败: {Message}", ex.Message);
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
                _logger.LogInformation("目录已创建: {Path}", path);
            }
        }

        /// <summary>
        /// 首次初始化：当计划表目录为空时创建默认计划表作为初始数据
        /// </summary>
        private void EnsureInitialScheduleExists()
        {
            if (!Directory.Exists(_timeSchedulesDirectory)) return;

            var existingFiles = Directory.GetFiles(_timeSchedulesDirectory, "*.json");
            if (existingFiles.Length == 0)
            {
                var defaultSchedule = CreateDefaultSchedule();
                SaveSchedule(defaultSchedule);
                _logger.LogInformation("首次初始化：已创建默认计划表");
            }
        }

        /// <summary>
        /// 创建默认时间计划（仅用于首次初始化）
        /// </summary>
        private TimeSchedule CreateDefaultSchedule()
        {
            var now = DateTime.UtcNow;
            
            return new TimeSchedule
            {
                Id = "Default",
                Version = "1.0.0",
                Settings = new TimeScheduleSettings
                {
                    Metadata = new TimeScheduleMetadata
                    {
                        Name = "默认计划表",
                        Description = "",
                        CreatedAt = now.ToString("o"),
                        UpdatedAt = now.ToString("o")
                    }
                },
                Schedules = new List<TimeScheduleItem>()
            };
        }

        /// <summary>
        /// 加载所有时间计划
        /// </summary>
        public List<TimeSchedule> LoadAllSchedules()
        {
            try
            {
                var schedules = new List<TimeSchedule>();

                if (!Directory.Exists(_timeSchedulesDirectory))
                {
                    return schedules;
                }

                var files = Directory.GetFiles(_timeSchedulesDirectory, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var schedule = LoadScheduleFromFile(file);
                        if (schedule != null)
                        {
                            schedules.Add(schedule);
                            _scheduleCache[schedule.Id] = schedule;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "读取文件失败: {File}, 错误: {Message}", file, ex.Message);
                    }
                }

                return schedules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载所有时间计划失败: {Message}", ex.Message);
                return new List<TimeSchedule>();
            }
        }

        /// <summary>
        /// 根据指定ID加载时间计划（返回深拷贝）
        /// </summary>
        public TimeSchedule? LoadSchedule(string id)
        {
            try
            {
                // 先从缓存中查找
                if (_scheduleCache.TryGetValue(id, out var cachedSchedule))
                {
                    return DeepClone(cachedSchedule);
                }

                // 从文件加载
                var filePath = BuildScheduleFilePath(id);
                
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var schedule = LoadScheduleFromFile(filePath);
                if (schedule != null)
                {
                    _scheduleCache[id] = schedule;
                    return DeepClone(schedule);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载时间计划失败: {Id}, 错误: {Message}", id, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 深拷贝时间计划对象
        /// </summary>
        private TimeSchedule DeepClone(TimeSchedule source)
        {
            var json = JsonSerializer.Serialize(source, _jsonOptions);
            return JsonSerializer.Deserialize<TimeSchedule>(json, _jsonOptions) ?? source;
        }

        /// <summary>
        /// 从文件加载时间计划
        /// </summary>
        private TimeSchedule? LoadScheduleFromFile(string filePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                var schedule = JsonSerializer.Deserialize<TimeSchedule>(jsonContent, _jsonOptions);

                if (schedule != null)
                {
                    schedule.Id = Path.GetFileNameWithoutExtension(filePath);

                    if (string.IsNullOrEmpty(schedule.Settings.Metadata.CreatedAt))
                    {
                        schedule.Settings.Metadata.CreatedAt = DateTime.UtcNow.ToString("o");
                    }
                }

                return schedule;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON解析失败: {FilePath}, 错误: {Message}", filePath, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 保存时间计划（先写文件再更新缓存，确保一致性）
        /// </summary>
        public void SaveSchedule(TimeSchedule schedule)
        {
            try
            {
                if (string.IsNullOrEmpty(schedule.Id))
                {
                    throw new ArgumentException("时间计划ID不能为空");
                }

                // 更新修改时间
                schedule.Settings.Metadata.UpdatedAt = DateTime.UtcNow.ToString("o");

                // 先保存到文件
                var filePath = BuildScheduleFilePath(schedule.Id);
                SaveScheduleToFile(schedule, filePath);

                // 文件写入成功后再更新缓存
                _scheduleCache[schedule.Id] = DeepClone(schedule);

                // 触发事件
                OnScheduleChanged?.Invoke(schedule);

                _logger.LogInformation("时间计划保存成功: {Id}", schedule.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存时间计划失败: {Id}, 错误: {Message}", schedule.Id, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 保存时间计划到文件（原子写入：先写临时文件再替换）
        /// </summary>
        private void SaveScheduleToFile(TimeSchedule schedule, string filePath)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(schedule, _jsonOptions);
                var tempFile = filePath + ".tmp";

                File.WriteAllText(tempFile, jsonContent);
                File.Copy(tempFile, filePath, overwrite: true);
                File.Delete(tempFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "写入文件失败: {FilePath}, 错误: {Message}", filePath, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 删除时间计划
        /// </summary>
        /// <param name="id">计划表ID</param>
        /// <returns>删除成功返回 true</returns>
        public bool DeleteSchedule(string id)
        {
            try
            {
                var filePath = BuildScheduleFilePath(id);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _scheduleCache.Remove(id);
                    OnScheduleDeleted?.Invoke(id);
                    
                    _logger.LogInformation("时间计划删除成功: {Id}", id);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除时间计划失败: {Id}, 错误: {Message}", id, ex.Message);
                return false;
            }
        }

        #region 计划表操作

        /// <summary>
        /// 获取计划表列表（简化信息）
        /// </summary>
        /// <returns>计划表信息列表</returns>
        public List<ScheduleInfo> GetScheduleList()
        {
            try
            {
                var schedules = LoadAllSchedules();
                return schedules.Select(s => new ScheduleInfo
                {
                    Id = s.Id,
                    Name = s.Settings?.Metadata?.Name ?? s.Id,
                    Description = s.Settings?.Metadata?.Description,
                    AssociatedGroupId = s.Settings?.Metadata?.AssociatedGroupId ?? ScheduleGroup.DefaultGroupId,
                    IsEnabled = s.Settings?.Metadata?.IsEnabled ?? true,
                    DayOfWeek = s.Settings?.Metadata?.DayOfWeek ?? 0,
                    RotationCycleCount = s.Settings?.Metadata?.RotationCycleCount ?? 1,
                    RotationWeekIndex = s.Settings?.Metadata?.RotationWeekIndex ?? 0,
                    CreatedAt = TryParseDateTime(s.Settings?.Metadata?.CreatedAt),
                    UpdatedAt = TryParseDateTime(s.Settings?.Metadata?.UpdatedAt)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取计划表列表失败: {Message}", ex.Message);
                return new List<ScheduleInfo>();
            }
        }

        /// <summary>
        /// 创建新计划表（空白）
        /// </summary>
        /// <param name="id">计划表ID</param>
        /// <param name="name">计划表名称</param>
        /// <returns>新创建的计划表</returns>
        public TimeSchedule CreateNewSchedule(string id, string name)
        {
            var now = DateTime.UtcNow;
            
            var schedule = new TimeSchedule
            {
                Id = id,
                Version = "1.0.0",
                Settings = new TimeScheduleSettings
                {
                    Metadata = new TimeScheduleMetadata
                    {
                        Name = name,
                        Description = "",
                        AssociatedGroupId = ScheduleGroup.DefaultGroupId,
                        CreatedAt = now.ToString("o"),
                        UpdatedAt = now.ToString("o")
                    }
                },
                Schedules = new List<TimeScheduleItem>(),
                TimePoints = new List<CustomTimePoint>()
            };

            SaveSchedule(schedule);
            return schedule;
        }

        /// <summary>
        /// 复制计划表
        /// </summary>
        /// <param name="sourceId">源计划表ID</param>
        /// <param name="newId">新计划表ID</param>
        /// <returns>新计划表</returns>
        public TimeSchedule? CopySchedule(string sourceId, string newId)
        {
            try
            {
                var source = LoadSchedule(sourceId);
                if (source == null)
                {
                    _logger.LogWarning("源计划表不存在: {SourceId}", sourceId);
                    return null;
                }

                var now = DateTime.UtcNow;
                var newSchedule = new TimeSchedule
                {
                    Id = newId,
                    Version = source.Version,
                    Settings = new TimeScheduleSettings
                    {
                        Metadata = new TimeScheduleMetadata
                        {
                            Name = $"{source.Settings?.Metadata?.Name} (副本)",
                            Description = source.Settings?.Metadata?.Description ?? "",
                            CreatedAt = now.ToString("o"),
                            UpdatedAt = now.ToString("o")
                        }
                    },
                    Schedules = JsonSerializer.Deserialize<List<TimeScheduleItem>>(
                        JsonSerializer.Serialize(source.Schedules ?? [], _jsonOptions), _jsonOptions) ?? [],
                    TimePoints = JsonSerializer.Deserialize<List<CustomTimePoint>>(
                        JsonSerializer.Serialize(source.TimePoints ?? [], _jsonOptions), _jsonOptions) ?? []
                };

                SaveSchedule(newSchedule);
                return newSchedule;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制计划表失败: {SourceId} -> {NewId}, 错误: {Message}", sourceId, newId, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 重命名计划表
        /// </summary>
        /// <param name="id">计划表ID</param>
        /// <param name="newName">新名称</param>
        /// <returns>重命名成功返回 true</returns>
        public bool RenameSchedule(string id, string newName)
        {
            try
            {
                var schedule = LoadSchedule(id);
                if (schedule == null)
                {
                    return false;
                }

                schedule.Settings.Metadata.Name = newName;
                SaveSchedule(schedule);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重命名计划表失败: {Id}, 错误: {Message}", id, ex.Message);
                return false;
            }
        }

        public bool UpdateScheduleMetadata(string id, string name, string? description)
        {
            try
            {
                var schedule = LoadSchedule(id);
                if (schedule == null)
                {
                    return false;
                }

                schedule.Settings.Metadata.Name = name;
                schedule.Settings.Metadata.Description = description;
                SaveSchedule(schedule);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新计划表元数据失败: {Id}, 错误: {Message}", id, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 检查计划表是否存在
        /// </summary>
        /// <param name="id">计划表ID</param>
        /// <returns>存在返回 true</returns>
        public bool ScheduleExists(string id)
        {
            if (_scheduleCache.ContainsKey(id))
            {
                return true;
            }

            var filePath = BuildScheduleFilePath(id);
            return File.Exists(filePath);
        }

        #endregion

        #region 时间段操作

        /// <summary>
        /// 添加时间段
        /// </summary>
        /// <param name="scheduleId">计划表ID</param>
        /// <param name="segment">时间段</param>
        /// <returns>添加成功返回 true</returns>
        public bool AddTimeSegment(string scheduleId, TimeScheduleItem segment)
        {
            try
            {
                var schedule = LoadSchedule(scheduleId);
                if (schedule == null)
                {
                    return false;
                }

                schedule.Schedules ??= new List<TimeScheduleItem>();
                schedule.Schedules.Add(segment);
                SaveSchedule(schedule);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加时间段失败: {ScheduleId}, 错误: {Message}", scheduleId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 更新时间段
        /// </summary>
        /// <param name="scheduleId">计划表ID</param>
        /// <param name="segment">时间段（按 ID 匹配）</param>
        /// <returns>更新成功返回 true</returns>
        public bool UpdateTimeSegment(string scheduleId, TimeScheduleItem segment)
        {
            try
            {
                var schedule = LoadSchedule(scheduleId);
                if (schedule == null || schedule.Schedules == null)
                {
                    return false;
                }

                var index = schedule.Schedules.FindIndex(s => s.Id == segment.Id);
                if (index < 0)
                {
                    return false;
                }

                schedule.Schedules[index] = segment;
                SaveSchedule(schedule);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新时间段失败: {ScheduleId}, 错误: {Message}", scheduleId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 删除时间段
        /// </summary>
        /// <param name="scheduleId">计划表ID</param>
        /// <param name="segmentId">时间段ID</param>
        /// <returns>删除成功返回 true</returns>
        public bool RemoveTimeSegment(string scheduleId, string segmentId)
        {
            try
            {
                var schedule = LoadSchedule(scheduleId);
                if (schedule == null || schedule.Schedules == null)
                {
                    return false;
                }

                var removed = schedule.Schedules.RemoveAll(s => s.Id == segmentId);
                if (removed > 0)
                {
                    SaveSchedule(schedule);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除时间段失败: {ScheduleId}, 错误: {Message}", scheduleId, ex.Message);
                return false;
            }
        }

        #endregion

        #region 时间点操作

        /// <summary>
        /// 添加时间点
        /// </summary>
        /// <param name="scheduleId">计划表ID</param>
        /// <param name="timePoint">时间点</param>
        /// <returns>添加成功返回 true</returns>
        public bool AddTimePoint(string scheduleId, CustomTimePoint timePoint)
        {
            try
            {
                var schedule = LoadSchedule(scheduleId);
                if (schedule == null)
                {
                    return false;
                }

                schedule.TimePoints ??= new List<CustomTimePoint>();
                schedule.TimePoints.Add(timePoint);
                SaveSchedule(schedule);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加时间点失败: {ScheduleId}, 错误: {Message}", scheduleId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 更新时间点
        /// </summary>
        /// <param name="scheduleId">计划表ID</param>
        /// <param name="timePoint">时间点（按 ID 匹配）</param>
        /// <returns>更新成功返回 true</returns>
        public bool UpdateTimePoint(string scheduleId, CustomTimePoint timePoint)
        {
            try
            {
                var schedule = LoadSchedule(scheduleId);
                if (schedule == null || schedule.TimePoints == null)
                {
                    return false;
                }

                var index = schedule.TimePoints.FindIndex(t => t.Id == timePoint.Id);
                if (index < 0)
                {
                    return false;
                }

                schedule.TimePoints[index] = timePoint;
                SaveSchedule(schedule);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新时间点失败: {ScheduleId}, 错误: {Message}", scheduleId, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 删除时间点
        /// </summary>
        /// <param name="scheduleId">计划表ID</param>
        /// <param name="timePointId">时间点ID</param>
        /// <returns>删除成功返回 true</returns>
        public bool RemoveTimePoint(string scheduleId, string timePointId)
        {
            try
            {
                var schedule = LoadSchedule(scheduleId);
                if (schedule == null || schedule.TimePoints == null)
                {
                    return false;
                }

                var removed = schedule.TimePoints.RemoveAll(t => t.Id == timePointId);
                if (removed > 0)
                {
                    SaveSchedule(schedule);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除时间点失败: {ScheduleId}, 错误: {Message}", scheduleId, ex.Message);
                return false;
            }
        }

        #endregion

        /// <summary>
        /// 尝试解析日期时间
        /// </summary>
        private DateTime? TryParseDateTime(string? dateTimeString)
        {
            if (string.IsNullOrEmpty(dateTimeString))
            {
                return null;
            }

            if (DateTime.TryParse(dateTimeString, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// 验证时间计划
        /// </summary>
        public bool ValidateSchedule(TimeSchedule schedule)
        {
            if (string.IsNullOrEmpty(schedule.Id))
            {
                return false;
            }

            if (string.IsNullOrEmpty(schedule.Settings.Metadata.Name))
            {
                return false;
            }

            if (schedule.Schedules == null || schedule.Schedules.Count == 0)
            {
                return false;
            }

            foreach (var item in schedule.Schedules)
            {
                if (string.IsNullOrEmpty(item.Id))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(item.Name))
                {
                    return false;
                }

                if (!IsValidTimeFormat(item.StartTime))
                {
                    return false;
                }

                if (!IsValidTimeFormat(item.EndTime))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 验证时间格式（HH:mm:ss）
        /// </summary>
        private bool IsValidTimeFormat(string time)
        {
            if (string.IsNullOrEmpty(time))
            {
                return false;
            }

            var parts = time.Split(':');
            if (parts.Length != 3)
            {
                return false;
            }

            return int.TryParse(parts[0], out var hour) && hour >= 0 && hour < 24 &&
                   int.TryParse(parts[1], out var minute) && minute >= 0 && minute < 60 &&
                   int.TryParse(parts[2], out var second) && second >= 0 && second < 60;
        }

        /// <summary>
        /// 验证计划表ID合法性，防止路径遍历攻击
        /// </summary>
        private static bool IsValidScheduleId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            return ScheduleIdPattern.IsMatch(id);
        }

        /// <summary>
        /// 构建计划表文件路径（含安全验证）
        /// </summary>
        private string BuildScheduleFilePath(string id)
        {
            if (!IsValidScheduleId(id))
                throw new ArgumentException($"计划表ID不合法: {id}，仅允许字母、数字、下划线和连字符");

            var filePath = Path.Combine(_timeSchedulesDirectory, $"{id}.json");
            var fullPath = Path.GetFullPath(filePath);
            var normalizedBase = Path.GetFullPath(_timeSchedulesDirectory);

            if (!fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException($"路径遍历攻击检测: {id}");

            return filePath;
        }

        /// <summary>
        /// 刷新缓存
        /// </summary>
        public void RefreshCache()
        {
            _scheduleCache.Clear();
            LoadAllSchedules();
        }

        /// <summary>
        /// 清除指定计划的缓存
        /// </summary>
        public void ClearCache(string id)
        {
            _scheduleCache.Remove(id);
        }
    }
}