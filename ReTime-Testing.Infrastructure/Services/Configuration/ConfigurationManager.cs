using ReTime_Testing.Models;
using System.IO;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 配置管理器
    /// 职责：路径注册中心——管理应用所有文件和目录的路径
    /// 配置的读取、保存、缓存、校验、通知由 ISettingsService 负责
    /// JSON 文件 I/O 由 JsonConfigProvider 负责
    /// </summary>
    public class ConfigurationManager : IConfigurationManager
    {
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
        /// 获取计划表组配置目录路径
        /// </summary>
        public string ScheduleGroupsDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 获取TimeTop设置文件路径
        /// </summary>
        public string TimeTopSettingFilePath { get; private set; } = string.Empty;

        /// <summary>
        /// 获取日志文件目录路径
        /// </summary>
        public string LogsDirectory { get; private set; } = string.Empty;

        /// <summary>
        /// 构造函数（支持 DI 注入）
        /// </summary>
        public ConfigurationManager()
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
                ScheduleGroupsDirectory = Path.Combine(ConfigsDirectory, "TimeTopDesktop", "ScheduleGroups");
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
        /// 仅创建目录，不创建默认配置文件（由 SettingsService 按需创建）
        /// </summary>
        public void InitializeDirectories()
        {
            try
            {
                EnsureDirectoryExists(DataDirectory);
                EnsureDirectoryExists(ConfigsDirectory);
                EnsureDirectoryExists(TimeSchedulesDirectory);
                EnsureDirectoryExists(ScheduleGroupsDirectory);
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
    }
}