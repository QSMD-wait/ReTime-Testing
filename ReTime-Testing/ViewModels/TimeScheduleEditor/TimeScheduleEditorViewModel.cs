using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 时间计划表编辑器 ViewModel
/// </summary>
public partial class TimeScheduleEditorViewModel : ObservableObject
{
    private readonly ITimeScheduleManager _scheduleManager;
    private readonly ISettingsService _settingsService;
    private readonly ITimeService? _timeService;
    private readonly IScheduleManager? _scheduleRunManager;

    private ScheduleItemListItem? _previousSelectedItem;

    private TimeSchedule? _currentSchedule;

    [ObservableProperty]
    private bool _hasUnsavedChanges = false;

    [ObservableProperty]
    private bool _isValidationInfoBarOpen = false;

    [ObservableProperty]
    private string _validationInfoBarMessage = "";

    [ObservableProperty]
    private ScheduleListItem? _selectedSchedule;

    [ObservableProperty]
    private ScheduleItemListItem? _selectedScheduleItem;

    public ObservableCollection<ScheduleListItem> Schedules { get; } = new();
    public ObservableCollection<ScheduleItemListItem> ScheduleItems { get; } = new();

    public bool IsSegmentSelected => SelectedScheduleItem != null && SelectedScheduleItem.ItemType == ScheduleItemType.Segment;
    public bool IsTimePointSelected => SelectedScheduleItem != null && SelectedScheduleItem.ItemType == ScheduleItemType.TimePoint;
    public bool HasScheduleItems => ScheduleItems.Count > 0;

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

