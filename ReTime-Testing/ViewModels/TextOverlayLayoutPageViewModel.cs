using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using iNKORE.UI.WPF.Modern.Common.IconKeys;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels;

public partial class TextOverlayLayoutPageViewModel : ObservableObject, IDropTarget
{
    private readonly ISettingsService _settingsService;
    private readonly IDesktopWindowManager _desktopWindowManager;
    private Models.TimeTopSetting _setting;
    private bool _isInitializing = true;

    #region 插槽集合

    public ObservableCollection<TextSlotItemViewModel> LeftSlots { get; } = [];
    public ObservableCollection<TextSlotItemViewModel> CenterSlots { get; } = [];
    public ObservableCollection<TextSlotItemViewModel> RightSlots { get; } = [];

    #endregion

    #region 选中状态

    [ObservableProperty]
    private TextSlotItemViewModel? _selectedSlot;

    [ObservableProperty]
    private int _selectedGroupIndex;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private TextSlotItemViewModel? _selectedLeftSlot;

    [ObservableProperty]
    private TextSlotItemViewModel? _selectedCenterSlot;

    [ObservableProperty]
    private TextSlotItemViewModel? _selectedRightSlot;

    partial void OnSelectedLeftSlotChanged(TextSlotItemViewModel? value) => SyncSelectedSlot(value, 0);
    partial void OnSelectedCenterSlotChanged(TextSlotItemViewModel? value) => SyncSelectedSlot(value, 1);
    partial void OnSelectedRightSlotChanged(TextSlotItemViewModel? value) => SyncSelectedSlot(value, 2);

    private void SyncSelectedSlot(TextSlotItemViewModel? value, int groupIndex)
    {
        if (value == null) return;

        if (groupIndex != 0) SelectedLeftSlot = null;
        if (groupIndex != 1) SelectedCenterSlot = null;
        if (groupIndex != 2) SelectedRightSlot = null;

        SelectedSlot = value;
        SelectedGroupIndex = groupIndex;
    }

    partial void OnSelectedSlotChanged(TextSlotItemViewModel? value)
    {
        if (value == null)
        {
            SelectedLeftSlot = null;
            SelectedCenterSlot = null;
            SelectedRightSlot = null;
        }
    }

    #endregion

    #region 组件库搜索与筛选

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _selectedCategory = "全部";

    public string[] Categories => ["全部", "文本类", "时间类", "日期类", "进度类"];

    #endregion

    #region 组件库数据源定义

    public ComponentLibraryItem[] AllComponents =>
    [
        new("自定义文本", "显示固定的自定义文本内容", FluentSystemIcons.TextT_24_Regular, Models.TextSourceType.CustomText, "文本类"),
        new("当前段名", "显示当前时间段的名称", FluentSystemIcons.Tag_24_Regular, Models.TextSourceType.SegmentName, "文本类"),
        new("剩余时间", "显示当前段的剩余时间", FluentSystemIcons.Hourglass_24_Regular, Models.TextSourceType.RemainingTime, "时间类"),
        new("已过时间", "显示当前段已过的时间", FluentSystemIcons.History_24_Regular, Models.TextSourceType.ElapsedTime, "时间类"),
        new("进度百分比", "显示当前段的进度百分比", FluentSystemIcons.DataUsage_24_Regular, Models.TextSourceType.ProgressPercent, "进度类"),
        new("当前时间", "显示当前系统时间", FluentSystemIcons.Clock_24_Regular, Models.TextSourceType.CurrentTime, "时间类"),
        new("下一段名", "显示下一个时间段的名称", FluentSystemIcons.FastForward_24_Regular, Models.TextSourceType.NextSegment, "文本类"),
        new("当前日期", "显示当前日期", FluentSystemIcons.Calendar_24_Regular, Models.TextSourceType.CurrentDate, "日期类"),
        new("当前星期", "显示当前是星期几", FluentSystemIcons.CalendarWeekNumbers_24_Regular, Models.TextSourceType.CurrentDayOfWeek, "日期类"),
    ];

