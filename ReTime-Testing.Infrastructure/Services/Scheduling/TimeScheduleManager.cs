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
    /// 时间计划管理器的主要实现
    /// 职责：管理时间计划文件的创建、读取、保存、删除
    /// </summary>
    public class TimeScheduleManager : ITimeScheduleManager
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly Dictionary<string, TimeSchedule> _scheduleCache = new();
        private string _timeSchedulesDirectory = string.Empty;

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
        public TimeScheduleManager(IConfigurationManager configManager)
        {
            _timeSchedulesDirectory = configManager.TimeSchedulesDirectory;

            Logger.Info("ReTime_Testing.Services.TimeScheduleManager",
                $"路径初始化完成: TimeSchedulesDirectory={_timeSchedulesDirectory}");
        }

        /// <summary>
        /// 初始化所有时间计划，
        /// </summary>
        public void Initialize()
        {
            try
            {
                EnsureDirectoryExists(_timeSchedulesDirectory);
                EnsureDefaultScheduleExists();

                Logger.Info("ReTime_Testing.Services.TimeScheduleManager", "初始化完成");
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
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
                Logger.Info("ReTime_Testing.Services.TimeScheduleManager",
                    $"目录已创建: {path}");
            }
        }

        /// <summary>
        /// 确保默认时间计划存在
        /// </summary>
        private void EnsureDefaultScheduleExists()
        {
            var defaultFilePath = Path.Combine(_timeSchedulesDirectory, "Default.json");
            
            if (!File.Exists(defaultFilePath))
            {
                var defaultSchedule = CreateDefaultSchedule();
                SaveSchedule(defaultSchedule);
                Logger.Info("ReTime_Testing.Services.TimeScheduleManager", "默认时间计划已创建");
            }
        }

        /// <summary>
        /// 创建默认时间计划
        /// </summary>
        public TimeSchedule CreateDefaultSchedule()
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
                        Name = "默认工作时间",
                        Description = "标准的 9:00 - 18:00 工作时间",
                        CreatedAt = now.ToString("o"),
                        UpdatedAt = now.ToString("o")
                    }
                },
                Schedules = new List<TimeScheduleItem>
                {
                    new TimeScheduleItem
                    {
                        Id = "schedule_001",
                        Name = "工作时间段",
                        StartTime = "09:00:00",
                        EndTime = "18:00:00"
                    }
                }
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
                        Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                            $"读取文件失败: {file}, 错误: {ex.Message}");
                    }
                }

                return schedules;
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"加载所有时间计划失败: {ex.Message}", ex);
                return new List<TimeSchedule>();
            }
        }

        /// <summary>
        /// 根据指定ID的时间计划
        /// </summary>
        public TimeSchedule? LoadSchedule(string id)
        {
            try
            {
                // 先从缓存中查找
                if (_scheduleCache.TryGetValue(id, out var cachedSchedule))
                {
                    return cachedSchedule;
                }

                // 从文件加载
                var filePath = Path.Combine(_timeSchedulesDirectory, $"{id}.json");
                
                if (!File.Exists(filePath))
                {
                    return null;
                }

                return LoadScheduleFromFile(filePath);
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"加载时间计划失败: {id}, 错误: {ex.Message}", ex);
                return null;
            }
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
                    // 使用文件名作为ID，确保文件名与内部ID一致
                    schedule.Id = Path.GetFileNameWithoutExtension(filePath);

                    if (string.IsNullOrEmpty(schedule.Settings.Metadata.CreatedAt))
                    {
                        // 添加创建时间戳作为当前时间
                        schedule.Settings.Metadata.CreatedAt = DateTime.UtcNow.ToString("o");
                        SaveScheduleToFile(schedule, filePath);
                    }
                }

                return schedule;
            }
            catch (JsonException ex)
            {
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"JSON解析失败: {filePath}, 错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存时间计划
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

                // 保存到文件
                var filePath = Path.Combine(_timeSchedulesDirectory, $"{schedule.Id}.json");
                SaveScheduleToFile(schedule, filePath);

                // 更新缓存
                _scheduleCache[schedule.Id] = schedule;

                // 触发事件
                OnScheduleChanged?.Invoke(schedule);

                Logger.Info("ReTime_Testing.Services.TimeScheduleManager",
                    $"时间计划保存成功: {schedule.Id}");
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"保存时间计划失败: {schedule.Id}, 错误: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 保存时间计划到文件
        /// </summary>
        private void SaveScheduleToFile(TimeSchedule schedule, string filePath)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(schedule, _jsonOptions);
                File.WriteAllText(filePath, jsonContent);
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"写入文件失败: {filePath}, 错误: {ex.Message}");
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
                var filePath = Path.Combine(_timeSchedulesDirectory, $"{id}.json");
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _scheduleCache.Remove(id);
                    OnScheduleDeleted?.Invoke(id);
                    
                    Logger.Info("ReTime_Testing.Services.TimeScheduleManager",
                        $"时间计划删除成功: {id}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"删除时间计划失败: {id}, 错误: {ex.Message}", ex);
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
                    CreatedAt = TryParseDateTime(s.Settings?.Metadata?.CreatedAt),
                    UpdatedAt = TryParseDateTime(s.Settings?.Metadata?.UpdatedAt)
                }).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"获取计划表列表失败: {ex.Message}", ex);
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
                    Logger.Warn("ReTime_Testing.Services.TimeScheduleManager",
                        $"源计划表不存在: {sourceId}");
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
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"复制计划表失败: {sourceId} -> {newId}, 错误: {ex.Message}", ex);
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
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"重命名计划表失败: {id}, 错误: {ex.Message}", ex);
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

            var filePath = Path.Combine(_timeSchedulesDirectory, $"{id}.json");
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
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"添加时间段失败: {scheduleId}, 错误: {ex.Message}", ex);
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
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"更新时间段失败: {scheduleId}, 错误: {ex.Message}", ex);
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
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"删除时间段失败: {scheduleId}, 错误: {ex.Message}", ex);
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
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"添加时间点失败: {scheduleId}, 错误: {ex.Message}", ex);
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
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"更新时间点失败: {scheduleId}, 错误: {ex.Message}", ex);
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
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"删除时间点失败: {scheduleId}, 错误: {ex.Message}", ex);
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