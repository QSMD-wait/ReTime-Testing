using ReTime_Testing.Models;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ReTime_Testing.Services
{
    /// <summary>
    /// JSON 配置文件读写提供者
    /// 职责：纯 JSON 文件的读取、写入、目录创建
    /// 不包含任何业务逻辑（默认值、校验、缓存、通知）
    /// </summary>
    public class JsonConfigProvider
    {
        private const string LOG_MODULE = "JsonConfigProvider";

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        public bool FileExists(string filePath) => File.Exists(filePath);

        /// <summary>
        /// 确保目录存在
        /// </summary>
        public void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Logger.Info(LOG_MODULE, $"目录已创建: {path}");
            }
        }

        /// <summary>
        /// 读取 JSON 文件并反序列化为指定类型
        /// 文件不存在或为空时返回 null
        /// </summary>
        public T? Read<T>(string filePath) where T : class
        {
            if (!File.Exists(filePath))
            {
                Logger.Warn(LOG_MODULE, $"文件不存在: {Path.GetFileName(filePath)}");
                return null;
            }

            string jsonContent = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Trim() == "{}")
            {
                Logger.Warn(LOG_MODULE, $"文件内容为空: {Path.GetFileName(filePath)}");
                return null;
            }

            var result = JsonSerializer.Deserialize<T>(jsonContent, _jsonOptions);
            Logger.Info(LOG_MODULE, $"文件读取成功: {Path.GetFileName(filePath)}");
            return result;
        }

        /// <summary>
        /// 读取 JSON 文件原始文本
        /// </summary>
        public string? ReadRawText(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Logger.Warn(LOG_MODULE, $"文件不存在: {Path.GetFileName(filePath)}");
                return null;
            }

            string jsonContent = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Trim() == "{}")
            {
                Logger.Warn(LOG_MODULE, $"文件内容为空: {Path.GetFileName(filePath)}");
                return null;
            }

            Logger.Info(LOG_MODULE, $"文件读取成功: {Path.GetFileName(filePath)}");
            return jsonContent;
        }

        /// <summary>
        /// 将对象序列化并写入 JSON 文件
        /// </summary>
        public void Write<T>(string filePath, T value)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                EnsureDirectoryExists(dir);

            string jsonContent = JsonSerializer.Serialize(value, _jsonOptions);
            File.WriteAllText(filePath, jsonContent);
            Logger.Info(LOG_MODULE, $"文件写入成功: {Path.GetFileName(filePath)}");
        }

        /// <summary>
        /// 解析 JSON 文本为 JsonNode DOM
        /// </summary>
        public JsonNode? ParseJson(string jsonContent)
        {
            try
            {
                return JsonNode.Parse(jsonContent, null, new JsonDocumentOptions { AllowTrailingCommas = true });
            }
            catch (JsonException)
            {
                return null;
            }
        }

        #region 逐域容错反序列化

        /// <summary>
        /// 尝试反序列化指定域（失败返回 null，不影响其他域）
        /// </summary>
        public T? TryDeserializeDomain<T>(JsonNode? parent, string propertyName) where T : class
        {
            try
            {
                var node = parent?[propertyName];
                if (node == null) return null;

                var json = node.ToJsonString();
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                Logger.Warn(LOG_MODULE, $"域 '{propertyName}' 解析失败，使用该域默认值");
                return null;
            }
        }

        /// <summary>
        /// 尝试获取字符串属性
        /// </summary>
        public static string? TryGetString(JsonNode? node, string propertyName)
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
        public static bool? TryGetBool(JsonNode? node, string propertyName)
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
        public static int? TryGetInt(JsonNode? node, string propertyName)
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
        public static double? TryGetDouble(JsonNode? node, string propertyName)
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
        public CalibrationConfig? DeserializeCalibrationDomain(JsonNode root)
        {
            var calNode = root["calibration"];
            if (calNode == null) return null;

            var whole = TryDeserializeDomain<CalibrationConfig>(root, "calibration");
            if (whole != null) return whole;

            Logger.Warn(LOG_MODULE, "calibration 整域解析失败，尝试逐子域解析");
            var result = new CalibrationConfig();
            result.Enabled = TryGetBool(calNode, "enabled") ?? result.Enabled;
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
        public TextOverlayConfig? DeserializeTextOverlayDomain(JsonNode root)
        {
            var toNode = root["textOverlay"];
            if (toNode == null) return null;

            var whole = TryDeserializeDomain<TextOverlayConfig>(root, "textOverlay");
            if (whole != null) return whole;

            Logger.Warn(LOG_MODULE, "textOverlay 整域解析失败，尝试逐子域解析");
            var result = new TextOverlayConfig();
            result.Enabled = TryGetBool(toNode, "enabled") ?? result.Enabled;
            result.Layout = TryDeserializeDomain<TextOverlayLayoutConfig>(toNode, "layout") ?? result.Layout;
            result.Style = TryDeserializeDomain<TextOverlayStyleConfig>(toNode, "style") ?? result.Style;
            return result;
        }

        #endregion
    }
}