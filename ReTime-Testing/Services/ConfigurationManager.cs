using ReTime_Testing.Models;
using System.IO;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 配置管理器（单例）
    /// 职责：路径注册中心——管理应用所有文件和目录的路径
    /// 配置的读取、保存、缓存、校验、通知由 SettingsService 负责
    /// JSON 文件 I/O 由 JsonConfigProvider 负责
    /// </summary>
    public class ConfigurationManager : IConfigurationManager
    {
        private static readonly Lazy<ConfigurationManager> _instance =
            new Lazy<ConfigurationManager>(() => new ConfigurationManager());

        /// <summary>
        /// 获取全局唯一实例
        /// </summary>
        public static ConfigurationManager Instance => _instance.Value;

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
        /// 获取日志文件目录路径
        /// </summary>
        public string LogsDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 全局配置变更事件（委托给 SettingsService）
        /// </summary>
        public event Action<GlobalSetting>? OnGlobalSettingChanged
        {
            add => SettingsService.Instance.OnGlobalSettingChanged += value;
            remove => SettingsService.Instance.OnGlobalSettingChanged -= value;
        }

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
                var applicationRootDirectory = AppContext.BaseDirectory;
                ApplicationRootDirectory = Path.GetFullPath(applicationRootDirectory);

                DataDirectory = Path.Combine(ApplicationRootDirectory, "data");
                GlobalSettingFilePath = Path.Combine(DataDirectory, "Setting.json");
                ConfigsDirectory = Path.Combine(DataDirectory, "Config");
                TimeSchedulesDirectory = Path.Combine(ConfigsDirectory, "TimeTopDesktop", "TimeSchedules");
                TimeTopSettingFilePath = Path.Combine(ConfigsDirectory, "TimeTopDesktop", "TimeTopSetting.json");
                LogsDirectory = Path.Combine(DataDirectory, "Logs");

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
                EnsureDirectoryExists(LogsDirectory);

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
                SettingsService.Instance.SaveGlobalSetting(defaultSetting);
                Logger.Info("ReTime_Testing.Services.ConfigurationManager",
                    $"全局配置文件已创建: {GlobalSettingFilePath}");
            }
        }

        /// <summary>
        /// 确保TimeTop设置文件存在
        /// </summary>
        private void EnsureTimeTopSettingExists()
        {
            if (!File.Exists(TimeTopSettingFilePath))
            {
                var defaultSetting = new TimeTopSetting();
                SettingsService.Instance.SaveTimeTopSetting(defaultSetting);
                Logger.Info("ReTime_Testing.Services.ConfigurationManager",
                    "TimeTop设置文件已创建");
            }
        }

        #region 兼容性委托方法（委托给 SettingsService）

        /// <summary>
        /// 加载全局配置（委托给 SettingsService）
        /// </summary>
        public GlobalSetting LoadGlobalSetting() => SettingsService.Instance.GetGlobalSetting();

        /// <summary>
        /// 保存全局配置（委托给 SettingsService）
        /// </summary>
        public void SaveGlobalSetting(GlobalSetting setting) => SettingsService.Instance.SaveGlobalSetting(setting);

        /// <summary>
        /// 重置全局配置为默认值（委托给 SettingsService）
        /// </summary>
        public void ResetGlobalSetting() => SettingsService.Instance.ResetGlobalSetting();

        /// <summary>
        /// 刷新全局配置缓存（委托给 SettingsService）
        /// </summary>
        public void RefreshGlobalSettingCache() => SettingsService.Instance.RefreshGlobalSettingCache();

        /// <summary>
        /// 获取缓存的全局配置（委托给 SettingsService）
        /// </summary>
        public GlobalSetting GetCachedGlobalSetting() => SettingsService.Instance.GetGlobalSetting();

        /// <summary>
        /// 加载TimeTop设置（委托给 SettingsService）
        /// </summary>
        public TimeTopSetting LoadTimeTopSetting() => SettingsService.Instance.GetTimeTopSetting();

        /// <summary>
        /// 保存TimeTop设置（委托给 SettingsService）
        /// </summary>
        public void SaveTimeTopSetting(TimeTopSetting setting) => SettingsService.Instance.SaveTimeTopSetting(setting);

        #endregion
    }
}