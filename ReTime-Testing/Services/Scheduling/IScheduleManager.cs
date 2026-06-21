using ReTime_Testing.Models;

namespace ReTime_Testing.Services;

/// <summary>
/// 调度管理器接口
/// 负责执行计划的调度和状态管理
/// </summary>
public interface IScheduleManager
{
    /// <summary>
    /// 当前执行计划
    /// </summary>
    ExecutionPlan? CurrentPlan { get; }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="plan">执行计划</param>
    void Initialize(ExecutionPlan plan);

    /// <summary>
    /// 重新生成执行计划
    /// </summary>
    /// <param name="newPlan">新的执行计划</param>
    void RegenerateExecutionPlan(ExecutionPlan newPlan);

    /// <summary>
    /// 停止调度
    /// </summary>
    void Stop();

    /// <summary>
    /// 应用当前状态
    /// </summary>
    void ApplyCurrentState();
}
