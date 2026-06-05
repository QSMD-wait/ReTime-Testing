using ReTime_Testing.Models;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 配置管理器（单例）
    /// 职责：管理应用配置文件的创建、读取、更新、删除
    /// </summary>
    public class ConfigurationManager : IConfigurationManager
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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
                // 文件不存在 → 创建全量默认配置并写入
                if (!File.Exists(GlobalSettingFilePath))
                {
                    Logger.Warn("ReTime_Testing.Services.ConfigurationManager",
                        "全局配置文件不存在，创建默认配置");
                    var newSetting = new GlobalSetting();
                    SaveGlobalSetting(newSetting);
                    return newSetting;
                }

                string jsonContent = File.ReadAllText(GlobalSettingFilePath);

                // 文件为空或空JSON → 写入全量默认配置
                if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Trim() == "{}")
                {
                    Logger.Info("ReTime_Testing.Services.ConfigurationManager",
                        "全局配置文件为空，写入默认配置");
                    var newSetting = new GlobalSetting();
                    SaveGlobalSetting(newSetting);
                    return newSetting;
                }

                // 解析 JSON
                JsonNode? rootNode;
                try
                {
                    rootNode = JsonNode.Parse(jsonContent, null, new JsonDocumentOptions { AllowTrailingCommas = true });
                }
                catch (JsonException ex)
                {
                    // JSON 损坏 → 使用硬编码默认值继续，不回写文件
                    Logger.Error("ReTime_Testing.Services.ConfigurationManager",
                        $"全局配置文件 JSON 语法错误: {ex.Message}，使用默认配置（不覆盖原文件）", ex);
                    return new GlobalSetting();
                }

                if (rootNode == null)
                {
                    return new GlobalSetting();
                }

                // 逐域反序列化（缺失域由模型构造函数提供默认值）
                var result = new GlobalSetting();
                result.Version = TryGetString(rootNode, "version") ?? result.Version;
                result.Basic = TryDeserializeDomain<BasicSetting>(rootNode, "basic") ?? result.Basic;

                _cachedGlobalSetting = result;

                Logger.Info("ReTime_Testing.Services.ConfigurationManager",
                    "全局配置加载成功");

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager",
                    $"全局配置加载失败: {ex.Message}，使用默认配置（不覆盖原文件）");
                return new GlobalSetting();
            }
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

                // 解析为 JsonNode DOM（仅做语法检查，不做类型校验）
                JsonNode? rootNode;
                try
                {
                    rootNode = JsonNode.Parse(jsonContent, null, new JsonDocumentOptions { AllowTrailingCommas = true });
                }
                catch (JsonException ex)
                {
                    Logger.Error("ReTime_Testing.Services.ConfigurationManager",
                        $"TimeTop设置 JSON 语法错误: {ex.Message}，使用默认配置（不覆盖原文件）", ex);
                    return new TimeTopSetting();
                }

                if (rootNode == null)
                {
                    return new TimeTopSetting();
                }

                // 逐域反序列化：某个域解析失败仅回退该域
                var result = new TimeTopSetting();

                // version
                result.Version = TryGetString(rootNode, "version") ?? result.Version;

                // 简单域：整域反序列化
                result.Schedule = TryDeserializeDomain<ScheduleConfig>(rootNode, "schedule") ?? result.Schedule;
                result.ProgressBar = TryDeserializeDomain<ProgressBarConfig>(rootNode, "progressBar") ?? result.ProgressBar;
                result.Behavior = TryDeserializeDomain<ProgressBarBehaviorConfig>(rootNode, "behavior") ?? result.Behavior;
                result.StateStyles = TryDeserializeDomain<StateStylesConfig>(rootNode, "stateStyles") ?? result.StateStyles;
                result.DefaultBehavior = TryDeserializeDomain<ScheduleBehaviorData>(rootNode, "defaultBehavior") ?? result.DefaultBehavior;

                // 复杂域：先尝试整域，失败则逐子域 + 逐属性
                result.Calibration = DeserializeCalibrationDomain(rootNode) ?? result.Calibration;
                result.TextOverlay = DeserializeTextOverlayDomain(rootNode) ?? result.TextOverlay;

                // window 域
                result.Window = TryDeserializeDomain<WindowConfig>(rootNode, "window") ?? result.Window;

                // 版本检查
                if (string.IsNullOrEmpty(result.Version) || result.Version != "1.0.0")
                {
                    Logger.Warn("ReTime_Testing.Services.ConfigurationManager",
                        $"TimeTop设置版本不匹配: {result.Version}，填充默认值");
                }

                // 填充缺失字段 + 值钳位
                var defaults = new TimeTopSetting();
                result = FillMissingFields(result, defaults);

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("ReTime_Testing.Services.ConfigurationManager",
                    $"加载TimeTop设置失败: {ex.Message}，使用默认配置（不覆盖原文件）", ex);
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

            // schedule 域
            target.Schedule ??= new ScheduleConfig();
            if (string.IsNullOrEmpty(target.Schedule.SelectedId))
                target.Schedule.SelectedId = defaults.Schedule.SelectedId;

            // progressBar 域
            target.ProgressBar ??= new ProgressBarConfig();
            target.ProgressBar.Height = Math.Max(1, target.ProgressBar.Height);
            target.ProgressBar.CornerRadius = Math.Max(0, target.ProgressBar.CornerRadius);

            // behavior 域
            target.Behavior ??= new ProgressBarBehaviorConfig();
            target.Behavior.IdleOpacity = Math.Clamp(target.Behavior.IdleOpacity, 0.0, 1.0);

            // calibration 域
            target.Calibration ??= new CalibrationConfig();
            target.Calibration.IntervalSeconds = Math.Clamp(target.Calibration.IntervalSeconds, 1, 86400);
            target.Calibration.TriggerSeconds = Math.Max(1, target.Calibration.TriggerSeconds);
            target.Calibration.MinorThresholdSeconds = Math.Max(1, target.Calibration.MinorThresholdSeconds);
            target.Calibration.ResumeThresholdSeconds = Math.Max(60, target.Calibration.ResumeThresholdSeconds);
            target.Calibration.MaxRetryCount = Math.Max(0, target.Calibration.MaxRetryCount);
            target.Calibration.BackoffMultiplier = Math.Max(1.0, target.Calibration.BackoffMultiplier);

            // 确保 TriggerSeconds ≤ MinorThresholdSeconds
            if (target.Calibration.TriggerSeconds > target.Calibration.MinorThresholdSeconds)
            {
                target.Calibration.MinorThresholdSeconds = target.Calibration.TriggerSeconds;
            }

            // Cloud 子对象
            target.Calibration.Cloud ??= new CloudCalibrationConfig();
            target.Calibration.Cloud.TimeoutSeconds = Math.Max(1, target.Calibration.Cloud.TimeoutSeconds);
            if (string.IsNullOrWhiteSpace(target.Calibration.Cloud.SelectedServerAddress))
                target.Calibration.Cloud.SelectedServerAddress = new CloudCalibrationConfig().SelectedServerAddress;

            // stateStyles 域
            target.StateStyles ??= new StateStylesConfig();
            var allStates = new[] { "Loading", "Progress", "Success", "Error", "Paused", "Hidden", "Disabled" };
            foreach (var state in allStates)
            {
                if (!target.StateStyles.Styles.ContainsKey(state))
                    target.StateStyles.Styles[state] = new StateStyleEntry();
                var entry = target.StateStyles.Styles[state];
                if (entry.Opacity.HasValue)
                    entry.Opacity = Math.Clamp(entry.Opacity.Value, 0.0, 1.0);
            }

            // defaultBehavior 域
            target.DefaultBehavior ??= new ScheduleBehaviorData();

            // textOverlay 域（第7域）
            target.TextOverlay ??= new TextOverlayConfig();
            target.TextOverlay.Layout ??= new TextOverlayLayoutConfig();
            target.TextOverlay.Layout.Left ??= new TextOverlayGroupConfig();
            target.TextOverlay.Layout.Center ??= new TextOverlayGroupConfig();
            target.TextOverlay.Layout.Right ??= new TextOverlayGroupConfig();
            target.TextOverlay.Style ??= new TextOverlayStyleConfig();
            target.TextOverlay.Style.FontSize = Math.Max(1, target.TextOverlay.Style.FontSize);
            target.TextOverlay.Style.Opacity = Math.Clamp(target.TextOverlay.Style.Opacity, 0.0, 1.0);
            target.TextOverlay.Style.ItemSpacing = Math.Max(0, target.TextOverlay.Style.ItemSpacing);

            // window 域（第8域）
            target.Window ??= new WindowConfig();

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
                var defaultSetting = new TimeTopSetting();
                SaveTimeTopSetting(defaultSetting);
                Logger.Info("ReTime_Testing.Services.ConfigurationManager",
                    "TimeTop设置文件已创建");
            }
        }

        #region 逐域容错反序列化

        /// <summary>
        /// 尝试反序列化指定域（失败返回 null，不影响其他域）
        /// </summary>
        private T? TryDeserializeDomain<T>(JsonNode? parent, string propertyName) where T : class
        {
            try
            {
                var node = parent?[propertyName];
                if (node == null) return null;
                return JsonSerializer.Deserialize<T>(node.ToJsonString(), _jsonOptions);
            }
            catch (JsonException ex)
            {
                Logger.Warn("ReTime_Testing.Services.ConfigurationManager",
                    $"域 '{propertyName}' 解析失败: {ex.Message}，使用该域默认值");
                return null;
            }
        }

        /// <summary>
        /// 尝试获取字符串属性
        /// </summary>
        private static string? TryGetString(JsonNode? node, string propertyName)
        {
            try
            {
                return node?[propertyName]?.GetValue<string>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 尝试获取布尔属性
        /// </summary>
        private static bool? TryGetBool(JsonNode? node, string propertyName)
        {
            try
            {
                return node?[propertyName]?.GetValue<bool>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 尝试获取整数属性
        /// </summary>
        private static int? TryGetInt(JsonNode? node, string propertyName)
        {
            try
            {
                return node?[propertyName]?.GetValue<int>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 尝试获取浮点数属性
        /// </summary>
        private static double? TryGetDouble(JsonNode? node, string propertyName)
        {
            try
            {
                return node?[propertyName]?.GetValue<double>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 反序列化 calibration 域（支持子域容错）
        /// </summary>
        private CalibrationConfig? DeserializeCalibrationDomain(JsonNode root)
        {
            var calNode = root["calibration"];
            if (calNode == null) return null;

            // 先尝试整域反序列化
            var whole = TryDeserializeDomain<CalibrationConfig>(root, "calibration");
            if (whole != null) return whole;

            // 整域失败：逐子域 + 逐属性
            Logger.Warn("ReTime_Testing.Services.ConfigurationManager",
                "calibration 整域解析失败，尝试逐子域解析");
            var result = new CalibrationConfig();
            result.Enabled = TryGetBool(calNode, "enabled") ?? result.Enabled;
            // source 字段同时支持整数（0=System, 1=Cloud）和字符串（"System"/"Cloud"）
            var sourceStr = TryGetString(calNode, "source");
            var sourceInt = TryGetInt(calNode, "source");
            result.Source = (sourceStr, sourceInt) switch
            {
                ("cloud", _) => CalibrationSource.Cloud,
                ("system", _) => CalibrationSource.System,
                (_, 1) => CalibrationSource.Cloud,
                (_, 0) => CalibrationSource.System,
                _ => CalibrationSource.System
            };
            result.IntervalSeconds = TryGetInt(calNode, "intervalSeconds") ?? result.IntervalSeconds;
            result.TriggerSeconds = TryGetInt(calNode, "triggerSeconds") ?? result.TriggerSeconds;
            result.MinorThresholdSeconds = TryGetInt(calNode, "minorThresholdSeconds") ?? result.MinorThresholdSeconds;
            result.ResumeThresholdSeconds = TryGetInt(calNode, "resumeThresholdSeconds") ?? result.ResumeThresholdSeconds;
            result.MaxRetryCount = TryGetInt(calNode, "maxRetryCount") ?? result.MaxRetryCount;
            result.BackoffMultiplier = TryGetDouble(calNode, "backoffMultiplier") ?? result.BackoffMultiplier;

            // 解析 cloud 子域
            var cloudNode = calNode["cloud"];
            if (cloudNode != null)
            {
                result.Cloud.SelectedServerAddress = TryGetString(cloudNode, "selectedServerAddress") ?? result.Cloud.SelectedServerAddress;
                result.Cloud.TimeoutSeconds = TryGetInt(cloudNode, "timeoutSeconds") ?? result.Cloud.TimeoutSeconds;
            }

            return result;
        }

        /// <summary>
        /// 反序列化 textOverlay 域（支持子域容错）
        /// </summary>
        private TextOverlayConfig? DeserializeTextOverlayDomain(JsonNode root)
        {
            var toNode = root["textOverlay"];
            if (toNode == null) return null;

            // 先尝试整域反序列化
            var whole = TryDeserializeDomain<TextOverlayConfig>(root, "textOverlay");
            if (whole != null) return whole;

            // 整域失败：逐子域 + 逐属性
            Logger.Warn("ReTime_Testing.Services.ConfigurationManager",
                "textOverlay 整域解析失败，尝试逐子域解析");
            var result = new TextOverlayConfig();
            result.Enabled = TryGetBool(toNode, "enabled") ?? result.Enabled;
            result.Layout = TryDeserializeDomain<TextOverlayLayoutConfig>(toNode, "layout") ?? result.Layout;
            result.Style = TryDeserializeDomain<TextOverlayStyleConfig>(toNode, "style") ?? result.Style;
            return result;
        }

        #endregion
    }
}