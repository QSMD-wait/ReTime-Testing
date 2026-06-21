using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 时间段行为配置数据（JSON 序列化层）
    /// 用于控制时间段的调度和显示行为
    /// 全部字段可选，null 表示使用上级配置或默认值
    /// </summary>
    public class ScheduleBehaviorData
    {
        /// <summary>
        /// 轮询间隔（毫秒），null 表示使用上级配置或默认值
        /// 合法范围：10–10000
        /// </summary>
        [JsonPropertyName("pollingIntervalMs")]
        public int? PollingIntervalMs { get; set; }

        /// <summary>
        /// 是否启用倒计时模式（剩余时间递减到0），null 表示使用上级配置或默认值
        /// </summary>
        [JsonPropertyName("reverseProgress")]
        public bool? ReverseProgress { get; set; }

        /// <summary>
        /// 是否有任何行为覆盖设置
        /// </summary>
        [JsonIgnore]
        public bool HasAnyOverride =>
            PollingIntervalMs.HasValue ||
            ReverseProgress.HasValue;

        /// <summary>
        /// 与低优先级行为配置合并，当前对象的非 null 值覆盖低优先级
        /// 纯函数，无副作用，易于测试
        /// </summary>
        /// <param name="lower">低优先级行为配置（可为 null）</param>
        /// <returns>合并后的新实例</returns>
        public ScheduleBehaviorData MergeWith(ScheduleBehaviorData? lower)
        {
            return new ScheduleBehaviorData
            {
                PollingIntervalMs = this.PollingIntervalMs ?? lower?.PollingIntervalMs,
                ReverseProgress = this.ReverseProgress ?? lower?.ReverseProgress
            };
        }

        /// <summary>
        /// 将可空配置解析为最终行为配置
        /// 剩余 null 值回退到硬编码默认值
        /// </summary>
        /// <returns>解析后的行为配置</returns>
        public ScheduleBehavior ToResolved()
        {
            return new ScheduleBehavior
            {
                PollingIntervalMs = PollingIntervalMs ?? ScheduleBehavior.DefaultPollingIntervalMs,
                ReverseProgress = ReverseProgress ?? ScheduleBehavior.DefaultReverseProgress
            };
        }
    }

    /// <summary>
    /// 时间段行为配置（运行时解析层）
    /// 全部字段非 null，表示已解析的最终值
    /// </summary>
    public class ScheduleBehavior
    {
        /// <summary>
        /// 默认轮询间隔（毫秒）
        /// </summary>
        public const int DefaultPollingIntervalMs = 1000;

        /// <summary>
        /// 默认倒计时模式
        /// </summary>
        public const bool DefaultReverseProgress = false;

        /// <summary>
        /// 轮询间隔（毫秒）
        /// </summary>
        public int PollingIntervalMs { get; set; } = DefaultPollingIntervalMs;

        /// <summary>
        /// 是否启用倒计时模式
        /// </summary>
        public bool ReverseProgress { get; set; } = DefaultReverseProgress;

        /// <summary>
        /// 默认行为配置
        /// </summary>
        public static ScheduleBehavior Default => new();

        /// <summary>
        /// 获取行为配置的字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"PollingInterval={PollingIntervalMs}ms, ReverseProgress={ReverseProgress}";
        }
    }
}
