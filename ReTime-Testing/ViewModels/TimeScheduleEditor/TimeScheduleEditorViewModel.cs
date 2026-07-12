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

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

public partial class TimeScheduleEditorViewModel : ObservableObject
{
    private readonly ITimeScheduleManager _scheduleManager;
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

    [ObservableProperty]
    private bool _hasUnpersistedChanges = false;

    public bool HasAnyUnpersistedChanges => _editingStates.Values.Any(s => s.HasUnpersistedChanges);

    [ObservableProperty]
    private ScheduleListItem? _selectedSchedule;

    [ObservableProperty]
    private ScheduleItemListItem? _selectedScheduleItem;

    [ObservableProperty]
    private bool _canUndo = false;

    [ObservableProperty]
    private bool _canRedo = false;

    public ObservableCollection<ScheduleListItem> Schedules { get; } = new();
    public ObservableCollection<ScheduleItemListItem> ScheduleItems => _currentEditingState?.Items ?? _emptyItems;

    private readonly ObservableCollection<ScheduleItemListItem> _emptyItems = new();

    public bool IsSegmentSelected => SelectedScheduleItem != null && SelectedScheduleItem.ItemType == ScheduleItemType.Segment;
    public bool IsTimePointSelected => SelectedScheduleItem != null && SelectedScheduleItem.ItemType == ScheduleItemType.TimePoint;
    public bool HasScheduleItems => _currentEditingState != null && _currentEditingState.Items.Count > 0;

    public Array ToStateOptions => Enum.GetValues(typeof(ProgressStateType));

    public TimeScheduleEditorViewModel(
        ITimeScheduleManager scheduleManager,
        ISettingsService settingsService,
        ITimeService? timeService = null,
        IScheduleManager? scheduleRunManager = null)
    {
        _scheduleManager = scheduleManager;
        _settingsService = settingsService;
        _timeService = timeService;
        _scheduleRunManager = scheduleRunManager;

        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _autoSaveTimer.Tick += OnAutoSaveTimerTick;

        RefreshScheduleList();
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
            Logger.Info("TimeScheduleEditor", $"创建新计划表: {newId}");
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
            if (setting.Schedule.Override.ScheduleId == deletedId)
            {
                setting.Schedule.Override.ScheduleId = "";
                setting.Schedule.Override.Enabled = false;
                _settingsService.SaveTimeTopSetting(setting);
            }

            _editingStates.Remove(deletedId);

            RefreshScheduleList();
            SelectedSchedule = null;
        }
    }

    [RelayCommand]
    private void ActivateSchedule(string? scheduleId)
    {
        if (string.IsNullOrEmpty(scheduleId)) return;

        var setting = _settingsService.GetTimeTopSetting();
        setting.Schedule.Override.ScheduleId = scheduleId;
        setting.Schedule.Override.Enabled = true;
        _settingsService.SaveTimeTopSetting(setting);

        UpdateScheduleListActivation(scheduleId);
    }

    [RelayCommand]
    private void EditScheduleInfo()
    {
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

    [RelayCommand]
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

            Logger.Info("TimeScheduleEditor", $"计划表保存成功: {_currentEditingState.ScheduleId}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("TimeScheduleEditor", $"计划表保存失败: {_currentEditingState.ScheduleId}, 错误: {ex.Message}", ex);
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
                var convertedPoint = ScheduleItemConverter.ToTimePoint(item);

                if (existingIndex >= 0)
                {
                    schedule.TimePoints[existingIndex] = convertedPoint;
                }
                else
                {
                    schedule.TimePoints.Add(convertedPoint);
                }
            }
            else
            {
                var existingIndex = schedule.Schedules.FindIndex(s => s.Id == item.Id);
                var convertedSegment = ScheduleItemConverter.ToScheduleItem(item);

                ApplySegmentStyles(convertedSegment, item);

                if (existingIndex >= 0)
                {
                    schedule.Schedules[existingIndex] = convertedSegment;
                }
                else
                {
                    schedule.Schedules.Add(convertedSegment);
                }
            }
        }

        return schedule;
    }

    private static void ApplySegmentStyles(TimeScheduleItem segment, ScheduleItemListItem item)
    {
        segment.Styles ??= new StyleOverridesData();

        if (item.HasCustomStyle)
        {
            segment.Styles.Enabled = true;
            segment.Styles.ForegroundColor = $"#{item.ForegroundR:X2}{item.ForegroundG:X2}{item.ForegroundB:X2}";
            segment.Styles.BackgroundColor = item.HasBackgroundColor
                ? $"#{item.BackgroundR:X2}{item.BackgroundG:X2}{item.BackgroundB:X2}"
                : null;
            segment.Styles.Opacity = item.Opacity / 100.0;
        }
        else
        {
            segment.Styles.Enabled = false;
        }
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
        var currentSelectedId = _settingsService.GetTimeTopSetting().Schedule.Override.ScheduleId;

        var sortedList = scheduleList
            .OrderBy(i => i.CreatedAt ?? DateTime.MaxValue)
            .ToList();

        foreach (var info in sortedList)
        {
            Schedules.Add(new ScheduleListItem
            {
                Id = info.Id,
                Name = info.Name,
                IsActivated = info.Id == currentSelectedId,
                CreatedAt = info.CreatedAt,
                UpdatedAt = info.UpdatedAt
            });
        }
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
                    Logger.Warn("TimeScheduleEditor", $"时间格式解析失败: {seg.StartTime}/{seg.EndTime}, 错误: {ex.Message}");
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
        var currentSelectedId = _settingsService.GetTimeTopSetting().Schedule.Override.ScheduleId;

        return scheduleList
            .OrderBy(i => i.CreatedAt ?? DateTime.MaxValue)
            .Select(s => new ScheduleListItem
            {
                Id = s.Id,
                Name = s.Name,
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

            var planGenerator = new ExecutionPlanGenerator();
            var now = _timeService.GetCurrentTime();
            var newPlan = planGenerator.Generate(schedule, now.Date, now);
            _scheduleRunManager.RegenerateExecutionPlan(newPlan);

            _timeService.Calibrate(_timeService.GetCurrentTime(), TimeJumpReason.ManualCalibration, TimeJumpSeverity.Minor);

            Logger.Info("TimeScheduleEditor", $"热重载成功: {scheduleId}");
            return (true, null);
        }
        catch (Exception ex)
        {
            Logger.Error("TimeScheduleEditor", $"热重载失败: {ex.Message}", ex);
            return (false, ex.Message);
        }
    }

    public void ApplyScheduleSelection(ScheduleListItem selectedItem)
    {
        var setting = _settingsService.GetTimeTopSetting();
        setting.Schedule.Override.ScheduleId = selectedItem.Id;
        setting.Schedule.Override.Enabled = true;
        _settingsService.SaveTimeTopSetting(setting);
    }

    public bool TryAutoSaveBeforeLeave()
    {
        if (_currentEditingState == null) return true;

        if (!_currentEditingState.HasUnpersistedChanges) return true;

        ValidateAllItems();

        if (!_currentEditingState.HasValidationErrors)
        {
            return PerformSave(force: false);
        }

        return false;
    }

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

    public void DiscardUnpersistedChanges()
    {
        if (_currentEditingState == null) return;

        var schedule = _scheduleManager.LoadSchedule(_currentEditingState.ScheduleId);
        if (schedule != null)
        {
            _currentEditingState.LoadFromSchedule(schedule);
        }

        UpdateHasUnpersistedChanges();
        UpdateUndoRedoState();
        UpdateScheduleItemsBinding();
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
}