    private ICollectionView? _filteredComponents;

    public ICollectionView FilteredComponents => _filteredComponents ??= CreateFilteredView();

    private ICollectionView CreateFilteredView()
    {
        var view = CollectionViewSource.GetDefaultView(AllComponents);
        view.Filter = FilterComponent;
        return view;
    }

    private bool FilterComponent(object item)
    {
        if (item is not ComponentLibraryItem comp) return false;
        var matchCategory = SelectedCategory == "全部" || comp.Category == SelectedCategory;
        var matchSearch = string.IsNullOrWhiteSpace(SearchText) ||
                          comp.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                          comp.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        return matchCategory && matchSearch;
    }

    partial void OnSearchTextChanged(string value) => _filteredComponents?.Refresh();
    partial void OnSelectedCategoryChanged(string value) => _filteredComponents?.Refresh();

    #endregion

    public TextOverlayLayoutPageViewModel(ISettingsService settingsService, IDesktopWindowManager desktopWindowManager)
    {
        _settingsService = settingsService;
        _desktopWindowManager = desktopWindowManager;
        _setting = _settingsService.GetTimeTopSetting();

        LoadFromConfig();
        _isInitializing = false;
    }

    private void LoadFromConfig()
    {
        var layout = _setting.TextOverlay.Layout;

        LoadSlots(LeftSlots, layout.Left.Slots);
        LoadSlots(CenterSlots, layout.Center.Slots);
        LoadSlots(RightSlots, layout.Right.Slots);
    }

    private void LoadSlots(ObservableCollection<TextSlotItemViewModel> collection, List<Models.TextSlotConfig> slots)
    {
        collection.Clear();
        foreach (var slot in slots)
        {
            collection.Add(new TextSlotItemViewModel(slot, SaveAndRefresh));
        }
    }

    private void SaveAndRefresh()
    {
        if (_isInitializing) return;
        SaveToConfig();
        _settingsService.SaveTimeTopSetting(_setting);
        _desktopWindowManager.RefreshTextOverlay();
    }

    private void SaveToConfig()
    {
        var layout = _setting.TextOverlay.Layout;
        layout.Left.Visible = true;
        layout.Center.Visible = true;
        layout.Right.Visible = true;

        SaveSlots(LeftSlots, layout.Left.Slots);
        SaveSlots(CenterSlots, layout.Center.Slots);
        SaveSlots(RightSlots, layout.Right.Slots);
    }

    private static void SaveSlots(ObservableCollection<TextSlotItemViewModel> collection, List<Models.TextSlotConfig> slots)
    {
        slots.Clear();
        foreach (var vm in collection)
        {
            vm.WriteBack();
            slots.Add(vm.Config);
        }
    }

    #region 插槽操作

    public void RemoveSlot(int groupIndex, TextSlotItemViewModel item)
    {
        var collection = GetCollection(groupIndex);
        if (collection.Remove(item))
        {
            if (SelectedSlot == item)
            {
                SelectedSlot = null;
                SelectedTabIndex = 0;
            }
            SaveAndRefresh();
        }
    }

    public void MoveSlotUp(int groupIndex, TextSlotItemViewModel item)
    {
        var collection = GetCollection(groupIndex);
        var index = collection.IndexOf(item);
        if (index > 0)
        {
            collection.Move(index, index - 1);
            SaveAndRefresh();
        }
    }

    public void MoveSlotDown(int groupIndex, TextSlotItemViewModel item)
    {
        var collection = GetCollection(groupIndex);
        var index = collection.IndexOf(item);
        if (index >= 0 && index < collection.Count - 1)
        {
            collection.Move(index, index + 1);
            SaveAndRefresh();
        }
    }

    public void SelectSlot(int groupIndex, TextSlotItemViewModel item)
    {
        switch (groupIndex)
        {
            case 0: SelectedLeftSlot = item; break;
            case 1: SelectedCenterSlot = item; break;
            case 2: SelectedRightSlot = item; break;
        }
        SelectedTabIndex = 1;
    }

