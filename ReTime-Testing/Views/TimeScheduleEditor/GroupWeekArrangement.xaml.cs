using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using ReTime_Testing.Models;
using ReTime_Testing.ViewModels.TimeScheduleEditor;

#nullable enable

namespace ReTime_Testing.Views.TimeScheduleEditor;

/// <summary>
/// 表组一级编辑页：周视图风格的指派行表。
/// 一个轮转周一张表格（表头行 = 星期 + 日期，指派行 = 计划表选择），周期内各周纵向堆叠；
/// 点击指派行弹出 ui:Flyout 选择计划表，修改在 500ms 防抖后统一落盘。
/// 组名/轮换配置/日期覆盖等高级项由二级抽屉 GroupEditDrawer 承担。
/// </summary>
public partial class GroupWeekArrangement : UserControl
{
    /// <summary>天列显示顺序：周一 → 周日（数据键 0=周日）</summary>
    private static readonly int[] DayOrder = { 1, 2, 3, 4, 5, 6, 0 };

    private static readonly string[] DayNames = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };

    private TimeScheduleEditorViewModel? _viewModel;
    private ScheduleGroup? _group;
    private bool _isLoading;

    /// <summary>堆叠的轮转周表格组（1 张 = 1 个轮转周）</summary>
    private readonly List<WeekTableItem> _tables = new();

    /// <summary>基础天→表映射工作副本</summary>
    private readonly Dictionary<string, string> _dayMap = new();

    /// <summary>轮转周差异映射工作副本（Key = 周序-1，"1"~"N-1"）</summary>
    private readonly Dictionary<string, Dictionary<string, string>> _rotatedMaps = new();

    /// <summary>全部可用计划表选项（不含空选项）</summary>
    private readonly List<ScheduleOption> _scheduleOptions = new();

    /// <summary>当前打开的计划表选择 Flyout（选中选项后收起）</summary>
    private FlyoutBase? _activeFlyout;

    /// <summary>当前打开选择器对应的天列（选中结果写回目标）</summary>
    private DayCellItem? _activeCell;

    /// <summary>同步选择器内容期间抑制写回（避免初始装配误改天列）</summary>
    private bool _syncingPicker;

    private readonly DispatcherTimer _autoSaveTimer;

    public GroupWeekArrangement()
    {
        InitializeComponent();

        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _autoSaveTimer.Tick += OnAutoSaveTimerTick;
    }

    /// <summary>
    /// 注入窗口的 ViewModel（与编辑抽屉共享同一实例）
    /// </summary>
    public void AttachViewModel(TimeScheduleEditorViewModel viewModel)
    {
        _viewModel ??= viewModel;
    }

    #region 加载 / 重载

    /// <summary>
    /// 加载组到编排页；切换到其他组时先提交旧组未触发的防抖保存
    /// </summary>
    public void LoadGroup(ScheduleGroup group)
    {
        if (_group != null && _group.Id != group.Id)
        {
            FlushPendingSave();

            // 防抖保存会触发 RefreshGroups 并重入本方法完成新组加载
            if (_group?.Id == group.Id) return;
        }
        else if (_group != null && _group.Id == group.Id)
        {
            // 同组刷新（保存后列表重建）：仅同步数据引用，保留表格现场
            _group = group;
            return;
        }

        FullLoad(group);
    }

    /// <summary>
    /// 从磁盘重新加载当前组（二级抽屉关闭后调用，同步轮换配置等改动）
    /// </summary>
    public void Reload()
    {
        if (_viewModel == null) return;

        FlushPendingSave();

        var group = _viewModel.GetSelectedGroupData();
        if (group == null)
        {
            Clear();
            return;
        }

        FullLoad(group);
    }

    /// <summary>
    /// 清空编排页（未选择任何组时）
    /// </summary>
    public void Clear()
    {
        _autoSaveTimer.Stop();
        _group = null;
        _activeFlyout = null;
        _activeCell = null;
        ReleaseTables();
        _dayMap.Clear();
        _rotatedMaps.Clear();
        _scheduleOptions.Clear();
    }

    private void FullLoad(ScheduleGroup group)
    {
        _isLoading = true;
        _group = group;

        // 工作副本（过滤空值，与落盘语义一致）
        _dayMap.Clear();
        foreach (var kv in group.DayScheduleMap)
        {
            if (!string.IsNullOrEmpty(kv.Value)) _dayMap[kv.Key] = kv.Value;
        }

        _rotatedMaps.Clear();
        foreach (var kv in group.RotatedDayScheduleMaps)
        {
            var diff = kv.Value
                .Where(v => !string.IsNullOrEmpty(v.Value))
                .ToDictionary(v => v.Key, v => v.Value);
            if (diff.Count > 0) _rotatedMaps[kv.Key] = diff;
        }

        // 可用计划表选项
        _scheduleOptions.Clear();
        foreach (var schedule in _viewModel?.GetAvailableSchedules() ?? new List<ScheduleListItem>())
        {
            if (!_scheduleOptions.Any(o => o.Id == schedule.Id))
                _scheduleOptions.Add(new ScheduleOption { Id = schedule.Id, Name = schedule.Name });
        }

        var cycleCount = Math.Clamp(group.RotationCycleCount, 1, 9);
        var currentWeek = Math.Clamp(_viewModel?.GetCurrentRotationWeek(group) ?? 1, 1, cycleCount);

        // 轮换锚点与 ScheduleGroupManager 同口径，用于推算各轮转周在当前周期内的实际日期
        var today = DateTime.Today;
        var anchor = today.AddDays(-(int)today.DayOfWeek);
        if (cycleCount > 1 && !string.IsNullOrEmpty(group.RotationStartDate) &&
            DateTime.TryParse(group.RotationStartDate, out var parsed))
        {
            anchor = parsed.Date;
        }

        var anchorDow = (int)anchor.DayOfWeek;
        DateTime cycleStart;
        if (cycleCount <= 1)
        {
            cycleStart = anchor;
        }
        else
        {
            var totalElapsed = (int)Math.Floor((today.Date - anchor).TotalDays / 7.0);
            var position = (totalElapsed + group.RotationOffset) % cycleCount;
            if (position < 0) position += cycleCount;
            cycleStart = anchor.AddDays((totalElapsed - position) * 7.0);
        }

        // 重建表格组
        ReleaseTables();

        for (var week = 1; week <= cycleCount; week++)
        {
            var table = new WeekTableItem
            {
                DisplayWeek = week,
                IsCurrentWeek = week == currentWeek,
                Title = TitleOf(week, cycleCount)
            };

            var weekStart = cycleStart.AddDays((week - 1) * 7.0);
            var map = week <= 1
                ? _dayMap
                : _rotatedMaps.TryGetValue(WeekKeyOf(week), out var rotated) ? rotated : null;
            var options = BuildOptions(week <= 1 ? "（未配置）" : "（继承基础映射）");

            for (var i = 0; i < DayOrder.Length; i++)
            {
                var day = DayOrder[i];
                string? rawId = null;
                map?.TryGetValue(day.ToString(), out rawId);

                var date = weekStart.AddDays(((day - anchorDow) % 7 + 7) % 7);
                var cell = new DayCellItem
                {
                    DayIndex = day,
                    DayName = DayNames[day],
                    DisplayWeek = week,
                    DateText = date.ToString("MM/dd"),
                    IsToday = date == today,
                    IsFirst = i == 0,
                    ScheduleOptions = options,
                    ScheduleId = rawId ?? ""
                };
                cell.PropertyChanged += OnCellPropertyChanged;
                UpdateCellView(cell);
                table.Cells.Add(cell);
            }

            _tables.Add(table);
        }

        WeekTablesControl.ItemsSource = _tables;
        RefreshSummaries();

        _isLoading = false;
    }

    /// <summary>
    /// 断开并清空现有表格组（重建 / 清空时调用）
    /// </summary>
    private void ReleaseTables()
    {
        WeekTablesControl.ItemsSource = null;
        foreach (var table in _tables)
        {
            foreach (var cell in table.Cells)
                cell.PropertyChanged -= OnCellPropertyChanged;
        }
        _tables.Clear();
    }

    #endregion

    #region 单元格构建

    private static string TitleOf(int week, int cycleCount)
        => cycleCount <= 1
            ? "每周安排"
            : week <= 1 ? "第 1 周（基础映射）" : $"第 {week} 周";

    private static string WeekKeyOf(int displayWeek) => (displayWeek - 1).ToString();

    /// <summary>
    /// 构建选择器选项：第一项为空选项（未配置 / 继承基础映射）+ 全部计划表
    /// </summary>
    private List<ScheduleOption> BuildOptions(string emptyLabel)
    {
        var options = new List<ScheduleOption> { new ScheduleOption { Id = "", Name = emptyLabel } };
        options.AddRange(_scheduleOptions);
        return options;
    }

    /// <summary>
    /// 刷新天列显示（生效计划表名、按钮文本、继承/差异标签）
    /// </summary>
    private void UpdateCellView(DayCellItem cell)
    {
        var baseId = _dayMap.TryGetValue(cell.DayIndex.ToString(), out var id) ? id : "";
        var effective = !string.IsNullOrEmpty(cell.ScheduleId)
            ? cell.ScheduleId
            : cell.DisplayWeek <= 1 ? "" : baseId;
        var hasValue = !string.IsNullOrEmpty(effective);

        cell.DisplayName = hasValue ? ResolveScheduleName(effective) : "（未配置）";
        cell.IsEmpty = !hasValue;
        cell.ButtonText = hasValue ? cell.DisplayName : "添加计划表";

        var rotationView = cell.DisplayWeek > 1;
        cell.ShowRotationTags = rotationView;
        cell.IsInherited = rotationView && string.IsNullOrEmpty(cell.ScheduleId);
        cell.TagText = cell.IsInherited ? "继承" : "差异";
    }

    private string ResolveScheduleName(string id)
        => _scheduleOptions.FirstOrDefault(o => o.Id == id)?.Name ?? "（已删除的计划表）";

    private void OnCellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isLoading || e.PropertyName != nameof(DayCellItem.ScheduleId)) return;
        if (sender is not DayCellItem cell) return;

        WriteCellToMaps(cell);

        // 基础映射改动会影响各轮转周继承列的显示，全量刷新
        foreach (var table in _tables)
        {
            foreach (var item in table.Cells)
                UpdateCellView(item);
        }
        RefreshSummaries();

        ScheduleAutoSave();
    }

    /// <summary>
    /// 把天列选择写入所属周的工作副本（空值移除键；轮转周清空后移除空映射）
    /// </summary>
    private void WriteCellToMaps(DayCellItem cell)
    {
        var key = cell.DayIndex.ToString();

        if (cell.DisplayWeek <= 1)
        {
            if (string.IsNullOrEmpty(cell.ScheduleId)) _dayMap.Remove(key);
            else _dayMap[key] = cell.ScheduleId;
            return;
        }

        var weekKey = WeekKeyOf(cell.DisplayWeek);
        if (string.IsNullOrEmpty(cell.ScheduleId))
        {
            if (_rotatedMaps.TryGetValue(weekKey, out var diff))
            {
                diff.Remove(key);
                if (diff.Count == 0) _rotatedMaps.Remove(weekKey);
            }
        }
        else
        {
            if (!_rotatedMaps.TryGetValue(weekKey, out var diff))
            {
                diff = new Dictionary<string, string>();
                _rotatedMaps[weekKey] = diff;
            }
            diff[key] = cell.ScheduleId;
        }
    }

    /// <summary>
    /// 刷新各表格标题行右侧的差异摘要
    /// </summary>
    private void RefreshSummaries()
    {
        foreach (var table in _tables)
            table.Summary = SummaryOf(table.DisplayWeek);
    }

    private string SummaryOf(int week)
    {
        var cycleCount = Math.Clamp(_group?.RotationCycleCount ?? 1, 1, 9);
        if (cycleCount <= 1) return "每周默认映射";
        if (week <= 1) return _rotatedMaps.Count > 0 ? "轮转周默认继承此映射" : "每周默认映射";

        var diffCount = _rotatedMaps.TryGetValue(WeekKeyOf(week), out var map) ? map.Count : 0;
        return diffCount == 0 ? "全部继承基础映射" : $"{diffCount} 天与基础映射不同";
    }

    #endregion

    #region 计划表选择 Flyout

    private void OnCellClick(object sender, RoutedEventArgs e)
    {
        if (_group == null) return;
        if (sender is not Button button) return;
        if (button.DataContext is not DayCellItem cell) return;
        if (FlyoutService.GetFlyout(button) is not Flyout flyout) return;

        _activeCell = cell;
        _activeFlyout = flyout;

        // Flyout 内容未进入视觉树时绑定不随 DataContext 刷新（见冒烟测试），
        // 改为命令式装配选项与当前选择，与事件顺序无关
        if (flyout.Content is ListBox listBox)
        {
            _syncingPicker = true;
            try
            {
                listBox.ItemsSource = cell.ScheduleOptions;
                listBox.SelectedValue = cell.ScheduleId;
            }
            finally
            {
                _syncingPicker = false;
            }
        }
    }

    /// <summary>
    /// 选择结果写回当前天列（初始装配期间抑制，避免误改）
    /// </summary>
    private void OnOptionSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingPicker || _activeCell == null) return;
        if (sender is not ListBox listBox) return;

        _activeCell.ScheduleId = listBox.SelectedValue as string ?? "";
    }

    private void OnOptionClick(object sender, RoutedEventArgs e)
    {
        // 点击选项行后收起选择器（选择已在按下时提交；滚动条等非选项区域不受影响）
        if (e.OriginalSource is not DependencyObject source || !HasListBoxItemAncestor(source)) return;
        _activeFlyout?.Hide();
    }

    private static bool HasListBoxItemAncestor(DependencyObject node)
    {
        while (node != null)
        {
            if (node is ListBoxItem) return true;
            node = VisualTreeHelper.GetParent(node);
        }
        return false;
    }

    #endregion

    #region 自动保存

    private void ScheduleAutoSave()
    {
        if (_isLoading || _group == null || _viewModel == null) return;

        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private void OnAutoSaveTimerTick(object? sender, EventArgs e)
    {
        _autoSaveTimer.Stop();
        Save();
    }

    /// <summary>
    /// 立即提交待保存的防抖编辑（切换组 / 打开二级抽屉 / 关闭窗口前调用）
    /// </summary>
    public void FlushPendingSave()
    {
        if (!_autoSaveTimer.IsEnabled) return;

        _autoSaveTimer.Stop();
        Save();
    }

    /// <summary>
    /// 汇总工作副本并一次性落盘（组名等非本页编辑的字段透传当前引用值）
    /// </summary>
    private void Save()
    {
        if (_group == null || _viewModel == null) return;

        var dayScheduleMap = _dayMap
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var rotatedMaps = _rotatedMaps
            .Where(kv => kv.Value.Count > 0)
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value
                    .Where(v => !string.IsNullOrEmpty(v.Value))
                    .ToDictionary(v => v.Key, v => v.Value));

        var dateOverrides = _group.DateOverrides
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        _viewModel.SaveGroupEdits(
            _group.Id,
            _group.Metadata.Name,
            _group.Metadata.Description,
            _group.RotationCycleCount,
            _group.RotationStartDate,
            _group.RotationOffset,
            dayScheduleMap,
            rotatedMaps,
            dateOverrides);
    }

    #endregion
}
