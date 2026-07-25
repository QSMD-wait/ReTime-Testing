using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ReTime_Testing.Models;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 单个计划表的编辑状态
/// 维护该计划表的编辑数据、撤销/重做栈、验证状态等
/// </summary>
public class ScheduleEditingState
{
    public string ScheduleId { get; }

    public ObservableCollection<ScheduleItemListItem> Items { get; } = new();

    public int SelectedItemIndex { get; set; } = -1;

    public List<string> ValidationErrors { get; } = new();

    public bool HasValidationErrors => ValidationErrors.Count > 0;

    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    private List<ScheduleItemListItem>? _lastSavedSnapshot;

    public bool HasUnpersistedChanges
    {
        get
        {
            if (_lastSavedSnapshot == null) return Items.Count > 0;
            return !SnapshotsEqual(_lastSavedSnapshot, Items);
        }
    }

    public ScheduleEditingState(string scheduleId)
    {
        ScheduleId = scheduleId;
    }

    public void LoadFromSchedule(TimeSchedule schedule)
    {
        Items.Clear();

        var listItems = new List<ScheduleItemListItem>();

        if (schedule.Schedules != null)
        {
            foreach (var item in schedule.Schedules)
            {
                listItems.Add(ScheduleItemConverter.ToListItem(item));
            }
        }

        if (schedule.TimePoints != null)
        {
            foreach (var point in schedule.TimePoints)
            {
                listItems.Add(ScheduleItemConverter.ToListItem(point));
            }
        }

        var sortedItems = listItems
            .Where(i => TimeFormatValidator.IsValidFormat(i.StartTime))
            .OrderBy(i => TimeSpan.Parse(i.StartTime))
            .ToList();

        foreach (var item in sortedItems)
        {
            Items.Add(item);
        }

        _lastSavedSnapshot = TakeSnapshot();
        ClearUndoRedoStacks();
        ValidationErrors.Clear();
    }

    public void MarkAsSaved()
    {
        _lastSavedSnapshot = TakeSnapshot();
    }

    public void ExecuteAction(IUndoableAction action)
    {
        action.Execute(Items);
        _undoStack.Push(action);
        _redoStack.Clear();
    }

    public bool Undo()
    {
        if (_undoStack.Count == 0) return false;

        var action = _undoStack.Pop();
        action.Undo(Items);
        _redoStack.Push(action);
        return true;
    }

    public bool Redo()
    {
        if (_redoStack.Count == 0) return false;

        var action = _redoStack.Pop();
        action.Execute(Items);
        _undoStack.Push(action);
        return true;
    }

    public void ClearUndoRedoStacks()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    private List<ScheduleItemListItem> TakeSnapshot()
    {
        var snapshot = new List<ScheduleItemListItem>();
        foreach (var item in Items)
        {
            snapshot.Add(CloneItem(item));
        }
        return snapshot;
    }

    private static ScheduleItemListItem CloneItem(ScheduleItemListItem source)
    {
        return new ScheduleItemListItem
        {
            Id = source.Id,
            Name = source.Name,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            TypeIcon = source.TypeIcon,
            ItemType = source.ItemType,
            ToState = source.ToState,
            ForegroundR = source.ForegroundR,
            ForegroundG = source.ForegroundG,
            ForegroundB = source.ForegroundB,
            BackgroundR = source.BackgroundR,
            BackgroundG = source.BackgroundG,
            BackgroundB = source.BackgroundB,
            Opacity = source.Opacity,
            HasStateChange = source.HasStateChange,
            HasStyleChange = source.HasStyleChange,
            HasCustomStyle = source.HasCustomStyle,
            HasBackgroundColor = source.HasBackgroundColor,
            HasBehavior = source.HasBehavior,
            PollingIntervalMs = source.PollingIntervalMs,
            ReverseProgress = source.ReverseProgress,
        };
    }

    private static bool SnapshotsEqual(List<ScheduleItemListItem> a, ObservableCollection<ScheduleItemListItem> b)
    {
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            if (!ItemsEqual(a[i], b[i])) return false;
        }

        return true;
    }

    private static bool ItemsEqual(ScheduleItemListItem a, ScheduleItemListItem b)
    {
        return a.Id == b.Id
            && a.Name == b.Name
            && a.StartTime == b.StartTime
            && a.EndTime == b.EndTime
            && a.ItemType == b.ItemType
            && a.ToState == b.ToState
            && a.ForegroundR == b.ForegroundR
            && a.ForegroundG == b.ForegroundG
            && a.ForegroundB == b.ForegroundB
            && a.BackgroundR == b.BackgroundR
            && a.BackgroundG == b.BackgroundG
            && a.BackgroundB == b.BackgroundB
            && Math.Abs(a.Opacity - b.Opacity) < 0.01
            && a.HasStateChange == b.HasStateChange
            && a.HasStyleChange == b.HasStyleChange
            && a.HasCustomStyle == b.HasCustomStyle
            && a.HasBackgroundColor == b.HasBackgroundColor
            && a.HasBehavior == b.HasBehavior
            && a.PollingIntervalMs == b.PollingIntervalMs
            && a.ReverseProgress == b.ReverseProgress;
    }
}