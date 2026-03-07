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
    /// 时间计划管理器（单例）
    /// 职责：管理时间计划配置文件的创建、读取、更新、删除
    /// </summary>
    public class TimeScheduleManager
    {
        private static readonly Lazy<TimeScheduleManager> _instance =
            new Lazy<TimeScheduleManager>(() => new TimeScheduleManager());

        /// <summary>
        /// 获取全局唯一实例
        /// </summary>
        public static TimeScheduleManager Instance => _instance.Value;

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
        /// 获取时间计划目录路径
        /// </summary>
        public string TimeSchedulesDirectory => _timeSchedulesDirectory;

        /// <summary>
        /// 时间计划变更事件
        /// </summary>
        public event Action<TimeSchedule>? OnScheduleChanged;

        /// <summary>
        /// 时间计划添加事件
        /// </summary>
        public event Action<TimeSchedule>? OnScheduleAdded;

        /// <summary>
        /// 时间计划删除事件
        /// </summary>
        public event Action<string>? OnScheduleDeleted;

        private TimeScheduleManager()
        {
            InitializePaths();
        }

        /// <summary>
        /// 初始化路径
        /// </summary>
        private void InitializePaths()
        {
            try
            {
                var configManager = ConfigurationManager.Instance;
                _timeSchedulesDirectory = configManager.TimeSchedulesDirectory;

                Logger.Info("ReTime_Testing.Services.TimeScheduleManager",
                    $"路径初始化完成: TimeSchedulesDirectory={_timeSchedulesDirectory}");
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"路径初始化失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 初始化（启动时调用）
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
                            $"加载文件失败: {file}, 错误: {ex.Message}");
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
        /// 加载指定ID的时间计划
        /// </summary>
        public TimeSchedule? LoadSchedule(string id)
        {
            try
            {
                // 先从缓存查找
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
                
                if (schedule != null && string.IsNullOrEmpty(schedule.Settings.Metadata.CreatedAt))
                {
                    // 首次加载时设置创建时间
                    schedule.Settings.Metadata.CreatedAt = DateTime.UtcNow.ToString("o");
                    SaveScheduleToFile(schedule, filePath);
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
        public void DeleteSchedule(string id)
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
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.TimeScheduleManager",
                    $"删除时间计划失败: {id}, 错误: {ex.Message}", ex);
                throw;
            }
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
    }
}
