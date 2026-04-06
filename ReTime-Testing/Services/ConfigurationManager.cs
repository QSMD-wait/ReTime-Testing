using ReTime_Testing.Models;
using System.IO;
using System.Text.Json;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 配置管理器（单例）
    /// 职责：管理应用配置文件的创建、读取、更新、删除
    /// </summary>
    public class ConfigurationManager
    {
        private static readonly Lazy<ConfigurationManager> _instance =
            new Lazy<ConfigurationManager>(() => new ConfigurationManager());

        /// <summary>
        /// 获取全局唯一实例
        /// </summary>
        public static ConfigurationManager Instance => _instance.Value;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private GlobalSetting? _cachedGlobalSetting;

        /// <summary>
        /// 获取应用根目录
        /// </summary>
        public string ApplicationRootDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 获取数据目录
        /// </summary>
        public string DataDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 获取全局配置文件路径
        /// </summary>
        public string GlobalSettingFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// 获取配置文件存储目录路径
        /// </summary>
        public string ConfigsDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 获取时间计划表目录路径
        /// </summary>
        public string TimeSchedulesDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 获取TimeTop设置文件路径
        /// </summary>
        public string TimeTopSettingFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// 全局配置变更事件
        /// </summary>
        public event Action<GlobalSetting>? OnGlobalSettingChanged;

        private ConfigurationManager()
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
                // 使用 AppContext.BaseDirectory 替代 Assembly.Location，避免单文件发布警告
                var applicationRootDirectory = AppContext.BaseDirectory;
                ApplicationRootDirectory = Path.GetFullPath(applicationRootDirectory);

                DataDirectory = Path.Combine(ApplicationRootDirectory, "data");
                GlobalSettingFilePath = Path.Combine(DataDirectory, "Setting.json");
                ConfigsDirectory = Path.Combine(DataDirectory, "Config");
                TimeSchedulesDirectory = Path.Combine(ConfigsDirectory, "TimeTopDesktop", "TimeSchedules");
                TimeTopSettingFilePath = Path.Combine(ConfigsDirectory, "TimeTopDesktop", "TimeTopSetting.json");

                Logger.Info("ReTime_Testing.Services.ConfigurationManager",
                    $"路径初始化完成: Root={ApplicationRootDirectory}, Data={DataDirectory}");
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager", 
                    $"路径初始化失败: {ex.Message}", ex);
                throw new ConfigurationException("路径初始化失败", ex);
            }
        }

        /// <summary>
        /// 初始化目录结构（启动时调用）
        /// </summary>
        public void InitializeDirectories()
        {
            try
            {
                EnsureDirectoryExists(DataDirectory);
                EnsureGlobalSettingExists();
                EnsureDirectoryExists(ConfigsDirectory);
                EnsureDirectoryExists(TimeSchedulesDirectory);
                EnsureTimeTopSettingExists();

                Logger.Info("ReTime_Testing.Services.ConfigurationManager", 
                    "目录结构初始化完成");
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager", 
                    $"目录结构初始化失败: {ex.Message}", ex);
                throw new ConfigurationException("目录结构初始化失败", ex);
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
                Logger.Info("ReTime_Testing.Services.ConfigurationManager", 
                    $"目录已创建: {path}");
            }
        }

        /// <summary>
        /// 确保全局配置文件存在
        /// </summary>
        private void EnsureGlobalSettingExists()
        {
            if (!File.Exists(GlobalSettingFilePath))
            {
                var defaultSetting = new GlobalSetting();
                SaveGlobalSetting(defaultSetting);
                Logger.Info("ReTime_Testing.Services.ConfigurationManager", 
                    $"全局配置文件已创建: {GlobalSettingFilePath}");
            }
        }

        /// <summary>
        /// 加载全局配置
        /// </summary>
        public GlobalSetting LoadGlobalSetting()
        {
            try
            {
                if (!File.Exists(GlobalSettingFilePath))
                {
                    Logger.Warn("ReTime_Testing.Services.ConfigurationManager", 
                        "全局配置文件不存在，创建默认配置");
                    var newSetting = new GlobalSetting();
                    SaveGlobalSetting(newSetting);
                    return newSetting;
                }

                string jsonContent = File.ReadAllText(GlobalSettingFilePath);

                if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Trim() == "{}")
                {
                    Logger.Info("ReTime_Testing.Services.ConfigurationManager", 
                        "全局配置文件为空，创建默认配置");
                    var newSetting = new GlobalSetting();
                    SaveGlobalSetting(newSetting);
                    return newSetting;
                }

                var setting = JsonSerializer.Deserialize<GlobalSetting>(jsonContent, _jsonOptions)
                    ?? new GlobalSetting();

                // 版本检查
                if (string.IsNullOrEmpty(setting.Version) || setting.Version != "1.0.0")
                {
                    Logger.Warn("ReTime_Testing.Services.ConfigurationManager",
                        $"全局配置文件版本不匹配: {setting.Version}，使用默认配置");
                    var defaultSetting = new GlobalSetting();
                    SaveGlobalSetting(defaultSetting);
                    return defaultSetting;
                }

                // 填充缺失字段的默认值
                var defaults = new GlobalSetting();
                setting = FillMissingFields(setting, defaults);

                _cachedGlobalSetting = setting;

                Logger.Info("ReTime_Testing.Services.ConfigurationManager", 
                    "全局配置加载成功");

                return setting;
            }
            catch (JsonException ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager", 
                    $"全局配置文件 JSON 解析失败: {ex.Message}", ex);

                var defaultSetting = new GlobalSetting();
                SaveGlobalSetting(defaultSetting);
                return defaultSetting;
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager", 
                    $"全局配置加载失败: {ex.Message}", ex);
                throw new ConfigurationException("全局配置加载失败", ex);
            }
        }

        /// <summary>
        /// 填充缺失字段的默认值
        /// </summary>
        private GlobalSetting FillMissingFields(GlobalSetting target, GlobalSetting defaults)
        {
            if (string.IsNullOrEmpty(target.Version))
                target.Version = defaults.Version;

            return target;
        }

        /// <summary>
        /// 保存全局配置
        /// </summary>
        public void SaveGlobalSetting(GlobalSetting setting)
        {
            try
            {
                // 确保目录存在
                EnsureDirectoryExists(DataDirectory);

                string jsonContent = JsonSerializer.Serialize(setting, _jsonOptions);
                File.WriteAllText(GlobalSettingFilePath, jsonContent);

                _cachedGlobalSetting = setting;

                Logger.Info("ReTime_Testing.Services.ConfigurationManager", 
                    "全局配置保存成功");

                OnGlobalSettingChanged?.Invoke(setting);
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager", 
                    $"全局配置保存失败: {ex.Message}", ex);
                throw new ConfigurationException("全局配置保存失败", ex);
            }
        }

        /// <summary>
        /// 重置全局配置为默认值
        /// </summary>
        public void ResetGlobalSetting()
        {
            try
            {
                var defaultSetting = new GlobalSetting();
                SaveGlobalSetting(defaultSetting);

                Logger.Info("ReTime_Testing.Services.ConfigurationManager", 
                    "全局配置已重置为默认值");
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager", 
                    $"全局配置重置失败: {ex.Message}", ex);
                throw new ConfigurationException("全局配置重置失败", ex);
            }
        }

        /// <summary>
        /// 刷新全局配置缓存
        /// </summary>
        public void RefreshGlobalSettingCache()
        {
            _cachedGlobalSetting = null;
            LoadGlobalSetting();
        }

        /// <summary>
        /// 获取缓存的全局配置（如果不存在则加载）
        /// </summary>
        public GlobalSetting GetCachedGlobalSetting()
        {
            return _cachedGlobalSetting ?? LoadGlobalSetting();
        }

        /// <summary>
        /// 加载TimeTop设置
        /// </summary>
        public TimeTopSetting LoadTimeTopSetting()
        {
            try
            {
                EnsureTimeTopSettingExists();
                string jsonContent = File.ReadAllText(TimeTopSettingFilePath);

                // 检查文件是否为空
                if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Trim() == "{}")
                {
                    Logger.Info("ReTime_Testing.Services.ConfigurationManager",
                        "TimeTop设置文件为空，创建默认配置");
                    var newSetting = new TimeTopSetting();
                    SaveTimeTopSetting(newSetting);
                    return newSetting;
                }

                // 反序列化
                var setting = JsonSerializer.Deserialize<TimeTopSetting>(jsonContent, _jsonOptions);

                if (setting == null)
                {
                    Logger.Warn("ReTime_Testing.Services.ConfigurationManager",
                        "TimeTop设置文件解析失败，使用默认配置");
                    return new TimeTopSetting();
                }

                // 版本检查
                if (string.IsNullOrEmpty(setting.Version) || setting.Version != "1.0.0")
                {
                    Logger.Warn("ReTime_Testing.Services.ConfigurationManager",
                        $"TimeTop设置文件版本不匹配: {setting.Version}，使用默认配置");
                    return new TimeTopSetting();
                }

                // 填充缺失字段的默认值
                var defaults = new TimeTopSetting();
                setting = FillMissingFields(setting, defaults);

                return setting;
            }
            catch (JsonException ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager",
                    $"TimeTop设置 JSON 解析失败: {ex.Message}", ex);
                return new TimeTopSetting();
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager",
                    $"加载TimeTop设置失败: {ex.Message}", ex);
                return new TimeTopSetting();
            }
        }

        /// <summary>
        /// 填充缺失字段的默认值
        /// </summary>
        private TimeTopSetting FillMissingFields(TimeTopSetting target, TimeTopSetting defaults)
        {
            if (string.IsNullOrEmpty(target.Version))
                target.Version = defaults.Version;

            if (string.IsNullOrEmpty(target.SelectedScheduleId))
                target.SelectedScheduleId = defaults.SelectedScheduleId;

            // 填充时间设置字段
            if (target.TimeSettings == null)
                target.TimeSettings = new TimeSettingsData();

            if (target.TimeSettings.Calibration == null)
                target.TimeSettings.Calibration = new CalibrationSettings();

            if (target.TimeSettings.Fallback == null)
                target.TimeSettings.Fallback = new FallbackSettings();

            if (target.TimeSettings.Threshold == null)
                target.TimeSettings.Threshold = new ThresholdSettings();

            // 确保 Calibration.Enabled 使用默认值（默认关闭）
            // 注意：JSON 反序列化时如果字段不存在会使用类的默认值

            return target;
        }

        /// <summary>
        /// 保存TimeTop设置
        /// </summary>
        public void SaveTimeTopSetting(TimeTopSetting setting)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(setting, _jsonOptions);
                File.WriteAllText(TimeTopSettingFilePath, jsonContent);
                Logger.Info("ReTime_Testing.Services.ConfigurationManager",
                    "TimeTop设置保存成功");
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager",
                    $"保存TimeTop设置失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 确保TimeTop设置文件存在
        /// </summary>
        private void EnsureTimeTopSettingExists()
        {
            if (!File.Exists(TimeTopSettingFilePath))
            {
                var defaultSetting = new TimeTopSetting
                {
                    Version = "1.0.0",
                    SelectedScheduleId = "Default",
                    EnableTimeSchedule = true,
                    TimeSettings = new TimeSettingsData
                    {
                        Calibration = new CalibrationSettings
                        {
                            Enabled = false,
                            IntervalSeconds = 300,
                            TimeoutSeconds = 3,
                            MaxRetryCount = 5,
                            BackoffMultiplier = 2.0
                        },
                        Fallback = new FallbackSettings
                        {
                            OnStartFailure = "systemTime",
                            OnRuntimeFailure = "keepCurrent"
                        },
                        Threshold = new ThresholdSettings
                        {
                            CalibrationTriggerSeconds = 5,
                            WarningThresholdSeconds = 60,
                            SleepThresholdMinutes = 5
                        }
                    }
                };
                SaveTimeTopSetting(defaultSetting);
                Logger.Info("ReTime_Testing.Services.ConfigurationManager",
                    "TimeTop设置文件已创建");
            }
        }

        }
}
