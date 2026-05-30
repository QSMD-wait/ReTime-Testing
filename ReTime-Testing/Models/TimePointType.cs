using System.Text.Json.Serialization;

namespace ReTime_Testing.Models;

/// <summary>
/// 时间点类型
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimePointType
{
    /// <summary>
    /// 更改状态，可能包含样式信息（对应原来的 ToState/FromState/StyleOverrides）
    /// </summary>
    StateChange,

    /// <summary>
    /// 仅更改样式（不影响状态），适用于只改变样式的时间点
    /// </summary>
    StyleChange
}