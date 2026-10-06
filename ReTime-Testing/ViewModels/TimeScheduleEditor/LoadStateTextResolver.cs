using ReTime_Testing.Models;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 顶部命令栏「加载表组 / 加载计划表」按钮动态文字解析（纯函数，便于单测）。
/// 口径与 ScheduleGroupManager.GetEffectiveScheduleId 一致：
/// 生效方显示当前名称（表组当天无表时显示"无计划"提示），非生效方回退默认动作文字；
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
    /// <param name="config">计划表配置（覆盖状态与激活组来源）</param>
    /// <param name="activeGroupName">激活组名；组不存在或未激活传 null</param>
    /// <param name="groupRotationInfo">激活组轮换信息（如 "第1/2周"）；非轮换组或无组传空</param>
    /// <param name="overrideScheduleName">覆盖启用的计划表名；表不存在传 null</param>
    /// <param name="effectiveScheduleName">今天实际生效的计划表名（未覆盖时由表组轮换决定）；无生效表传 null</param>
    public static (string GroupLabel, string GroupDetail, string ScheduleLabel, string ScheduleDetail, bool IsScheduleSecondary) Resolve(
        ScheduleConfig config,
        string? activeGroupName,
        string? groupRotationInfo,
        string? overrideScheduleName,
        string? effectiveScheduleName)
    {
        // 覆盖（含仅当天临时启用）生效时：计划表按钮显示表名（主）；
        // 表组生效时计划表按钮显示"当前应用的表"（次级信息用次要样式差分），当天无表则显示"无计划"提示；
        // 表组按钮：组生效时显示组名（主），否则回退动作文字
        var overrideActive = config.Override.IsEffectiveToday;
        // 表组生效 = 未被覆盖接管，且激活组仍在（组数据被删视为未启用）
        var groupEffective = !overrideActive
            && !string.IsNullOrEmpty(config.ActiveGroupId)
            && activeGroupName != null;

        string scheduleLabel;
        string scheduleDetail;
        var scheduleIsSecondary = false;

        if (overrideActive && !string.IsNullOrEmpty(config.Override.ScheduleId) && overrideScheduleName != null)
        {
            // 覆盖启用 = 主：显示覆盖的表名
            scheduleLabel = overrideScheduleName;
            scheduleDetail = string.IsNullOrEmpty(config.Override.TemporaryDate)
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
            // 未启用任何来源（含临时启用过期且无表组）：回退动作文字
            scheduleLabel = ScheduleActionText;
            scheduleDetail = "";
        }

        string groupLabel;
        string groupDetail;
        if (groupEffective)
        {
            // groupEffective 已含 activeGroupName 非空判定
            groupLabel = activeGroupName!;
            groupDetail = string.IsNullOrEmpty(groupRotationInfo)
                ? "按表组安排生效"
                : $"轮换中 · {groupRotationInfo}";
        }
        else
        {
            groupLabel = GroupActionText;
            groupDetail = "";
        }

        return (groupLabel, groupDetail, scheduleLabel, scheduleDetail, scheduleIsSecondary);
    }
}
