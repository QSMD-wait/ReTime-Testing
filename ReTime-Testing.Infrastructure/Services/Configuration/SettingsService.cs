using ReTime_Testing.Models;
using System.Text.Json.Nodes;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// 设置配置服务
    /// 职责：配置的缓存、校验、默认值填充、变更通知、热重载分发
    /// 通过 JsonConfigProvider 进行文件 I/O
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private const string LOG_MODULE = "SettingsService";

        private readonly JsonConfigProvider _provider;
        private readonly IConfigurationManager _configManager;

        private GlobalSetting? _cachedGlobalSetting;
        private TimeTopSetting? _cachedTimeTopSetting;

        /// <summary>
        /// 全局配置变更事件
        /// </summary>
        public event Action<GlobalSetting>? OnGlobalSettingChanged;

        /// <summary>
        /// TimeTop配置变更事件
        /// </summary>
        public event Action<TimeTopSetting>? OnTimeTopSettingChanged;

        /// <summary>
        /// 构造函数（支持 DI 注入）
        /// </summary>
        /// <param name="configManager">配置管理器</param>
        public SettingsService(IConfigurationManager configManager)
        {
            _provider = new JsonConfigProvider();
            _configManager = configManager;
        }

        #region GlobalSetting

        /// <summary>
        /// 获取全局配置（优先缓存）
        /// </summary>
        public GlobalSetting GetGlobalSetting()
        {
            if (_cachedGlobalSetting != null)
                return _cachedGlobalSetting;

            var setting = LoadGlobalSettingFromFile();
            _cachedGlobalSetting = setting;
            return setting;
        }

        /// <summary>
        /// 保存全局配置（写入文件 + 更新缓存 + 通知 + 热重载）
        /// </summary>
        public void SaveGlobalSetting(GlobalSetting setting)
        {
            try
            {
                _provider.Write(_configManager.GlobalSettingFilePath, setting);
                _cachedGlobalSetting = setting;

                Logger.Info(LOG_MODULE, "全局配置保存成功");

                OnGlobalSettingChanged?.Invoke(setting);
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE, $"全局配置保存失败: {ex.Message}", ex);
                throw new ConfigurationException("全局配置保存失败", ex);
            }
        }

        /// <summary>
        /// 重置全局配置为默认值
        /// </summary>
        public void ResetGlobalSetting()
        {
            var defaultSetting = new GlobalSetting();
            SaveGlobalSetting(defaultSetting);

            Logger.Info(LOG_MODULE, "全局配置已重置为默认值");
        }

        /// <summary>
        /// 刷新全局配置缓存
        /// </summary>
        public void RefreshGlobalSettingCache()
        {
            _cachedGlobalSetting = null;
        }

        /// <summary>
        /// 从文件加载全局配置（含校验和默认值填充）
        /// </summary>
        private GlobalSetting LoadGlobalSettingFromFile()
        {
            try
            {
                var filePath = _configManager.GlobalSettingFilePath;

                if (!_provider.FileExists(filePath))
                {
                    Logger.Warn(LOG_MODULE, "全局配置文件不存在，创建默认配置");
                    var newSetting = new GlobalSetting();
                    SaveGlobalSetting(newSetting);
                    return newSetting;
                }

                var jsonContent = _provider.ReadRawText(filePath);

                if (jsonContent == null)
                {
                    Logger.Info(LOG_MODULE, "全局配置文件为空，写入默认配置");
                    var newSetting = new GlobalSetting();
                    SaveGlobalSetting(newSetting);
                    return newSetting;
                }

                JsonNode? rootNode;
                try
                {
                    rootNode = _provider.ParseJson(jsonContent);
                }
                catch (Exception ex)
                {
                    Logger.Error(LOG_MODULE, $"全局配置文件 JSON 语法错误: {ex.Message}，使用默认配置（不覆盖原文件）", ex);
                    return new GlobalSetting();
                }

                if (rootNode == null)
                    return new GlobalSetting();

                var result = new GlobalSetting();
                result.Version = JsonConfigProvider.TryGetString(rootNode, "version") ?? result.Version;
                result.Basic = _provider.TryDeserializeDomain<BasicSetting>(rootNode, "basic") ?? result.Basic;

                result.Basic.Log ??= new LogConfig();
                result.Basic.Log.RetainedDays = Math.Max(1, result.Basic.Log.RetainedDays);
                result.Basic.Log.FileSizeLimitMB = Math.Max(1, result.Basic.Log.FileSizeLimitMB);

                Logger.Info(LOG_MODULE, "全局配置加载成功");

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE, $"全局配置加载失败: {ex.Message}，使用默认配置（不覆盖原文件）");
                return new GlobalSetting();
            }
        }

        #endregion

        #region TimeTopSetting

        /// <summary>
        /// 获取TimeTop配置（优先缓存）
        /// </summary>
        public TimeTopSetting GetTimeTopSetting()
        {
            if (_cachedTimeTopSetting != null)
                return _cachedTimeTopSetting;

            var setting = LoadTimeTopSettingFromFile();
            _cachedTimeTopSetting = setting;
            return setting;
        }

        /// <summary>
        /// 保存TimeTop配置（写入文件 + 更新缓存 + 通知 + 热重载）
        /// </summary>
        public void SaveTimeTopSetting(TimeTopSetting setting)
        {
            try
            {
                _provider.Write(_configManager.TimeTopSettingFilePath, setting);
                _cachedTimeTopSetting = setting;

                Logger.Info(LOG_MODULE, "TimeTop设置保存成功");

                OnTimeTopSettingChanged?.Invoke(setting);
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE, $"保存TimeTop设置失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 刷新TimeTop配置缓存
        /// </summary>
        public void RefreshTimeTopSettingCache()
        {
            _cachedTimeTopSetting = null;
        }

        /// <summary>
        /// 从文件加载TimeTop配置（含校验和默认值填充）
        /// </summary>
        private TimeTopSetting LoadTimeTopSettingFromFile()
        {
            try
            {
                var filePath = _configManager.TimeTopSettingFilePath;

                if (!_provider.FileExists(filePath))
                {
                    Logger.Info(LOG_MODULE, "TimeTop设置文件不存在，创建默认配置");
                    var newSetting = new TimeTopSetting();
                    SaveTimeTopSetting(newSetting);
                    return newSetting;
                }

                var jsonContent = _provider.ReadRawText(filePath);

                if (jsonContent == null)
                {
                    Logger.Info(LOG_MODULE, "TimeTop设置文件为空，创建默认配置");
                    var newSetting = new TimeTopSetting();
                    SaveTimeTopSetting(newSetting);
                    return newSetting;
                }

                JsonNode? rootNode;
                try
                {
                    rootNode = _provider.ParseJson(jsonContent);
                }
                catch (Exception ex)
                {
                    Logger.Error(LOG_MODULE, $"TimeTop设置 JSON 语法错误: {ex.Message}，使用默认配置（不覆盖原文件）", ex);
                    return new TimeTopSetting();
                }

                if (rootNode == null)
                    return new TimeTopSetting();

                var result = new TimeTopSetting();

                result.Version = JsonConfigProvider.TryGetString(rootNode, "version") ?? result.Version;
                result.Schedule = _provider.TryDeserializeDomain<ScheduleConfig>(rootNode, "schedule") ?? result.Schedule;
                result.ProgressBar = _provider.TryDeserializeDomain<ProgressBarConfig>(rootNode, "progressBar") ?? result.ProgressBar;
                result.Behavior = _provider.TryDeserializeDomain<ProgressBarBehaviorConfig>(rootNode, "behavior") ?? result.Behavior;
                result.StateStyles = _provider.TryDeserializeDomain<StateStylesConfig>(rootNode, "stateStyles") ?? result.StateStyles;
                result.DefaultBehavior = _provider.TryDeserializeDomain<ScheduleBehaviorData>(rootNode, "defaultBehavior") ?? result.DefaultBehavior;
                result.Calibration = _provider.DeserializeCalibrationDomain(rootNode) ?? result.Calibration;
                result.TextOverlay = _provider.DeserializeTextOverlayDomain(rootNode) ?? result.TextOverlay;
                result.Window = _provider.TryDeserializeDomain<WindowConfig>(rootNode, "window") ?? result.Window;

                if (string.IsNullOrEmpty(result.Version) || result.Version != "1.0.0")
                {
                    Logger.Warn(LOG_MODULE, $"TimeTop设置版本不匹配: {result.Version}，填充默认值");
                }

                var defaults = new TimeTopSetting();
                result = FillMissingFields(result, defaults);

                Logger.Info(LOG_MODULE, "TimeTop设置加载成功");

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(LOG_MODULE, $"加载TimeTop设置失败: {ex.Message}，使用默认配置（不覆盖原文件）", ex);
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

            target.Schedule ??= new ScheduleConfig();
            target.Schedule.Override ??= new ScheduleOverrideConfig();
            if (string.IsNullOrEmpty(target.Schedule.Override.ScheduleId))
                target.Schedule.Override.ScheduleId = defaults.Schedule.Override.ScheduleId;

            target.ProgressBar ??= new ProgressBarConfig();
            target.ProgressBar.Scale = Math.Clamp(target.ProgressBar.Scale, 0.5, 3.0);
            target.ProgressBar.CornerRadius = Math.Max(0, target.ProgressBar.CornerRadius);

            target.Behavior ??= new ProgressBarBehaviorConfig();
            target.Behavior.IdleOpacity = Math.Clamp(target.Behavior.IdleOpacity, 0.0, 1.0);

            target.Calibration ??= new CalibrationConfig();
            target.Calibration.IntervalSeconds = Math.Clamp(target.Calibration.IntervalSeconds, 1, 86400);
            target.Calibration.TriggerSeconds = Math.Max(1, target.Calibration.TriggerSeconds);
            target.Calibration.MinorThresholdSeconds = Math.Max(1, target.Calibration.MinorThresholdSeconds);
            target.Calibration.ResumeThresholdSeconds = Math.Max(60, target.Calibration.ResumeThresholdSeconds);
            target.Calibration.MaxRetryCount = Math.Max(0, target.Calibration.MaxRetryCount);
            target.Calibration.BackoffMultiplier = Math.Max(1.0, target.Calibration.BackoffMultiplier);

            if (target.Calibration.TriggerSeconds > target.Calibration.MinorThresholdSeconds)
            {
                target.Calibration.MinorThresholdSeconds = target.Calibration.TriggerSeconds;
            }

            target.Calibration.Cloud ??= new CloudCalibrationConfig();
            target.Calibration.Cloud.TimeoutSeconds = Math.Max(1, target.Calibration.Cloud.TimeoutSeconds);
            if (string.IsNullOrWhiteSpace(target.Calibration.Cloud.SelectedServerAddress))
                target.Calibration.Cloud.SelectedServerAddress = new CloudCalibrationConfig().SelectedServerAddress;

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

            target.DefaultBehavior ??= new ScheduleBehaviorData();

            target.TextOverlay ??= new TextOverlayConfig();
            target.TextOverlay.Layout ??= new TextOverlayLayoutConfig();
            target.TextOverlay.Layout.Left ??= new TextOverlayGroupConfig();
            target.TextOverlay.Layout.Center ??= new TextOverlayGroupConfig();
            target.TextOverlay.Layout.Right ??= new TextOverlayGroupConfig();
            target.TextOverlay.Style ??= new TextOverlayStyleConfig();
            target.TextOverlay.Style.FontSize = Math.Max(1, target.TextOverlay.Style.FontSize);
            target.TextOverlay.Style.Opacity = Math.Clamp(target.TextOverlay.Style.Opacity, 0.0, 1.0);
            target.TextOverlay.Style.ItemSpacing = Math.Max(0, target.TextOverlay.Style.ItemSpacing);

            EnsureSlotDefaults(target.TextOverlay.Layout.Left.Slots);
            EnsureSlotDefaults(target.TextOverlay.Layout.Center.Slots);
            EnsureSlotDefaults(target.TextOverlay.Layout.Right.Slots);

            target.Window ??= new WindowConfig();

            return target;
        }

        private static void EnsureSlotDefaults(List<TextSlotConfig> slots)
        {
            foreach (var slot in slots)
            {
                slot.SourceSettings ??= new TextSlotSourceSettings();
                slot.CommonSettings ??= new TextSlotCommonSettings();
                if (slot.CommonSettings.FontSizeOverride.HasValue)
                    slot.CommonSettings.FontSizeOverride = Math.Max(1, slot.CommonSettings.FontSizeOverride.Value);
            }
        }

        #endregion
    }
}