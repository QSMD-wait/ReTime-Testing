using System.Text.Json.Serialization;

namespace ReTime_Testing.Models;

/// <summary>
/// 时间点类型（原子值，可组合为数组使用）
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimePointType
{
    /// <summary>
    /// 状态变更
    /// </summary>
    StateChange = 0,

    /// <summary>
    /// 样式变更
    /// </summary>
    StyleChange = 1
}