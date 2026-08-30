using ReTime_Testing.Models;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.Services;

/// <summary>
/// 调度启动结果状态
/// </summary>
public enum ScheduleStartupStatus
{
    /// <summary>调度器已启动</summary>
    Started,

    /// <summary>时间计划控制已禁用</summary>
    Disabled,

    /// <summary>今日无生效计划表，保持空闲</summary>
    IdleNoSchedule,

    /// <summary>生效计划表无效或不存在</summary>
    InvalidScheduleId,

    /// <summary>执行计划验证失败，保持空闲</summary>
    InvalidPlan
}

/// <summary>
/// 调度启动结果
/// </summary>
/// <param name="Status">结果状态</param>
/// <param name="Message">需向用户展示的错误信息（仅错误状态）</param>
public sealed record ScheduleStartupResult(ScheduleStartupStatus Status, string? Message = null);

/// <summary>
/// 调度编排服务接口
/// 统一封装"评估生效计划表 → 生成安全执行计划 → 启动/切换/停止调度"的编排逻辑
/// </summary>
public interface IScheduleOrchestrator
{
    /// <summary>
    /// 应用启动时的调度初始化
    /// </summary>
    /// <param name="scheduleEnabled">时间计划控制是否启用</param>
    ScheduleStartupResult InitializeOnStartup(bool scheduleEnabled);

    /// <summary>
    /// 配置热重载时的调度切换（重新评估生效计划表并更新执行计划）
    /// </summary>
    /// <param name="scheduleEnabled">时间计划控制是否启用</param>
    void ApplyScheduleConfig(bool scheduleEnabled);
}

/// <summary>
/// 调度编排服务
/// 供 App 启动流程与配置热重载共用，消除两处重复的调度编排代码
/// </summary>
public class ScheduleOrchestrator : IScheduleOrchestrator
{
        private readonly ILogger<ScheduleOrchestrator> _logger;
    private readonly ITimeScheduleManager _timeScheduleManager;
    private readonly IScheduleGroupManager _scheduleGroupManager;
    private readonly IScheduleManager _scheduleRunManager;
    private readonly ITimeService _timeService;
    private readonly ExecutionPlanGenerator _planGenerator;

    public ScheduleOrchestrator(
        ILogger<ScheduleOrchestrator> logger,
        ITimeScheduleManager timeScheduleManager,
        IScheduleGroupManager scheduleGroupManager,
        IScheduleManager scheduleRunManager,
        ITimeService timeService,
        ExecutionPlanGenerator planGenerator)
    {
        _logger = logger;
        _timeScheduleManager = timeScheduleManager;
        _scheduleGroupManager = scheduleGroupManager;
        _scheduleRunManager = scheduleRunManager;
        _timeService = timeService;
        _planGenerator = planGenerator;
    }

    /// <inheritdoc/>
    public ScheduleStartupResult InitializeOnStartup(bool scheduleEnabled)
    {
        // 初始化表组管理器（确保默认组存在，与 Enabled 无关）
        _scheduleGroupManager.Initialize();

        if (!scheduleEnabled)
        {
            _logger.LogInformation("时间计划控制已禁用，跳过调度初始化");
            return new ScheduleStartupResult(ScheduleStartupStatus.Disabled);
        }

        var effectiveScheduleId = _scheduleGroupManager.GetEffectiveScheduleId();
        if (effectiveScheduleId == null)
        {
            _logger.LogInformation("今日无生效计划表，保持空闲状态");
            return new ScheduleStartupResult(ScheduleStartupStatus.IdleNoSchedule);
        }

        var selectedSchedule = _timeScheduleManager.LoadSchedule(effectiveScheduleId);
        if (selectedSchedule == null)
        {
            _logger.LogError("生效计划表无效或不存在: {ScheduleId}，保持空闲状态", effectiveScheduleId);
            return new ScheduleStartupResult(
                ScheduleStartupStatus.InvalidScheduleId,
                $"计划表 \"{effectiveScheduleId}\" 无效或不存在。\n\n请检查计划表组配置或计划表文件是否完整。");
        }

        var currentTime = _timeService.GetCurrentTime();
        var executionPlan = _planGenerator.GenerateSafe(selectedSchedule, DateTime.Today, currentTime);
        if (executionPlan == null)
        {
            _logger.LogWarning("时间计划验证失败，保持空闲状态");
            return new ScheduleStartupResult(
                ScheduleStartupStatus.InvalidPlan,
                "时间计划配置无效，已保持空闲状态。\n\n请检查时间计划表配置是否正确。");
        }

        _logger.LogInformation("执行计划已生成: {Plan}", executionPlan);
        _scheduleRunManager.Initialize(executionPlan);
        _logger.LogInformation("调度管理器已启动");
        return new ScheduleStartupResult(ScheduleStartupStatus.Started);
    }

    /// <inheritdoc/>
    public void ApplyScheduleConfig(bool scheduleEnabled)
    {
        try
        {
            if (!scheduleEnabled)
            {
                if (_scheduleRunManager.CurrentPlan != null)
                {
                    _scheduleRunManager.Stop();
                    _logger.LogInformation("热重载：时间计划控制已禁用，调度器已停止");
                }
                return;
            }

            var effectiveScheduleId = _scheduleGroupManager.GetEffectiveScheduleId();
            var currentPlan = _scheduleRunManager.CurrentPlan;
            var currentScheduleId = currentPlan?.ScheduleId;

            if (effectiveScheduleId == currentScheduleId)
            {
                return;
            }

            if (effectiveScheduleId == null)
            {
                _scheduleRunManager.Stop();
                _logger.LogInformation("热重载：今日无生效计划表，调度器已停止");
                return;
            }

            var newSchedule = _timeScheduleManager.LoadSchedule(effectiveScheduleId);
            if (newSchedule == null)
            {
                return;
            }

            var now = _timeService.GetCurrentTime();
            var newPlan = _planGenerator.GenerateSafe(newSchedule, DateTime.Today, now);
            if (newPlan == null)
            {
                return;
            }

            if (currentPlan != null)
            {
                _scheduleRunManager.RegenerateExecutionPlan(newPlan);
            }
            else
            {
                _scheduleRunManager.Initialize(newPlan);
            }
            _logger.LogInformation("热重载：执行计划已切换至 {ScheduleId}", effectiveScheduleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "热重载调度器失败: {Message}", ex.Message);
        }
    }
}
