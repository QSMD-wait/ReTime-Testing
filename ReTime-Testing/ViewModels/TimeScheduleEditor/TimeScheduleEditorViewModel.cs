using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Models.UI;
using ReTime_Testing.Services;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

public partial class TimeScheduleEditorViewModel : ObservableObject, IDisposable
{
        private readonly ILogger<TimeScheduleEditorViewModel> _logger;
    private readonly ILogger<ExecutionPlanGenerator> _planGeneratorLogger;
    private readonly ITimeScheduleManager _scheduleManager;
    private readonly IScheduleGroupManager _groupManager;
    private readonly ISettingsService _settingsService;
    private readonly ITimeService? _timeService;
    private readonly IScheduleManager? _scheduleRunManager;

    private readonly Dictionary<string, ScheduleEditingState> _editingStates = new();
    private ScheduleEditingState? _currentEditingState;

    private ScheduleItemListItem? _previousSelectedItem;

    private bool _isSwitchingSchedule = false;

    private readonly DispatcherTimer _autoSaveTimer;

    public event Func<string, List<string>, Task<bool>>? ForceSaveConfirmRequested;
    public event Action<ToastMessage>? ToastRequested;
    public event Func<string, Task<string?>>? CreateGroupNameRequested;

    [ObservableProperty]
    private bool _hasUnpersistedChanges = false;

    public bool HasAnyUnpersistedChanges => _editingStates.Values.Any(s => s.HasUnpersistedChanges);

    [ObservableProperty]
    private ScheduleListItem? _selectedSchedule;

    [ObservableProperty]
    private ScheduleItemListItem? _selectedScheduleItem;

    [ObservableProperty]
    private ScheduleGroupListItem? _selectedGroup;

    /// <summary>
    /// 顶部「加载表组」按钮动态文字：该组生效时显示组名，否则为默认动作文字
    /// </summary>
    [ObservableProperty]
    private string _groupButtonLabel = LoadStateTextResolver.GroupActionText;

    /// <summary>
    /// 顶部「加载计划表」按钮动态文字：该表覆盖生效时显示表名，否则为默认动作文字
    /// </summary>
    [ObservableProperty]
    private string _scheduleButtonLabel = LoadStateTextResolver.ScheduleActionText;

    /// <summary>
    /// 「加载表组」按钮 ToolTip 动态行（生效时的轮换信息，未生效为空以隐藏该行）
    /// </summary>
    [ObservableProperty]
    private string _groupButtonDetail = "";

    /// <summary>
    /// 「加载计划表」按钮 ToolTip 动态行（启用方式说明，未生效为空以隐藏该行）
    /// </summary>
    [ObservableProperty]
    private string _scheduleButtonDetail = "";

    /// <summary>
    /// 「加载计划表」按钮文字是否为次级信息：
    /// 表组生效时该按钮显示由轮换得到的"当前应用的表"，用次要色+细字重与主（组名）差分
    /// </summary>
    [ObservableProperty]
    private bool _isScheduleLabelSecondary;

    /// <summary>
    /// 「加载表组」按钮文字是否为次级信息：
    /// 仅当天临时启用接管时组配置仍保留，组名以次级样式显示并提示明日恢复
    /// </summary>
    [ObservableProperty]
    private bool _isGroupLabelSecondary;

    [ObservableProperty]
    private bool _canUndo = false;

    [ObservableProperty]
    private bool _canRedo = false;

    public ObservableCollection<ScheduleListItem> Schedules { get; } = new();
    public ObservableCollection<ScheduleItemListItem> ScheduleItems => _currentEditingState?.Items ?? _emptyItems;
    public ObservableCollection<ScheduleGroupListItem> Groups { get; } = new();

    private readonly ObservableCollection<ScheduleItemListItem> _emptyItems = new();

    public bool IsSegmentSelected => SelectedScheduleItem != null && SelectedScheduleItem.ItemType == ScheduleItemType.Segment;
    public bool IsTimePointSelected => SelectedScheduleItem != null && SelectedScheduleItem.ItemType == ScheduleItemType.TimePoint;
    public bool HasScheduleItems => _currentEditingState != null && _currentEditingState.Items.Count > 0;

    public List<StateOptionItem> ToStateOptions { get; } = new()
    {
        new() { Value = ProgressStateType.Loading, DisplayName = "加载中 (Loading)" },
        new() { Value = ProgressStateType.Success, DisplayName = "成功 (Success)" },
        new() { Value = ProgressStateType.Error, DisplayName = "错误 (Error)" },
        new() { Value = ProgressStateType.Paused, DisplayName = "暂停 (Paused)" },
    };

    private const string DEFAULT_GROUP_ID = ScheduleGroup.DefaultGroupId;
    private const string DEFAULT_GROUP_NAME = "默认";

    public TimeScheduleEditorViewModel(
        ILogger<TimeScheduleEditorViewModel> logger,
        ILogger<ExecutionPlanGenerator> planGeneratorLogger,
        ITimeScheduleManager scheduleManager,
        IScheduleGroupManager groupManager,
        ISettingsService settingsService,
        ITimeService? timeService = null,
        IScheduleManager? scheduleRunManager = null)
    {
        _logger = logger;
        _planGeneratorLogger = planGeneratorLogger;
        _scheduleManager = scheduleManager;
        _groupManager = groupManager;
        _settingsService = settingsService;
        _timeService = timeService;
        _scheduleRunManager = scheduleRunManager;

        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _autoSaveTimer.Tick += OnAutoSaveTimerTick;

        RefreshScheduleList();
        RefreshGroups();
    }

