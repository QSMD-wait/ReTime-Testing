namespace ReTime_Testing.Models;

/// <summary>
/// 时间跳跃严重程度
/// </summary>
public enum TimeJumpSeverity
{
    /// <summary>
    /// 微调（偏差较小，不需要重新计算调度状态）
    /// </summary>
    Minor,

    /// <summary>
    /// 重大跳跃（偏差较大，需要重新计算调度状态）
    /// </summary>
    Major
}