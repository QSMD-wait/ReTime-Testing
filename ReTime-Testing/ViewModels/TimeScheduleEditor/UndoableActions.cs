using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReTime_Testing.ViewModels.TimeScheduleEditor;

/// <summary>
/// 可撤销操作的接口
/// </summary>
public interface IUndoableAction
{
    void Execute(ObservableCollection<ScheduleItemListItem> items);
    void Undo(ObservableCollection<ScheduleItemListItem> items);
}

/// <summary>
/// 添加项操作
/// </summary>
public class AddItemAction : IUndoableAction
{
    private readonly ScheduleItemListItem _item;

    public AddItemAction(ScheduleItemListItem item)
    {
        _item = item;
    }

    public void Execute(ObservableCollection<ScheduleItemListItem> items)
    {
        items.Add(_item);
    }

    public void Undo(ObservableCollection<ScheduleItemListItem> items)
    {
        var index = items.IndexOf(_item);
        if (index >= 0)
        {
            items.RemoveAt(index);
        }
        else
        {
            var match = items.FirstOrDefault(i => i.Id == _item.Id);
            if (match != null)
            {
                items.Remove(match);
            }
        }
    }
}

/// <summary>
/// 删除项操作
/// </summary>
public class RemoveItemAction : IUndoableAction
{
    private readonly ScheduleItemListItem _item;
    private readonly int _index;

    public RemoveItemAction(ScheduleItemListItem item, int index)
    {
        _item = item;
        _index = index;
    }

    public void Execute(ObservableCollection<ScheduleItemListItem> items)
    {
        items.Remove(_item);
    }

    public void Undo(ObservableCollection<ScheduleItemListItem> items)
    {
        var insertIndex = Math.Min(_index, items.Count);
        items.Insert(insertIndex, _item);
    }
}

/// <summary>
/// 修改项属性操作
/// </summary>
public class ModifyItemAction : IUndoableAction
{
    private readonly string _itemId;
    private readonly Action<ScheduleItemListItem> _applyNew;
    private readonly Action<ScheduleItemListItem> _applyOld;

    public ModifyItemAction(string itemId, Action<ScheduleItemListItem> applyNew, Action<ScheduleItemListItem> applyOld)
    {
        _itemId = itemId;
        _applyNew = applyNew;
        _applyOld = applyOld;
    }

    public void Execute(ObservableCollection<ScheduleItemListItem> items)
    {
        var item = items.FirstOrDefault(i => i.Id == _itemId);
        if (item != null) _applyNew(item);
    }

    public void Undo(ObservableCollection<ScheduleItemListItem> items)
    {
        var item = items.FirstOrDefault(i => i.Id == _itemId);
        if (item != null) _applyOld(item);
    }
}

/// <summary>
/// 重排序操作
/// </summary>
public class ReorderAction : IUndoableAction
{
    private readonly int _oldIndex;
    private readonly int _newIndex;

    public ReorderAction(int oldIndex, int newIndex)
    {
        _oldIndex = oldIndex;
        _newIndex = newIndex;
    }

    public void Execute(ObservableCollection<ScheduleItemListItem> items)
    {
        if (_newIndex < 0 || _newIndex >= items.Count) return;
        var item = items[_oldIndex];
        items.RemoveAt(_oldIndex);
        items.Insert(_newIndex, item);
    }

    public void Undo(ObservableCollection<ScheduleItemListItem> items)
    {
        if (_oldIndex < 0 || _oldIndex >= items.Count + 1) return;
        var item = items[_newIndex];
        items.RemoveAt(_newIndex);
        items.Insert(_oldIndex, item);
    }
}

/// <summary>
/// 批量重排序操作（排序后整体替换）
/// </summary>
public class SortAllAction : IUndoableAction
{
    private readonly string[] _oldOrder;
    private readonly string[] _newOrder;

    public SortAllAction(ObservableCollection<ScheduleItemListItem> items, string[] newOrderIds)
    {
        _oldOrder = items.Select(i => i.Id).ToArray();
        _newOrder = newOrderIds;
    }

    public void Execute(ObservableCollection<ScheduleItemListItem> items)
    {
        ApplyOrder(items, _newOrder);
    }

    public void Undo(ObservableCollection<ScheduleItemListItem> items)
    {
        ApplyOrder(items, _oldOrder);
    }

    private static void ApplyOrder(ObservableCollection<ScheduleItemListItem> items, string[] order)
    {
        var lookup = items.ToDictionary(i => i.Id);
        var reordered = order
            .Where(id => lookup.ContainsKey(id))
            .Select(id => lookup[id])
            .ToList();

        items.Clear();
        foreach (var item in reordered)
        {
            items.Add(item);
        }
    }
}