using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Models;
using ReTime_Testing.ViewModels.TimeScheduleEditor;

#nullable enable

namespace ReTime_Testing.Views.TimeScheduleEditor;

/// <summary>
/// 表组二级编辑抽屉：组名/描述、轮换配置、日期覆盖、激活/解散。
/// 天→表映射与轮转周覆盖由一级编排页 GroupWeekArrangement 负责。
/// 所有编辑在 500ms 防抖后统一落盘（延迟自动保存）
/// </summary>
public partial class GroupEditDrawer : UserControl
{
    private TimeScheduleEditorViewModel? _viewModel;
    private ScheduleGroup? _group;
    private bool _isLoading;
    private bool _isLoadedOnce;
    private readonly DispatcherTimer _autoSaveTimer;

    /// <summary>
    /// 全部可用计划表选项（不含空选项）
    /// </summary>
    private readonly List<ScheduleOption> _scheduleOptions = new();

    /// <summary>
    /// 保存完成事件（groupId, groupName）
    /// </summary>
    public event Action<string, string>? SaveCompleted;

    public GroupEditDrawer()
    {
        InitializeComponent();

        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _autoSaveTimer.Tick += OnAutoSaveTimerTick;
    }

    /// <summary>
    /// 注入窗口的 ViewModel（与预览面板共享同一实例，保存后预览会同步刷新）
    /// </summary>
    public void AttachViewModel(TimeScheduleEditorViewModel viewModel)
    {
        _viewModel ??= viewModel;
    }

    /// <summary>
    /// 加载组数据到UI控件
    /// 同一组的重复加载（保存后列表刷新导致的重新选中）会被忽略，避免打断正在编辑的输入
    /// </summary>
    public void LoadGroup(ScheduleGroup group)
    {
        if (_isLoadedOnce && _group != null && _group.Id == group.Id)
        {
            _group = group;
            return;
        }

        _viewModel ??= (Application.Current as App)?.Services.GetRequiredService<TimeScheduleEditorViewModel>();

        _isLoading = true;
        _group = group;

        // 基本属性
        GroupNameBox.Text = group.Metadata.Name;
        GroupNameBox.IsReadOnly = true;
        GroupNameEditButton.Visibility = Visibility.Visible;

        GroupDescBox.Text = group.Metadata.Description ?? "";
        GroupDescBox.IsReadOnly = true;
        GroupDescEditButton.Visibility = Visibility.Visible;

        DissolveGroupButton.IsEnabled = _viewModel != null && !_viewModel.IsGroupProtected(group.Id);

        // 轮换配置
        CycleCountBox.Value = group.RotationCycleCount;
        StartDatePicker.SelectedDate = DateTime.TryParse(group.RotationStartDate, out var startDate) ? startDate : null;
        OffsetBox.Value = group.RotationOffset;

        // 可用计划表选项
        _scheduleOptions.Clear();
        foreach (var schedule in _viewModel?.GetAvailableSchedules() ?? new List<ScheduleListItem>())
        {
            if (!_scheduleOptions.Any(o => o.Id == schedule.Id))
                _scheduleOptions.Add(new ScheduleOption { Id = schedule.Id, Name = schedule.Name });
        }

        // 日期覆盖
        DateOverrideList.Items.Clear();
        foreach (var entry in group.DateOverrides)
        {
            if (!DateTime.TryParse(entry.Key, out var date)) continue;

            var item = new DateOverrideItem
            {
                Date = date,
                ScheduleId = entry.Value,
                ScheduleOptions = BuildDateOptions(entry.Value)
            };
            HookDateItem(item);
            DateOverrideList.Items.Add(item);
        }

        UpdateEmptyHints();

        _isLoading = false;
        _isLoadedOnce = true;
    }

    #region 选项构建

    /// <summary>
    /// 构建日期覆盖的选项（无空选项；已删除的计划表补一个占位项）
    /// </summary>
    private List<ScheduleOption> BuildDateOptions(string scheduleId)
    {
        var options = new List<ScheduleOption>(_scheduleOptions);
        if (!string.IsNullOrEmpty(scheduleId) && !options.Any(o => o.Id == scheduleId))
            options.Add(new ScheduleOption { Id = scheduleId, Name = "（已删除的计划表）" });
        return options;
    }