    /// <summary>
    /// 释放自动保存计时器（窗口真正关闭时由 View 调用；常驻隐藏不调用）
    /// </summary>
    public void Dispose()
    {
        _autoSaveTimer.Stop();
        _autoSaveTimer.Tick -= OnAutoSaveTimerTick;
    }

    #region 计划表选择切换

    partial void OnSelectedScheduleChanged(ScheduleListItem? value)
    {
        if (_isSwitchingSchedule) return;

        LoadScheduleForSelection(value);
    }

    private void LoadScheduleForSelection(ScheduleListItem? value)
    {
        _autoSaveTimer.Stop();

        if (value != null)
        {
            if (!_editingStates.TryGetValue(value.Id, out var state))
            {
                var schedule = _scheduleManager.LoadSchedule(value.Id);
                if (schedule == null)
                {
                    _currentEditingState = null;
                    UpdateScheduleItemsBinding();
                    return;
                }

                state = new ScheduleEditingState(value.Id);
                state.LoadFromSchedule(schedule);
                _editingStates[value.Id] = state;
            }

            _currentEditingState = state;
        }
        else
        {
            _currentEditingState = null;
        }

        UpdateScheduleItemsBinding();
        SelectedScheduleItem = null;
        UpdateUndoRedoState();
        UpdateHasUnpersistedChanges();

        if (_currentEditingState != null)
        {
            ValidateAllItems();
            TryStartAutoSaveTimer();
        }

        OnPropertyChanged(nameof(HasScheduleItems));
    }

    private void UpdateScheduleItemsBinding()
    {
        OnPropertyChanged(nameof(ScheduleItems));
        OnPropertyChanged(nameof(HasScheduleItems));
    }

    #endregion

    #region 选中项变更

    partial void OnSelectedScheduleItemChanged(ScheduleItemListItem? value)
    {
        if (_previousSelectedItem != null)
        {
            _previousSelectedItem.ItemChanged -= OnScheduleItemChanged;
        }

        if (value != null)
        {
            value.ItemChanged += OnScheduleItemChanged;
        }

        _previousSelectedItem = value;

        OnPropertyChanged(nameof(IsSegmentSelected));
        OnPropertyChanged(nameof(IsTimePointSelected));
    }

    private void OnScheduleItemChanged(ScheduleItemListItem item)
    {
        ValidateAllItems();
        UpdateHasUnpersistedChanges();

        if (_currentEditingState != null && !_currentEditingState.HasValidationErrors)
        {
            TryStartAutoSaveTimer();
        }
        else
        {
            _autoSaveTimer.Stop();
        }
    }

    #endregion

    #region 自动保存

