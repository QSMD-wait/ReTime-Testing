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
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                ApplicationRootDirectory = assemblyDirectory ?? Environment.CurrentDirectory;

                DataDirectory = Path.Combine(ApplicationRootDirectory, "data");
                GlobalSettingFilePath = Path.Combine(DataDirectory, "Setting.json");
                ConfigsDirectory = Path.Combine(DataDirectory, "Config");

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
                EnsureFileExists(GlobalSettingFilePath);
                EnsureDirectoryExists(ConfigsDirectory);

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
        /// 确保文件存在
        /// </summary>
        private void EnsureFileExists(string path)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "{}");
                Logger.Info("ReTime_Testing.Services.ConfigurationManager", 
                    $"文件已创建: {path}");
            }
        }

        /// <summary>
        /// 加载全局配置
        /// </summary>
        public GlobalSetting LoadGlobalSetting()
        {
            try
            {
                EnsureFileExists(GlobalSettingFilePath);

                string jsonContent = File.ReadAllText(GlobalSettingFilePath);

                if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Trim() == "{}")
                {
                    Logger.Info("ReTime_Testing.Services.ConfigurationManager", 
                        "全局配置文件为空，创建默认配置");
                    var defaultSetting = new GlobalSetting();
                    SaveGlobalSetting(defaultSetting);
                    return defaultSetting;
                }

                var setting = JsonSerializer.Deserialize<GlobalSetting>(jsonContent, _jsonOptions)
                    ?? new GlobalSetting();

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
        /// 保存全局配置
        /// </summary>
        public void SaveGlobalSetting(GlobalSetting setting)
        {
            try
            {
                EnsureFileExists(GlobalSettingFilePath);

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
    }
}