        RefreshScheduleList();
        HasUnsavedChanges = false;
    }

    partial void OnSelectedScheduleChanged(ScheduleListItem? value)
    {
        if (value != null)
        {
            _currentSchedule = _scheduleManager.LoadSchedule(value.Id);
        }
        else
        {
            _currentSchedule = null;
        }
        LoadScheduleItems();
        OnPropertyChanged(nameof(HasScheduleItems));
    }

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
        HasUnsavedChanges = true;
        ValidateAllItems();
    }

    #region 计划表操作命令

    [RelayCommand]
    private void AddSchedule()
    {
        var newId = $"schedule_{DateTime.Now:yyyyMMddHHmmss}";
        var newName = "新计划表";

        var schedule = _scheduleManager.CreateNewSchedule(newId, newName);
        if (schedule != null)
        {
            Logger.Info("TimeScheduleEditor", $"创建新计划表: {newId}");
            HasUnsavedChanges = true;
            RefreshScheduleList();
            SelectedSchedule = Schedules.FirstOrDefault(s => s.Id == newId);
        }
    }

    [RelayCommand]
    private void CopySchedule()
    {
        if (SelectedSchedule == null) return;

        var newId = $"schedule_{DateTime.Now:yyyyMMddHHmmss}";
        var newSchedule = _scheduleManager.CopySchedule(SelectedSchedule.Id, newId);

        if (newSchedule != null)
        {
            HasUnsavedChanges = true;
            RefreshScheduleList();
            SelectedSchedule = Schedules.FirstOrDefault(s => s.Id == newId);
        }
    }

    [RelayCommand]
    private void DeleteSchedule()
    {
        if (SelectedSchedule == null) return;
        if (SelectedSchedule.Id == "Default") return;

        if (_scheduleManager.DeleteSchedule(SelectedSchedule.Id))
        {
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
        if (_currentSchedule == null) return;

        ComputeDefaultTime(isTimePoint: false, out var startTime, out var endTime);

        var newSegment = new ScheduleItemListItem
        {
            Id = $"segment_{DateTime.Now:yyyyMMddHHmmss}",
            Name = "新时间段",
            StartTime = startTime,
            EndTime = endTime,
            ItemType = ScheduleItemType.Segment
        };

        ScheduleItems.Add(newSegment);
        HasUnsavedChanges = true;
        SelectedScheduleItem = newSegment;
        OnPropertyChanged(nameof(HasScheduleItems));
    }

    [RelayCommand]
    private void AddTimePoint()
    {
        if (_currentSchedule == null) return;

        ComputeDefaultTime(isTimePoint: true, out var startTime, out _);

        var newTimePoint = new ScheduleItemListItem
        {
            Id = $"tp_{DateTime.Now:yyyyMMddHHmmss}",
            Name = "新时间点",
            StartTime = startTime,
            ItemType = ScheduleItemType.TimePoint,
            ToState = ProgressStateType.Success
        };

        ScheduleItems.Add(newTimePoint);
        HasUnsavedChanges = true;
        SelectedScheduleItem = newTimePoint;
        OnPropertyChanged(nameof(HasScheduleItems));
    }

    [RelayCommand]
    private void DeleteScheduleItem()
    {
        if (_currentSchedule == null || SelectedScheduleItem == null) return;

        ScheduleItems.Remove(SelectedScheduleItem);
        SelectedScheduleItem = null;
        HasUnsavedChanges = true;
        OnPropertyChanged(nameof(HasScheduleItems));
    }

    [RelayCommand]
    private void RefreshOrder()
    {
        var sortedItems = ScheduleItems
            .Where(i => TryParseTime(i.StartTime, out _))
            .OrderBy(i => TimeSpan.Parse(i.StartTime))
            .ToList();

        bool needsReorder = false;
        for (int i = 0; i < sortedItems.Count; i++)
        {
            if (ScheduleItems[i].Id != sortedItems[i].Id)
            {
                needsReorder = true;
                break;
            }
        }

        if (needsReorder)
        {
            ScheduleItems.Clear();
            foreach (var item in sortedItems)
            {
                ScheduleItems.Add(item);
            }
            HasUnsavedChanges = true;
        }

        ValidateAllItems();
    }

    [RelayCommand]
    private void Save()
    {
        if (ValidateAndSave())
        {
            HasUnsavedChanges = false;
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

    private void LoadScheduleItems()
    {
        ScheduleItems.Clear();

        if (_currentSchedule == null) return;

        var items = new List<ScheduleItemListItem>();

        if (_currentSchedule.Schedules != null)
        {
            foreach (var item in _currentSchedule.Schedules)
            {
                items.Add(ScheduleItemConverter.ToListItem(item));
            }
        }

        if (_currentSchedule.TimePoints != null)
        {
            foreach (var point in _currentSchedule.TimePoints)
            {
                items.Add(ScheduleItemConverter.ToListItem(point));
            }
        }

        var sortedItems = items
            .Where(i => TryParseTime(i.StartTime, out _))
            .OrderBy(i => TimeSpan.Parse(i.StartTime))
            .ToList();
        foreach (var item in sortedItems)
        {
            ScheduleItems.Add(item);
        }

        ValidateAllItems();
        OnPropertyChanged(nameof(HasScheduleItems));
    }

    #endregion

    #region 验证

    public void ValidateAllItems()
    {
        foreach (var item in ScheduleItems)
        {
            item.StartTimeError = "";
            item.EndTimeError = "";
        }

        var segments = ScheduleItems.Where(i => i.ItemType == ScheduleItemType.Segment).ToList();
        var timePoints = ScheduleItems.Where(i => i.ItemType == ScheduleItemType.TimePoint).ToList();

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
    }

    private bool TryParseTime(string timeString, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        if (string.IsNullOrEmpty(timeString)) return false;
        return TimeSpan.TryParse(timeString, out result);
    }

    #endregion

    #region 保存

    public bool ValidateAndSave()
    {
        if (_currentSchedule == null) return false;

        ValidateAllItems();

        bool hasErrors = ScheduleItems.Any(i => i.HasStartTimeError || i.HasEndTimeError);
        if (hasErrors)
        {
            ValidationInfoBarMessage = "存在验证错误，请修正后再保存";
            IsValidationInfoBarOpen = true;
            return false;
        }

        if (SelectedSchedule != null && !string.IsNullOrEmpty(SelectedSchedule.Id))
        {
            _currentSchedule.Id = SelectedSchedule.Id;
        }

        _currentSchedule.Schedules ??= new List<TimeScheduleItem>();
        _currentSchedule.TimePoints ??= new List<CustomTimePoint>();

        var currentItemIds = ScheduleItems.Select(i => i.Id).ToHashSet();

        _currentSchedule.Schedules?.RemoveAll(s => !currentItemIds.Contains(s.Id));
        _currentSchedule.TimePoints?.RemoveAll(t => !currentItemIds.Contains(t.Id));

        foreach (var item in ScheduleItems)
        {
            if (item.ItemType == ScheduleItemType.TimePoint)
            {
                var existingPoint = _currentSchedule.TimePoints?.FirstOrDefault(t => t.Id == item.Id);
                if (existingPoint != null)
                {
                    existingPoint.Name = item.Name;
                    existingPoint.Time = item.StartTime;
                    existingPoint.Type = TimePointType.StateChange;
                    existingPoint.StateChange ??= new StateChangeData();
                    existingPoint.StateChange.ToState = item.ToState;

                    if (item.HasCustomStyle)
                    {
                        existingPoint.Type = TimePointType.StyleChange;
                        existingPoint.StyleChange ??= new StyleChangeData();
                        existingPoint.StyleChange.ForegroundColor = $"#{item.ForegroundR:X2}{item.ForegroundG:X2}{item.ForegroundB:X2}";
                        existingPoint.StyleChange.BackgroundColor = item.HasBackgroundColor ? $"#{item.BackgroundR:X2}{item.BackgroundG:X2}{item.BackgroundB:X2}" : null;
                        existingPoint.StyleChange.Opacity = item.Opacity / 100.0;
                    }
                    else
                    {
                        existingPoint.Type = TimePointType.StateChange;
                        existingPoint.StyleChange = null;
                    }
                }
                else
                {
                    var newPoint = new CustomTimePoint
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Time = item.StartTime,
                        Type = TimePointType.StateChange,
                        StateChange = new StateChangeData
                        {
                            ToState = item.ToState
                        }
                    };

                    if (item.HasCustomStyle)
                    {
                        newPoint.Type = TimePointType.StyleChange;
                        newPoint.StyleChange = new StyleChangeData
                        {
                            ForegroundColor = $"#{item.ForegroundR:X2}{item.ForegroundG:X2}{item.ForegroundB:X2}",
                            BackgroundColor = item.HasBackgroundColor ? $"#{item.BackgroundR:X2}{item.BackgroundG:X2}{item.BackgroundB:X2}" : null,
                            Opacity = item.Opacity / 100.0
                        };
                    }

                    _currentSchedule.TimePoints?.Add(newPoint);
                }
            }
            else
            {
                var existingSegment = _currentSchedule.Schedules?.FirstOrDefault(s => s.Id == item.Id);
                if (existingSegment != null)
                {
                    existingSegment.Name = item.Name;
                    existingSegment.StartTime = item.StartTime;
                    existingSegment.EndTime = item.EndTime;
                    if (item.HasCustomStyle)
                    {
                        existingSegment.Styles ??= new StyleOverridesData();
                        existingSegment.Styles.Enabled = true;
                        existingSegment.Styles.ForegroundColor = $"#{item.ForegroundR:X2}{item.ForegroundG:X2}{item.ForegroundB:X2}";
                        existingSegment.Styles.BackgroundColor = item.HasBackgroundColor
                            ? $"#{item.BackgroundR:X2}{item.BackgroundG:X2}{item.BackgroundB:X2}"
                            : null;
                        existingSegment.Styles.Opacity = item.Opacity / 100.0;
                    }
                    else
                    {
                        existingSegment.Styles = null;
                    }
                }
                else
                {
                    _currentSchedule.Schedules?.Add(new TimeScheduleItem
                    {
                        Id = item.Id,
                        Name = item.Name,
                        StartTime = item.StartTime,
                        EndTime = item.EndTime
                    });
                }
            }
        }

        var validator = new TimeScheduleValidator();
        var result = validator.Validate(_currentSchedule);
        if (!result.IsValid)
        {
            ValidationInfoBarMessage = string.Join("\n", result.Errors);
            IsValidationInfoBarOpen = true;
            return false;
        }

        try
        {
            _scheduleManager.SaveSchedule(_currentSchedule);
            Logger.Info("TimeScheduleEditor", $"计划表保存成功: {_currentSchedule.Id}");
            IsValidationInfoBarOpen = false;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("TimeScheduleEditor", $"计划表保存失败: {_currentSchedule.Id}, 错误: {ex.Message}", ex);
            ValidationInfoBarMessage = $"保存失败: {ex.Message}";
            IsValidationInfoBarOpen = true;
            return false;
        }
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

        var items = new List<ScheduleListItem>();
        foreach (var info in scheduleList)
        {
            items.Add(new ScheduleListItem
            {
                Id = info.Id,
                Name = info.Name,
                IsActivated = info.Id == currentSelectedId
            });
        }
        return items;
    }

    public async Task HotReloadScheduleAsync(string scheduleId)
    {
        try
        {
            if (_timeService == null || _scheduleRunManager == null)
            {
                Logger.Warn("TimeScheduleEditor", "时间服务或调度管理器未初始化");
                return;
            }

            var newSchedule = _scheduleManager.LoadSchedule(scheduleId);
            if (newSchedule == null)
            {
                Logger.Warn("TimeScheduleEditor", $"加载计划表失败: {scheduleId}");
                return;
            }

            var planGenerator = new ExecutionPlanGenerator();
            var currentTime = _timeService.GetCurrentTime();
            var newPlan = planGenerator.GenerateSafe(newSchedule, DateTime.Today, currentTime);

            if (newPlan == null)
            {
                Logger.Warn("TimeScheduleEditor", "时间计划配置无效");
                return;
            }

            _scheduleRunManager.RegenerateExecutionPlan(newPlan);
            UpdateScheduleListActivation(scheduleId);

            Logger.Info("TimeScheduleEditor", $"热重载成功: {scheduleId}");
        }
        catch (Exception ex)
        {
            Logger.Error("TimeScheduleEditor", $"热重载失败: {ex.Message}", ex);
        }
    }

    public void ApplyScheduleSelection(ScheduleListItem selectedItem)
    {
        var setting = _settingsService.GetTimeTopSetting();
        setting.Schedule.Override.ScheduleId = selectedItem.Id;
        setting.Schedule.Override.Enabled = true;
        _settingsService.SaveTimeTopSetting(setting);
    }

    #endregion
}