    /// <summary>
    /// 订阅日期覆盖项变更
    /// </summary>
    private void HookDateItem(DateOverrideItem item)
    {
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(DateOverrideItem.Date) or nameof(DateOverrideItem.ScheduleId))
                ScheduleAutoSave();
        };
    }

    #endregion

    #region 编辑按钮

    private void OnGroupNameEditClick(object sender, RoutedEventArgs e)
    {
        if (_group == null) return;

        // 默认组保护
        if (_viewModel?.IsGroupProtected(_group.Id) == true) return;

        GroupNameBox.IsReadOnly = false;
        GroupNameBox.Focus();
        GroupNameBox.SelectAll();
        GroupNameEditButton.Visibility = Visibility.Collapsed;
    }

    private void OnGroupDescEditClick(object sender, RoutedEventArgs e)
    {
        if (_group == null) return;

        GroupDescBox.IsReadOnly = false;
        GroupDescBox.Focus();
        GroupDescEditButton.Visibility = Visibility.Collapsed;
    }

    private void OnEditBoxLostFocus(object sender, RoutedEventArgs e)
    {
        ExitEditMode(sender as TextBox);
    }

    private void OnGroupNameKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        ExitEditMode(sender as TextBox);
        e.Handled = true;
    }

    /// <summary>
    /// 结束名称/描述的编辑态（内容已由防抖保存写入）
    /// </summary>
    private void ExitEditMode(TextBox? box)
    {
        if (box == null || box.IsReadOnly) return;

        box.IsReadOnly = true;
        if (box == GroupNameBox)
            GroupNameEditButton.Visibility = Visibility.Visible;
        else if (box == GroupDescBox)
            GroupDescEditButton.Visibility = Visibility.Visible;

        ScheduleAutoSave();
    }

    #endregion

    #region 字段变更处理

    private void OnFieldChanged(object sender, RoutedEventArgs e)
    {
        ScheduleAutoSave();
    }

    private void OnCycleCountChanged(iNKORE.UI.WPF.Modern.Controls.NumberBox sender, iNKORE.UI.WPF.Modern.Controls.NumberBoxValueChangedEventArgs e)
    {
        // 周期数变化只影响轮换配置本身；周切换器在抽屉关闭时由编排页重载同步
        ScheduleAutoSave();
    }

    private void OnStartDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        ScheduleAutoSave();
    }

    private void OnOffsetChanged(iNKORE.UI.WPF.Modern.Controls.NumberBox sender, iNKORE.UI.WPF.Modern.Controls.NumberBoxValueChangedEventArgs e)
    {
        ScheduleAutoSave();
    }

    /// <summary>
    /// 当前轮换周期数（NumberBox 清空时为 NaN，按 1 处理）
    /// </summary>
    private int CurrentCycleCount
    {
        get
        {
            var value = CycleCountBox.Value;
            return double.IsNaN(value) || double.IsInfinity(value)
                ? 1
                : Math.Clamp((int)Math.Round(value), 1, 9);
        }
    }

    /// <summary>
    /// 当前轮换偏移量（NumberBox 清空时为 NaN，按 0 处理）
    /// </summary>
    private int CurrentRotationOffset
    {
        get
        {
            var value = OffsetBox.Value;
            return double.IsNaN(value) || double.IsInfinity(value)
                ? 0
                : (int)Math.Round(value);
        }
    }

    private void ScheduleAutoSave()
    {
        if (_isLoading || _group == null || _viewModel == null) return;

        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    #endregion

    #region 日期覆盖

    private void OnAddDateOverrideClick(object sender, RoutedEventArgs e)
    {
        if (_scheduleOptions.Count == 0) return;

        var existingDates = DateOverrideList.Items.OfType<DateOverrideItem>()
            .Select(i => i.Date?.Date)
            .ToHashSet();

        // 默认从今天开始找第一个未被占用的日期
        var date = DateTime.Today;
        for (var i = 0; i < 366 && existingDates.Contains(date); i++)
            date = date.AddDays(1);

        var item = new DateOverrideItem
        {
            Date = date,
            ScheduleId = _scheduleOptions[0].Id,
            ScheduleOptions = BuildDateOptions(_scheduleOptions[0].Id)
        };

        HookDateItem(item);
        DateOverrideList.Items.Add(item);
        UpdateEmptyHints();
        ScheduleAutoSave();
    }

    private void OnRemoveDateOverrideClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DateOverrideItem item) return;

        DateOverrideList.Items.Remove(item);
        UpdateEmptyHints();
        ScheduleAutoSave();
    }

    private void UpdateEmptyHints()
    {
        DateEmptyHint.Visibility = DateOverrideList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    #endregion

    #region 自动保存

    private void OnAutoSaveTimerTick(object? sender, EventArgs e)
    {
        _autoSaveTimer.Stop();
        Save();
    }

    /// <summary>
    /// 立即提交待保存的防抖编辑（窗口关闭前调用）
    /// </summary>
    public void FlushPendingSave()
    {
        if (!_autoSaveTimer.IsEnabled) return;

        _autoSaveTimer.Stop();
        Save();
    }

    /// <summary>
    /// 汇总抽屉中的全部数据并一次性保存到ViewModel
    /// </summary>
    private void Save()
    {
        if (_group == null || _viewModel == null) return;

        var groupId = _group.Id;

        // 基本属性（名称、描述）：文本框始终反映当前内容，直接取值即可
        string newName = string.IsNullOrWhiteSpace(GroupNameBox.Text) ? _group.Metadata.Name : GroupNameBox.Text.Trim();
        string? newDesc = string.IsNullOrWhiteSpace(GroupDescBox.Text) ? null : GroupDescBox.Text.Trim();

        // 轮换配置
        int cycleCount = CurrentCycleCount;
        string? startDate = StartDatePicker.SelectedDate?.ToString("yyyy-MM-dd");
        int offset = CurrentRotationOffset;

        // 基础天→表映射与轮转覆盖由一级编排页维护：打开抽屉前已提交，这里透传磁盘当前值
        // （空值过滤与空周剔除保持落盘语义一致）
        var dayScheduleMap = _group.DayScheduleMap
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var rotatedMaps = _group.RotatedDayScheduleMaps
            .Where(kv => kv.Value != null && kv.Value.Any(v => !string.IsNullOrEmpty(v.Value)))
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value
                    .Where(v => !string.IsNullOrEmpty(v.Value))
                    .ToDictionary(v => v.Key, v => v.Value));

        // 日期覆盖（重复日期保留先出现的一条）
        var dateOverrides = new Dictionary<string, string>();
        foreach (var item in DateOverrideList.Items.OfType<DateOverrideItem>())
        {
            if (item.Date == null || string.IsNullOrEmpty(item.ScheduleId)) continue;
            dateOverrides.TryAdd(item.Date.Value.ToString("yyyy-MM-dd"), item.ScheduleId);
        }

        _viewModel.SaveGroupEdits(groupId, newName, newDesc, cycleCount, startDate, offset,
            dayScheduleMap, rotatedMaps, dateOverrides);

        // 同步本地引用
        if (_group != null)
        {
            _group.Metadata.Name = newName;
            _group.Metadata.Description = newDesc;
            _group.RotationCycleCount = cycleCount;
            _group.RotationStartDate = startDate;
            _group.RotationOffset = offset;
            _group.DayScheduleMap = dayScheduleMap;
            _group.RotatedDayScheduleMaps = rotatedMaps;
            _group.DateOverrides = dateOverrides;
        }

        SaveCompleted?.Invoke(groupId, newName);
    }

    #endregion

    #region 操作按钮

    private void OnActivateGroupClick(object sender, RoutedEventArgs e)
    {
        if (_group == null || _viewModel == null) return;
        _viewModel.ActivateGroupByIdCommand.Execute(_group.Id);
    }

    /// <summary>
    /// 危险操作确认：解散当前组（由 DissolveConfirmFlyout 确认按钮触发）
    /// </summary>
    private void OnDissolveConfirmClick(object sender, RoutedEventArgs e)
    {
        DissolveConfirmFlyout?.Hide();
        if (_group == null || _viewModel == null) return;
        _viewModel.DisbandGroupCommand.Execute(_group.Id);
    }

    /// <summary>
    /// 取消解散（收起确认 Flyout）
    /// </summary>
    private void OnDissolveCancelClick(object sender, RoutedEventArgs e)
    {
        DissolveConfirmFlyout?.Hide();
    }

    #endregion
}