    private void TryStartAutoSaveTimer()
    {
        if (_currentEditingState == null || !_currentEditingState.HasUnpersistedChanges)
        {
            _autoSaveTimer.Stop();
            return;
        }

        if (_currentEditingState.HasValidationErrors)
        {
            _autoSaveTimer.Stop();
            return;
        }

        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private void OnAutoSaveTimerTick(object? sender, EventArgs e)
    {
        _autoSaveTimer.Stop();

        if (_currentEditingState == null) return;

        if (_currentEditingState.HasValidationErrors)
        {
            return;
        }

        if (PerformSave(force: false))
        {
        }
    }

    #endregion

    #region 计划表操作命令

    [RelayCommand]
    private void AddSchedule()
    {
        var newId = $"schedule_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..4]}";
        var newName = "新计划表";

        var schedule = _scheduleManager.CreateNewSchedule(newId, newName);
        if (schedule != null)
        {
            _logger.LogInformation("创建新计划表: {ScheduleId}", newId);
            RefreshScheduleList();
            SelectedSchedule = Schedules.FirstOrDefault(s => s.Id == newId);
        }
    }

    [RelayCommand]
    private void CopySchedule()
    {
        if (SelectedSchedule == null) return;

        var newId = $"schedule_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..4]}";
        var newSchedule = _scheduleManager.CopySchedule(SelectedSchedule.Id, newId);

        if (newSchedule != null)
        {
            RefreshScheduleList();
            SelectedSchedule = Schedules.FirstOrDefault(s => s.Id == newId);
        }
    }

    [RelayCommand]
    private void DeleteSchedule()
    {
        if (SelectedSchedule == null) return;

        var deletedId = SelectedSchedule.Id;

        if (_scheduleManager.DeleteSchedule(deletedId))
        {
            var setting = _settingsService.GetTimeTopSetting();
            var needSave = false;

            // 清理手动指定（该表被删除时）
            needSave = setting.Schedule.ClearManualFor(deletedId);

            if (needSave)
                _settingsService.SaveTimeTopSetting(setting);

            _editingStates.Remove(deletedId);

            RefreshScheduleList();
            RefreshGroups();
            SelectedSchedule = null;
        }
    }

    [RelayCommand]
    private void ActivateSchedule(string? scheduleId)
    {
        if (string.IsNullOrEmpty(scheduleId)) return;

        var setting = _settingsService.GetTimeTopSetting();
        // 右键"设为活跃"为覆盖式启用（长期）：清掉可能残留的临时启用日期，
        // 并与覆盖式启用同语义清除表组设置
        setting.Schedule.EnableManualPermanent(scheduleId);
        _settingsService.SaveTimeTopSetting(setting);

        UpdateScheduleListActivation(scheduleId);
        // 单独启用表后，组的生效圆点需要同步消失
        RefreshGroups();
    }

    #endregion

    #region 时间段/时间点操作命令

    [RelayCommand]
    private void AddTimeSegment()
    {
        if (_currentEditingState == null) return;

        ComputeDefaultTime(isTimePoint: false, out var startTime, out var endTime);

        var newSegment = new ScheduleItemListItem
        {
            Id = $"segment_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..4]}",
            Name = "新时间段",
            StartTime = startTime,
            EndTime = endTime,
            ItemType = ScheduleItemType.Segment
        };

        _currentEditingState.ExecuteAction(new AddItemAction(newSegment));
        SelectedScheduleItem = newSegment;
        OnScheduleItemsChanged();
    }

    [RelayCommand]
    private void AddTimePoint()
    {
        if (_currentEditingState == null) return;

        ComputeDefaultTime(isTimePoint: true, out var startTime, out _);

        var newTimePoint = new ScheduleItemListItem
        {
            Id = $"tp_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..4]}",
            Name = "新时间点",
            StartTime = startTime,
            ItemType = ScheduleItemType.TimePoint,
            HasStateChange = true,
            HasStyleChange = false,
            ToState = ProgressStateType.Success
        };

        _currentEditingState.ExecuteAction(new AddItemAction(newTimePoint));
        SelectedScheduleItem = newTimePoint;
        OnScheduleItemsChanged();
    }

    [RelayCommand]
    private void DeleteScheduleItem()
    {
        if (_currentEditingState == null || SelectedScheduleItem == null) return;

        var index = _currentEditingState.Items.IndexOf(SelectedScheduleItem);
        _currentEditingState.ExecuteAction(new RemoveItemAction(SelectedScheduleItem, index));
        SelectedScheduleItem = null;
        OnScheduleItemsChanged();
    }

    [RelayCommand]
    private void RefreshOrder()
    {
        if (_currentEditingState == null) return;

        var validItems = _currentEditingState.Items
            .Where(i => TryParseTime(i.StartTime, out _))
            .OrderBy(i => TimeSpan.Parse(i.StartTime))
            .Select(i => i.Id)
            .ToList();

        var invalidItems = _currentEditingState.Items
            .Where(i => !TryParseTime(i.StartTime, out _))
            .Select(i => i.Id)
            .ToList();

        var sortedIds = validItems.Concat(invalidItems).ToArray();

        var currentIds = _currentEditingState.Items.Select(i => i.Id).ToArray();
        if (currentIds.SequenceEqual(sortedIds)) return;

        _currentEditingState.ExecuteAction(new SortAllAction(_currentEditingState.Items, sortedIds));
        OnScheduleItemsChanged();
    }

    [RelayCommand]
    private void Save()
    {
        if (_currentEditingState == null) return;

        ValidateAllItems();

        if (_currentEditingState.HasValidationErrors)
        {
            var errors = CollectValidationErrors();
            _ = ShowForceSaveDialogAsync(errors);
            return;
        }

        if (PerformSave(force: false))
        {
            ToastRequested?.Invoke(new ToastMessage("保存成功", $"计划表 \"{_currentEditingState.ScheduleId}\" 已保存") { Severity = ToastSeverity.Success, Duration = TimeSpan.FromSeconds(2) });
        }
    }

    public void ForceSave()
    {
        if (_currentEditingState == null) return;

        if (PerformSave(force: true))
        {
            ToastRequested?.Invoke(new ToastMessage("已强制保存", $"计划表 \"{_currentEditingState.ScheduleId}\" 已强制保存，可能存在验证错误") { Severity = ToastSeverity.Warning, Duration = TimeSpan.FromSeconds(3) });
        }
    }

    [RelayCommand]
    private void Undo()
    {
        if (_currentEditingState == null) return;

        _currentEditingState.Undo();
        UpdateUndoRedoState();
        UpdateHasUnpersistedChanges();
        ValidateAllItems();
        UpdateScheduleItemsBinding();
    }

    [RelayCommand]
    private void Redo()
    {
        if (_currentEditingState == null) return;

        _currentEditingState.Redo();
        UpdateUndoRedoState();
        UpdateHasUnpersistedChanges();
        ValidateAllItems();
        UpdateScheduleItemsBinding();
    }

    #endregion

    #region 保存核心逻辑

    private bool PerformSave(bool force)
    {
        if (_currentEditingState == null) return false;

        if (!force)
        {
            ValidateAllItems();
            if (_currentEditingState.HasValidationErrors)
            {
                return false;
            }
        }

        try
        {
            var schedule = BuildScheduleFromState(_currentEditingState);
            _scheduleManager.SaveSchedule(schedule);
            _currentEditingState.MarkAsSaved();

            UpdateHasUnpersistedChanges();

            _logger.LogInformation("计划表保存成功: {ScheduleId}", _currentEditingState.ScheduleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "计划表保存失败: {ScheduleId}", _currentEditingState.ScheduleId);
            ToastRequested?.Invoke(new ToastMessage("保存失败", ex.Message) { Severity = ToastSeverity.Error, Duration = TimeSpan.FromSeconds(8) });
            return false;
        }
    }

    private TimeSchedule BuildScheduleFromState(ScheduleEditingState state)
    {
        var schedule = _scheduleManager.LoadSchedule(state.ScheduleId) ?? new TimeSchedule
        {
            Id = state.ScheduleId,
            Version = "1.0.0",
            Settings = new TimeScheduleSettings
            {
                Metadata = new TimeScheduleMetadata()
            }
        };

        schedule.Id = state.ScheduleId;
        schedule.Schedules ??= new List<TimeScheduleItem>();
        schedule.TimePoints ??= new List<CustomTimePoint>();

        var currentItemIds = state.Items.Select(i => i.Id).ToHashSet();

        schedule.Schedules.RemoveAll(s => !currentItemIds.Contains(s.Id));
        schedule.TimePoints.RemoveAll(t => !currentItemIds.Contains(t.Id));

        foreach (var item in state.Items)
        {
            if (item.ItemType == ScheduleItemType.TimePoint)
            {
                var existingIndex = schedule.TimePoints.FindIndex(t => t.Id == item.Id);

                if (existingIndex >= 0)
                {
                    ScheduleItemConverter.ApplyListItemToTimePoint(item, schedule.TimePoints[existingIndex]);
                }
                else
                {
                    schedule.TimePoints.Add(ScheduleItemConverter.ToTimePoint(item));
                }
            }
            else
            {
                var existingIndex = schedule.Schedules.FindIndex(s => s.Id == item.Id);

                if (existingIndex >= 0)
                {
                    ScheduleItemConverter.ApplyListItemToSegment(item, schedule.Schedules[existingIndex]);
                }
                else
                {
                    schedule.Schedules.Add(ScheduleItemConverter.ToScheduleItem(item));
                }
            }
        }

        return schedule;
    }

    private async Task ShowForceSaveDialogAsync(List<string> errors)
    {
        if (ForceSaveConfirmRequested != null)
        {
            var shouldForce = await ForceSaveConfirmRequested("存在验证错误", errors);
            if (shouldForce)
            {
                ForceSave();
            }
            else
            {
                ToastRequested?.Invoke(new ToastMessage("保存已取消", "请修正验证错误后再保存") { Severity = ToastSeverity.Warning, Duration = TimeSpan.FromSeconds(3) });
            }
        }
        else
        {
            ToastRequested?.Invoke(new ToastMessage("无法保存", "存在验证错误，请修正后再保存") { Severity = ToastSeverity.Warning, Duration = TimeSpan.FromSeconds(3) });
        }
    }

    #endregion

    #region 数据加载

    public void RefreshScheduleList()
    {
        Schedules.Clear();

        var scheduleList = _scheduleManager.GetScheduleList();
        var effectiveScheduleId = _groupManager.GetEffectiveScheduleId();

        var sortedList = scheduleList
            .OrderBy(i => i.CreatedAt ?? DateTime.MaxValue)
            .ToList();

        foreach (var info in sortedList)
        {
            Schedules.Add(new ScheduleListItem
            {
                Id = info.Id,
                Name = info.Name,
                Description = info.Description,
                IsEnabled = info.IsEnabled,
                IsActivated = info.Id == effectiveScheduleId,
                CreatedAt = info.CreatedAt,
                UpdatedAt = info.UpdatedAt
            });
        }

        RefreshLoadState();
    }

    /// <summary>
    /// 刷新顶部两个加载按钮的动态文字、ToolTip 细节与主次样式标记（由 RefreshGroups / RefreshScheduleList
    /// 末尾统一调用，激活组、覆盖启用、加载、改名、删除等所有状态变更路径都会流经这两个方法）
    /// </summary>
    private void RefreshLoadState()
    {
        var config = _settingsService.GetTimeTopSetting().Schedule;

        // 激活组（覆盖接管时不生效）：解析组名与轮换信息
        string? activeGroupName = null;
        string groupRotationInfo = "";
        if (!string.IsNullOrEmpty(config.ActiveGroupId))
        {
            var group = _groupManager.LoadGroup(config.ActiveGroupId);
            if (group != null)
            {
                activeGroupName = group.Metadata.Name;
                groupRotationInfo = group.RotationCycleCount > 1
                    ? _groupManager.GetRotationInfo(group.Id)
                    : "";
            }
        }

        // 手动指定的计划表（表已被删除时为 null，按钮回退默认动作文字）；
        // 未手动指定时解析今天实际生效的表（由表组轮换决定，作为计划表按钮的次级显示）
        string? manualScheduleName = null;
        string? effectiveScheduleName = null;
        var manual = config.EffectiveManual;
        if (manual != null)
        {
            manualScheduleName = _scheduleManager.LoadSchedule(manual.ScheduleId)?
                .Settings.Metadata.Name;
        }
        else
        {
            var effectiveId = _groupManager.GetEffectiveScheduleId();
            if (!string.IsNullOrEmpty(effectiveId))
            {
                effectiveScheduleName = _scheduleManager.LoadSchedule(effectiveId)?
                    .Settings.Metadata.Name;
            }
        }

        var (groupLabel, groupDetail, isGroupSecondary, scheduleLabel, scheduleDetail, isScheduleSecondary) =
            LoadStateTextResolver.Resolve(config, activeGroupName, groupRotationInfo, manualScheduleName, effectiveScheduleName);

        GroupButtonLabel = groupLabel;
        GroupButtonDetail = groupDetail;
        IsGroupLabelSecondary = isGroupSecondary;
        ScheduleButtonLabel = scheduleLabel;
        ScheduleButtonDetail = scheduleDetail;
        IsScheduleLabelSecondary = isScheduleSecondary;
    }

    #endregion

    #region 验证

    public void ValidateAllItems()
    {
        if (_currentEditingState == null) return;

        var items = _currentEditingState.Items;

        foreach (var item in items)
        {
            item.StartTimeError = "";
            item.EndTimeError = "";
        }

        var segments = items.Where(i => i.ItemType == ScheduleItemType.Segment).ToList();
        var timePoints = items.Where(i => i.ItemType == ScheduleItemType.TimePoint).ToList();

        foreach (var seg in segments)
        {
            bool hasStartError = false;
            bool hasEndError = false;

            if (string.IsNullOrEmpty(seg.StartTime))
            {
                seg.StartTimeError = "不能为空";
                hasStartError = true;
            }
            else if (!TimeFormatValidator.IsValidFormat(seg.StartTime))
            {
                seg.StartTimeError = "格式应为 HH:mm:ss";
                hasStartError = true;
            }

            if (string.IsNullOrEmpty(seg.EndTime))
            {
                seg.EndTimeError = "不能为空";
                hasEndError = true;
            }
            else if (!TimeFormatValidator.IsValidFormat(seg.EndTime))
            {
                seg.EndTimeError = "格式应为 HH:mm:ss";
                hasEndError = true;
            }

            if (!hasStartError && !hasEndError && !string.IsNullOrEmpty(seg.StartTime) && !string.IsNullOrEmpty(seg.EndTime))
            {
                try
                {
                    var start = TimeSpan.Parse(seg.StartTime);
                    var end = TimeSpan.Parse(seg.EndTime);
                    if (end < start)
                    {
                        seg.EndTimeError = "结束时间不能早于开始时间";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "时间格式解析失败: {StartTime}/{EndTime}", seg.StartTime, seg.EndTime);
                    seg.StartTimeError = "时间格式无效";
                }
            }
        }

        foreach (var tp in timePoints)
        {
            if (string.IsNullOrEmpty(tp.StartTime))
            {
                tp.StartTimeError = "不能为空";
            }
            else if (!TimeFormatValidator.IsValidFormat(tp.StartTime))
            {
                tp.StartTimeError = "格式应为 HH:mm:ss";
            }
        }

        for (int i = 0; i < segments.Count; i++)
        {
            for (int j = i + 1; j < segments.Count; j++)
            {
                var a = segments[i];
                var b = segments[j];

                if (!TimeFormatValidator.IsValidFormat(a.StartTime) ||
                    !TimeFormatValidator.IsValidFormat(a.EndTime) ||
                    !TimeFormatValidator.IsValidFormat(b.StartTime) ||
                    !TimeFormatValidator.IsValidFormat(b.EndTime))
                    continue;

                if (!TryParseTime(a.StartTime, out var aStart) ||
                    !TryParseTime(a.EndTime, out var aEnd) ||
                    !TryParseTime(b.StartTime, out var bStart) ||
                    !TryParseTime(b.EndTime, out var bEnd))
                    continue;

                if (aEnd < aStart) aEnd = aEnd.Add(TimeSpan.FromDays(1));
                if (bEnd < bStart) bEnd = bEnd.Add(TimeSpan.FromDays(1));

                if (aStart < bEnd && bStart < aEnd)
                {
                    a.StartTimeError = "与其他时间段重叠";
                    b.StartTimeError = "与其他时间段重叠";
                }
            }
        }

        foreach (var tp in timePoints)
        {
            if (!TimeFormatValidator.IsValidFormat(tp.StartTime))
                continue;

            if (!TryParseTime(tp.StartTime, out var tpTime))
                continue;

            // 仅包含 StateChange 的时间点不能在时间段内部
            if (!tp.HasStateChange)
                continue;

            foreach (var seg in segments)
            {
                if (!TimeFormatValidator.IsValidFormat(seg.StartTime) ||
                    !TimeFormatValidator.IsValidFormat(seg.EndTime))
                    continue;

                if (!TryParseTime(seg.StartTime, out var segStart) ||
                    !TryParseTime(seg.EndTime, out var segEnd))
                    continue;

                if (segEnd < segStart) segEnd = segEnd.Add(TimeSpan.FromDays(1));

                if (tpTime > segStart && tpTime < segEnd)
                {
                    tp.StartTimeError = "位于时间段内部";
                }
            }
        }

        _currentEditingState.ValidationErrors.Clear();
        foreach (var item in items)
        {
            if (item.HasStartTimeError)
                _currentEditingState.ValidationErrors.Add($"[{item.Name}] {item.StartTimeError}");
            if (item.HasEndTimeError)
                _currentEditingState.ValidationErrors.Add($"[{item.Name}] {item.EndTimeError}");
            if (item.HasTypeError)
                _currentEditingState.ValidationErrors.Add($"[{item.Name}] 至少需要启用一种类型");
        }
    }

    private List<string> CollectValidationErrors()
    {
        if (_currentEditingState == null) return new List<string>();
        return _currentEditingState.ValidationErrors.ToList();
    }

    private bool TryParseTime(string timeString, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        if (string.IsNullOrEmpty(timeString)) return false;
        return TimeSpan.TryParse(timeString, out result);
    }

    #endregion

    #region 状态更新

    private void OnScheduleItemsChanged()
    {
        UpdateHasUnpersistedChanges();
        UpdateUndoRedoState();
        ValidateAllItems();
        OnPropertyChanged(nameof(ScheduleItems));
        OnPropertyChanged(nameof(HasScheduleItems));

        if (_currentEditingState != null && !_currentEditingState.HasValidationErrors)
        {
            TryStartAutoSaveTimer();
        }
    }

    private void UpdateHasUnpersistedChanges()
    {
        HasUnpersistedChanges = _currentEditingState?.HasUnpersistedChanges ?? false;
        OnPropertyChanged(nameof(HasAnyUnpersistedChanges));
    }

    private void UpdateUndoRedoState()
    {
        CanUndo = _currentEditingState?.CanUndo ?? false;
        CanRedo = _currentEditingState?.CanRedo ?? false;
    }

    #endregion

    #region 辅助方法

    private void ComputeDefaultTime(bool isTimePoint, out string defaultStartTime, out string defaultEndTime)
    {
        defaultStartTime = "09:00:00";
        defaultEndTime = "10:00:00";

        if (SelectedScheduleItem != null && !string.IsNullOrEmpty(SelectedScheduleItem.StartTime))
        {
            if (TryParseTime(SelectedScheduleItem.StartTime, out var baseTime))
            {
                defaultStartTime = baseTime.ToString(@"hh\:mm\:ss");
                if (!isTimePoint)
                {
                    defaultEndTime = baseTime.Add(TimeSpan.FromMinutes(10)).ToString(@"hh\:mm\:ss");
                }
            }
        }
    }

    public void UpdateScheduleListActivation(string activatedScheduleId)
    {
        foreach (var schedule in Schedules)
        {
            schedule.IsActivated = schedule.Id == activatedScheduleId;
        }
    }

    public List<ScheduleListItem> BuildScheduleListItems()
    {
        var scheduleList = _scheduleManager.GetScheduleList();
        var currentSelectedId = _settingsService.GetTimeTopSetting().Schedule.Manual?.ScheduleId;

        return scheduleList
            .OrderBy(i => i.CreatedAt ?? DateTime.MaxValue)
            .Select(s => new ScheduleListItem
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActivated = s.Id == currentSelectedId,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList();
    }

    public async Task<(bool Success, string? ErrorMessage)> HotReloadScheduleAsync(string scheduleId)
    {
        if (_scheduleRunManager == null || _timeService == null)
            return (false, "调度服务未初始化");

        try
        {
            var schedule = _scheduleManager.LoadSchedule(scheduleId);
            if (schedule == null)
                return (false, $"计划表 \"{scheduleId}\" 不存在");

            var planGenerator = new ExecutionPlanGenerator(_planGeneratorLogger);
            var now = _timeService.GetCurrentTime();
            var newPlan = planGenerator.Generate(schedule, now.Date, now);
            _scheduleRunManager.RegenerateExecutionPlan(newPlan);

            _timeService.Calibrate(_timeService.GetCurrentTime(), TimeJumpReason.ManualCalibration, TimeJumpSeverity.Minor);

            _logger.LogInformation("热重载成功: {ScheduleId}", scheduleId);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "热重载失败");
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 启用一张计划表并热重载调度（"加载计划表"弹窗）
    /// </summary>
    /// <param name="onlyToday">true = 仅当天临时启用（保留表组设置，次日自动回落轮换）；false = 覆盖式启用（清除表组设置）</param>
    public void ApplyScheduleSelection(ScheduleListItem selectedItem, bool onlyToday = false)
    {
        var setting = _settingsService.GetTimeTopSetting();
        // 仅当天临时启用保留表组设置（次日自动恢复轮换）；
        // 覆盖式启用视为放弃组轮换，清除表组设置，组按钮回退"加载表组"
        if (onlyToday)
            setting.Schedule.EnableManualToday(selectedItem.Id, DateTime.Now.ToString("yyyy-MM-dd"));
        else
            setting.Schedule.EnableManualPermanent(selectedItem.Id);
        _settingsService.SaveTimeTopSetting(setting);

        // 手动覆盖接管后：组圆点消失，表圆点移到实际生效的表
        UpdateScheduleListActivation(_groupManager.GetEffectiveScheduleId() ?? "");
        RefreshGroups();
    }

    /// <summary>
    /// 热重载当前生效的计划表（表组切换后同步调度用；今天无生效表时跳过）
    /// </summary>
    public async Task<(bool success, string? error)> ReloadEffectiveScheduleAsync()
    {
        var effectiveId = _groupManager.GetEffectiveScheduleId();
        if (string.IsNullOrEmpty(effectiveId))
            return (true, null);

        return await HotReloadScheduleAsync(effectiveId);
    }

    /// <summary>
    /// 当前激活的表组 ID（"加载表组"弹窗预选用）
    /// </summary>
    public string? GetActiveGroupId() => _settingsService.GetTimeTopSetting().Schedule.ActiveGroupId;

    public bool TryAutoSaveAllBeforeLeave()
    {
        bool allSuccess = true;

        foreach (var state in _editingStates.Values)
        {
            if (!state.HasUnpersistedChanges) continue;

            var savedSchedule = _scheduleManager.LoadSchedule(state.ScheduleId);
            if (savedSchedule == null) { allSuccess = false; continue; }

            ValidateState(state);

            if (!state.HasValidationErrors)
            {
                try
                {
                    var schedule = BuildScheduleFromState(state);
                    _scheduleManager.SaveSchedule(schedule);
                    state.MarkAsSaved();
                }
                catch
                {
                    allSuccess = false;
                }
            }
            else
            {
                allSuccess = false;
            }
        }

        UpdateHasUnpersistedChanges();
        return allSuccess;
    }

    public void ForceSaveAll()
    {
        foreach (var state in _editingStates.Values)
        {
            if (!state.HasUnpersistedChanges) continue;

            try
            {
                var schedule = BuildScheduleFromState(state);
                _scheduleManager.SaveSchedule(schedule);
                state.MarkAsSaved();
            }
            catch
            {
            }
        }

        UpdateHasUnpersistedChanges();
    }

    public void DiscardAllUnpersistedChanges()
    {
        var idsToDiscard = _editingStates.Keys.ToList();

        foreach (var id in idsToDiscard)
        {
            var schedule = _scheduleManager.LoadSchedule(id);
            if (schedule != null)
            {
                _editingStates[id].LoadFromSchedule(schedule);
            }
            else
            {
                _editingStates.Remove(id);
            }
        }

        UpdateHasUnpersistedChanges();
    }

    private void ValidateState(ScheduleEditingState state)
    {
        var items = state.Items;

        foreach (var item in items)
        {
            item.StartTimeError = "";
            item.EndTimeError = "";
        }

        var segments = items.Where(i => i.ItemType == ScheduleItemType.Segment).ToList();
        var timePoints = items.Where(i => i.ItemType == ScheduleItemType.TimePoint).ToList();

        foreach (var seg in segments)
        {
            if (string.IsNullOrEmpty(seg.StartTime))
                seg.StartTimeError = "不能为空";
            else if (!TimeFormatValidator.IsValidFormat(seg.StartTime))
                seg.StartTimeError = "格式应为 HH:mm:ss";

            if (string.IsNullOrEmpty(seg.EndTime))
                seg.EndTimeError = "不能为空";
            else if (!TimeFormatValidator.IsValidFormat(seg.EndTime))
                seg.EndTimeError = "格式应为 HH:mm:ss";

            if (string.IsNullOrEmpty(seg.StartTimeError) && string.IsNullOrEmpty(seg.EndTimeError)
                && !string.IsNullOrEmpty(seg.StartTime) && !string.IsNullOrEmpty(seg.EndTime))
            {
                try
                {
                    if (TimeSpan.Parse(seg.EndTime) < TimeSpan.Parse(seg.StartTime))
                        seg.EndTimeError = "结束时间不能早于开始时间";
                }
                catch { }
            }
        }

        foreach (var tp in timePoints)
        {
            if (string.IsNullOrEmpty(tp.StartTime))
                tp.StartTimeError = "不能为空";
            else if (!TimeFormatValidator.IsValidFormat(tp.StartTime))
                tp.StartTimeError = "格式应为 HH:mm:ss";
        }

        state.ValidationErrors.Clear();
        foreach (var item in items)
        {
            if (item.HasStartTimeError)
                state.ValidationErrors.Add($"[{item.Name}] {item.StartTimeError}");
            if (item.HasEndTimeError)
                state.ValidationErrors.Add($"[{item.Name}] {item.EndTimeError}");
        }
    }

    #endregion

    #region 表组管理

    public void RefreshGroups()
    {
        var selectedGroupId = SelectedGroup?.Id;

        Groups.Clear();

        var groups = _groupManager.LoadAllGroups();
        var setting = _settingsService.GetTimeTopSetting();
        var currentActiveGroupId = setting.Schedule.ActiveGroupId;
        // 手动指定（单独启用某张表）生效时，实际生效的不再是组的轮换计划；
        // 过期的"仅当天临时启用"不算接管，组圆点恢复点亮
        var overrideTakingOver = setting.Schedule.EffectiveManual != null;

        foreach (var group in groups.OrderBy(g => g.Id == ScheduleGroup.DefaultGroupId ? 0 : 1).ThenBy(g => g.Metadata.CreatedAt))
        {
            // 组内表数量：基础映射 + 轮转映射 + 日期覆盖，去重统计（忽略未配置的空值）
            var memberCount = group.DayScheduleMap.Values
                .Concat(group.RotatedDayScheduleMaps.Values.SelectMany(m => m.Values))
                .Concat(group.DateOverrides.Values)
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct()
                .Count();

            Groups.Add(new ScheduleGroupListItem
            {
                Id = group.Id,
                Name = group.Metadata.Name,
                Description = group.Metadata.Description,
                RotationCycleCount = group.RotationCycleCount,
                MemberCount = memberCount,
                // 生效圆点：该组为激活组，且未被"单独启用的表"（手动覆盖）接管
                IsActivated = group.Id == currentActiveGroupId && !overrideTakingOver,
                // 非轮换组不显示轮换信息；轮换组显示 "· 第N/M周"
                RotationInfo = group.RotationCycleCount > 1
                    ? $"· {_groupManager.GetRotationInfo(group.Id)}"
                    : "",
                CreatedAt = DateTime.TryParse(group.Metadata.CreatedAt, out var created) ? created : null,
                UpdatedAt = DateTime.TryParse(group.Metadata.UpdatedAt, out var updated) ? updated : null
            });
        }

        // 保持选中组（清空列表会重置选中项，重建后按ID恢复，避免保存后编辑页状态丢失）
        if (selectedGroupId != null)
        {
            SelectedGroup = Groups.FirstOrDefault(g => g.Id == selectedGroupId);
        }

        RefreshLoadState();
    }

    [RelayCommand]
    private async Task AddGroupAsync()
    {
        if (CreateGroupNameRequested == null) return;

        var newName = await CreateGroupNameRequested("新计划表组");
        if (string.IsNullOrWhiteSpace(newName)) return;

        var newId = $"group_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..4]}";
        var group = _groupManager.CreateNewGroup(newId, newName);

        if (group != null)
        {
            _logger.LogInformation("创建新表组: {GroupId}", newId);
            RefreshGroups();
        }
    }

    [RelayCommand]
    private void DisbandGroup(string? groupId)
    {
        if (string.IsNullOrEmpty(groupId) || groupId == ScheduleGroup.DefaultGroupId) return;

        // 如果解散的是当前激活的组，取消激活
        var setting = _settingsService.GetTimeTopSetting();
        if (setting.Schedule.ActiveGroupId == groupId)
        {
            setting.Schedule.ActiveGroupId = null;
            _settingsService.SaveTimeTopSetting(setting);
        }

        _groupManager.DisbandGroup(groupId);
        RefreshGroups();
        UpdateScheduleListActivation(_groupManager.GetEffectiveScheduleId() ?? "");
        ToastRequested?.Invoke(new ToastMessage("组已解散", "组内计划表已移至默认组") { Severity = ToastSeverity.Success, Duration = TimeSpan.FromSeconds(2) });
    }

    [RelayCommand]
    private void ActivateGroupById(string? groupId)
    {
        if (string.IsNullOrEmpty(groupId)) return;

        var setting = _settingsService.GetTimeTopSetting();
        // 激活组 = 按轮换执行：清掉手动指定的计划表
        setting.Schedule.ActivateGroup(groupId);
        _settingsService.SaveTimeTopSetting(setting);

        RefreshGroups();
        UpdateScheduleListActivation(_groupManager.GetEffectiveScheduleId() ?? "");

        var groupName = Groups.FirstOrDefault(g => g.Id == groupId)?.Name ?? groupId;
        ToastRequested?.Invoke(new ToastMessage("表组已激活", $"已激活表组 \"{groupName}\" 的轮换计划") { Severity = ToastSeverity.Success, Duration = TimeSpan.FromSeconds(2) });
    }

    /// <summary>
    /// 获取计划表当前的启用状态
    /// </summary>
    public bool GetScheduleRule(string scheduleId)
    {
        var schedule = _scheduleManager.LoadSchedule(scheduleId);
        if (schedule?.Settings?.Metadata == null)
            return true;

        return schedule.Settings.Metadata.IsEnabled;
    }

    /// <summary>
    /// 获取所有可用组（用于信息对话框的 ComboBox）
    /// </summary>
    public List<ScheduleGroupListItem> GetAvailableGroups() => Groups.ToList();

    /// <summary>
    /// 判断组是否受保护（默认组不可删除/重命名）
    /// </summary>
    public bool IsGroupProtected(string? groupId) => groupId == ScheduleGroup.DefaultGroupId;

    #region 组编辑

    /// <summary>
    /// 获取当前选中组的完整数据
    /// </summary>
    public ScheduleGroup? GetSelectedGroupData()
    {
        if (SelectedGroup == null) return null;
        return _groupManager.LoadGroup(SelectedGroup.Id);
    }

    /// <summary>
    /// 一次性保存表组编辑抽屉中的全部数据（属性、轮换配置、天→表映射、轮转覆盖、日期覆盖）
    /// 只落盘一次并只刷新一次列表，避免防抖保存时的重复刷新
    /// </summary>
    public void SaveGroupEdits(
        string groupId,
        string name,
        string? description,
        int rotationCycleCount,
        string? rotationStartDate,
        int rotationOffset,
        Dictionary<string, string> dayScheduleMap,
        Dictionary<string, Dictionary<string, string>> rotatedDayScheduleMaps,
        Dictionary<string, string> dateOverrides)
    {
        var group = _groupManager.LoadGroup(groupId);
        if (group == null) return;

        group.Metadata.Name = name;
        group.Metadata.Description = description;
        group.RotationCycleCount = Math.Clamp(rotationCycleCount, 1, 9);
        group.RotationStartDate = rotationStartDate;
        group.RotationOffset = rotationOffset;
        group.DayScheduleMap = dayScheduleMap;
        group.RotatedDayScheduleMaps = rotatedDayScheduleMaps;
        group.DateOverrides = dateOverrides;

        _groupManager.SaveGroup(group);
        RefreshGroups();
    }

    /// <summary>
    /// 获取所有可用计划表（用于天→表映射的 ComboBox）
    /// </summary>
    public List<ScheduleListItem> GetAvailableSchedules() => Schedules.ToList();

    /// <summary>
    /// 获取组在今天所处的轮换周序号（1=基础周, 2..N=轮转周），用于周切换器标记"本周"
    /// </summary>
    public int GetCurrentRotationWeek(ScheduleGroup group) => _groupManager.GetCurrentRotationWeek(group);

    #endregion

    #endregion
}