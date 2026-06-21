using System.Text.Json.Serialization;

namespace ReTime_Testing.Models
{
    /// <summary>
    /// 自定义时间点
    /// 用于在配置中定义自定义的状态切换时间点
    /// 可以覆盖自动生成的开始/结束时间点，或添加额外的时间点
    /// </summary>
    /// <remarks>
    /// fromState 由系统自动计算，无需用户配置：
    /// - 第一个时间点: fromState = Loading
    /// - 后续时间点: fromState = 上一个时间点的 toState
    /// </remarks>
    public class CustomTimePoint
    {
        /// <summary>
        /// 时间点唯一标识（必填）
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 时间点名称（可选）
        /// 用于日志和配置的可读性
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 时间点时刻（HH:mm:ss 格式，必填）
        /// </summary>
        [JsonPropertyName("time")]
        public string Time { get; set; } = "00:00:00";

        /// <summary>
    /// 时间点类型（必填）："StateChange" 或 "StyleChange"
    /// </summary>
    [JsonPropertyName("type")]
    public TimePointType Type { get; set; } = TimePointType.StateChange;

    /// <summary>
    /// 状态变更数据（当 Type = StateChange 时生效）
    /// </summary>
    public StateChangeData? StateChange { get; set; }

    /// <summary>
    /// 样式变更数据（当 Type = StyleChange 时生效）
    /// </summary>
    public StyleChangeData? StyleChange { get; set; }
}
}