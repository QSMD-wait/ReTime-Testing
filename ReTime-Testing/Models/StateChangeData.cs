using System.Text.Json.Serialization;

namespace ReTime_Testing.Models;

/// <summary>
/// 状态变更数据
/// </summary>
public class StateChangeData
{
    /// <summary>
    /// 目标状态（必填）
    /// </summary>
    public ProgressStateType ToState { get; set; }

    /// <summary>
    /// 源状态（可选，由系统自动计算）
    /// </summary>
    public ProgressStateType? FromState { get; set; }
}