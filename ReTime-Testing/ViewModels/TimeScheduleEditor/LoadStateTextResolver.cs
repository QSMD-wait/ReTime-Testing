using ReTime_Testing.Models;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 顶部命令栏「加载表组 / 加载计划表」按钮动态文字解析（纯函数，便于单测）。
/// 口径与 ScheduleGroupManager.GetEffectiveScheduleId 一致（生效判定统一走 ScheduleConfig.EffectiveManual）：
/// 生效方显示当前名称（表组当天无表时显示"无计划"提示），非生效方回退默认动作文字；
/// 仅当天临时启用接管时表组配置仍保留，组按钮以次级样式显示组名提示明日恢复；
/// 具体名称解析（含"表已删除"回退）由调用方完成。
/// </summary>
public static class LoadStateTextResolver
{
    /// <summary>表组按钮的默认动作文字</summary>
    public const string GroupActionText = "加载表组";

    /// <summary>计划表按钮的默认动作文字</summary>
    public const string ScheduleActionText = "加载计划表";

    /// <summary>表组当天没有安排计划表时的提示文字（非动作，按次级样式显示）</summary>
    public const string NoScheduleText = "无计划";

    /// <summary>
    /// 解析两个加载按钮的动态文字与 ToolTip 细节行
    /// </summary>
    /// <param name="config">计划表配置（手动指定状态与激活组来源）</param>
    /// <param name="activeGroupName">激活组名；组不存在或未激活传 null</param>
    /// <param name="groupRotationInfo">激活组轮换信息（如 "第1/2周"）；非轮换组或无组传空</param>
    /// <param name="manualScheduleName">手动指定的计划表名；表不存在传 null</param>
    /// <param name="effectiveScheduleName">今天实际生效的计划表名（未手动指定时由表组轮换决定）；无生效表传 null</param>
    public static (string GroupLabel, string GroupDetail, bool IsGroupSecondary,
                   string ScheduleLabel, string ScheduleDetail, bool IsScheduleSecondary) Resolve(
        ScheduleConfig config,
        string? activeGroupName,
        string? groupRotationInfo,
        string? manualScheduleName,
        string? effectiveScheduleName)
    {
        // 今天生效的手动指定表（null = 无 / 仅当天已过期）；仅当天 = 组配置保留、次日恢复轮换
        var manual = config.EffectiveManual;
        var manualTemporary = manual != null && manual.Mode == ScheduleManualMode.Today;
        // 组配置仍在（组数据被删视为不在）
        var groupConfigured = !string.IsNullOrEmpty(config.ActiveGroupId) && activeGroupName != null;
        // 表组今日实际生效 = 未被手动指定接管
        var groupEffective = manual == null && groupConfigured;

        string scheduleLabel;
        string scheduleDetail;
        var scheduleIsSecondary = false;

        if (manual != null && manualScheduleName != null)
        {
            // 手动指定生效 = 主：显示手动表名
            scheduleLabel = manualScheduleName;
            scheduleDetail = manual.Mode == ScheduleManualMode.Permanent
                ? "覆盖式启用中，点击可切换计划表"
                : "仅当天临时启用，次日自动恢复按表组轮换";
        }
        else if (groupEffective)
        {
            // 表组生效：次级信息显示今天轮换得到的当前表；
            // 当天没有安排计划表时显示"无计划"提示文案，而非默认动作文字
            scheduleIsSecondary = true;
            if (effectiveScheduleName != null)
            {
                scheduleLabel = effectiveScheduleName;
                scheduleDetail = "由表组轮换生效";
            }
            else
            {
                scheduleLabel = NoScheduleText;
                scheduleDetail = "表组今天未安排计划表";
            }
        }
        else
        {
            // 未启用任何来源（含仅当天过期且无表组、手动表已删除）：回退动作文字
            scheduleLabel = ScheduleActionText;
            scheduleDetail = "";
        }

        string groupLabel;
        string groupDetail;
        var groupIsSecondary = false;
        if (groupEffective)
        {
            // groupConfigured 已含 activeGroupName 非空判定
            groupLabel = activeGroupName!;
            groupDetail = string.IsNullOrEmpty(groupRotationInfo)
                ? "按表组安排生效"
                : $"轮换中 · {groupRotationInfo}";
        }
        else if (manualTemporary && groupConfigured)
        {
            // 仅当天临时接管：组配置保留（ActiveGroupId 未清），组名以次级样式提示明日恢复；
            // 覆盖式接管时组已清除，走下方默认动作文字
            groupIsSecondary = true;
            groupLabel = activeGroupName!;
            groupDetail = "次日恢复按表组轮换";
        }
        else
        {
            groupLabel = GroupActionText;
            groupDetail = "";
        }

        return (groupLabel, groupDetail, groupIsSecondary, scheduleLabel, scheduleDetail, scheduleIsSecondary);
    }
}