    private static string GetGroupName(int index) => index switch { 0 => "左组", 1 => "中组", 2 => "右组", _ => "" };

    private ObservableCollection<TextSlotItemViewModel> GetCollection(int groupIndex) => groupIndex switch
    {
        0 => LeftSlots,
        1 => CenterSlots,
        2 => RightSlots,
        _ => LeftSlots
    };

    private int GetGroupIndexForCollection(ObservableCollection<TextSlotItemViewModel> collection) => ReferenceEquals(collection, LeftSlots) ? 0
        : ReferenceEquals(collection, CenterSlots) ? 1 : 2;

    #endregion

    #region 命令

    [RelayCommand]
    private void DeleteSlot(TextSlotItemViewModel? item)
    {
        if (item == null) return;
        RemoveSlot(SelectedGroupIndex, item);
    }

    [RelayCommand]
    private void MoveSlotUp(TextSlotItemViewModel? item)
    {
        if (item == null) return;
        MoveSlotUp(SelectedGroupIndex, item);
    }

    [RelayCommand]
    private void MoveSlotDown(TextSlotItemViewModel? item)
    {
        if (item == null) return;
        MoveSlotDown(SelectedGroupIndex, item);
    }

    [RelayCommand]
    private void RemoveSelectedSlot()
    {
        if (SelectedSlot == null) return;
        RemoveSlot(SelectedGroupIndex, SelectedSlot);
    }

    #endregion

    #region 数据源选项

    public SourceTypeOption[] SourceTypeOptions { get; } = Enum.GetValues<Models.TextSourceType>()
        .Where(t => t != Models.TextSourceType.None)
        .Select(t => new SourceTypeOption(t.GetDisplayName(), t))
        .ToArray();

    #endregion

    #region IDropTarget 拖拽实现

    public void DragOver(IDropInfo dropInfo)
    {
        var sourceItem = dropInfo.Data;
        var targetCollection = dropInfo.TargetCollection as ObservableCollection<TextSlotItemViewModel>;

        if (targetCollection == null) return;

        if (sourceItem is TextSlotItemViewModel)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }
        else if (sourceItem is ComponentLibraryItem)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Copy;
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        var targetCollection = dropInfo.TargetCollection as ObservableCollection<TextSlotItemViewModel>;
        if (targetCollection == null) return;

        var targetGroupIndex = GetGroupIndexForCollection(targetCollection);
        var insertIndex = dropInfo.InsertIndex;

        if (dropInfo.Data is TextSlotItemViewModel slotItem)
        {
            var sourceCollection = dropInfo.DragInfo?.SourceCollection as ObservableCollection<TextSlotItemViewModel>;
            if (sourceCollection == null) return;

            var sourceGroupIndex = GetGroupIndexForCollection(sourceCollection);
            var sourceIndex = sourceCollection.IndexOf(slotItem);

            if (sourceIndex < 0) return;

            if (ReferenceEquals(sourceCollection, targetCollection))
            {
                if (sourceIndex < insertIndex)
                    insertIndex--;

                if (sourceIndex == insertIndex) return;

                sourceCollection.Move(sourceIndex, insertIndex);
            }
            else
            {
                sourceCollection.RemoveAt(sourceIndex);
                if (insertIndex > targetCollection.Count)
                    insertIndex = targetCollection.Count;
                targetCollection.Insert(insertIndex, slotItem);
            }

            SelectSlot(targetGroupIndex, slotItem);
            SaveAndRefresh();
        }
        else if (dropInfo.Data is ComponentLibraryItem compItem)
        {
            var newSlot = new Models.TextSlotConfig { Source = compItem.SourceType };
            var vm = new TextSlotItemViewModel(newSlot, SaveAndRefresh);
            if (insertIndex > targetCollection.Count)
                insertIndex = targetCollection.Count;
            targetCollection.Insert(insertIndex, vm);

            SelectSlot(targetGroupIndex, vm);
            SaveAndRefresh();
        }
    }

    #endregion
}