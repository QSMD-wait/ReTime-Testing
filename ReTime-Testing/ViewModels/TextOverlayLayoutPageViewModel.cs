using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
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

    #region 组可见性

    public bool LeftGroupVisible => true;
    public bool CenterGroupVisible => true;
    public bool RightGroupVisible => true;

    #endregion

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
        new("系统时间", "显示当前系统时间", FluentSystemIcons.Clock_24_Regular, Models.TextSourceType.CurrentTime, "时间类"),
        new("下一段名", "显示下一个时间段的名称", FluentSystemIcons.FastForward_24_Regular, Models.TextSourceType.NextSegment, "文本类"),
        new("当前日期", "显示当前日期", FluentSystemIcons.Calendar_24_Regular, Models.TextSourceType.CurrentDate, "日期类"),
        new("星期几", "显示当前是星期几", FluentSystemIcons.CalendarWeekNumbers_24_Regular, Models.TextSourceType.CurrentDayOfWeek, "日期类"),
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

    #region 属性变更回调

    #endregion

    #region 插槽操作

    public void RemoveSlot(int groupIndex, TextSlotItemViewModel item)
    {
        var collection = GetCollection(groupIndex);
        if (collection.Remove(item))
        {
            if (SelectedSlot == item)
                SelectedSlot = null;
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

/// <summary>
/// 组件库项数据模型
/// </summary>
public class ComponentLibraryItem
{
    public string Name { get; }
    public string Description { get; }
    public FontIconData? IconGlyph { get; }
    public Models.TextSourceType SourceType { get; }
    public string Category { get; }

    public ComponentLibraryItem(string name, string description, FontIconData? iconGlyph, Models.TextSourceType sourceType, string category)
    {
        Name = name;
        Description = description;
        IconGlyph = iconGlyph;
        SourceType = sourceType;
        Category = category;
    }
}

public partial class TextSlotItemViewModel : ObservableObject
{
    private readonly Action? _saveCallback;
    private bool _isInitializing = true;

    public Models.TextSlotConfig Config { get; }

    [ObservableProperty]
    private int _sourceTypeIndex;

    [ObservableProperty]
    private string _customText = "";

    [ObservableProperty]
    private string _format = "";

    [ObservableProperty]
    private bool _showSeconds = true;

    [ObservableProperty]
    private int _decimalPlaces = 1;

    [ObservableProperty]
    private string _fallback = "";

    [ObservableProperty]
    private bool _showTime;

    [ObservableProperty]
    private bool _visible = true;

    [ObservableProperty]
    private string _prefix = "";

    [ObservableProperty]
    private string _suffix = "";

    [ObservableProperty]
    private double? _fontSizeOverride;

    public double FontSizeOverrideValue
    {
        get => FontSizeOverride ?? 0;
        set
        {
            if (value <= 0)
                FontSizeOverride = null;
            else
                FontSizeOverride = value;
        }
    }

    [ObservableProperty]
    private string _colorOverride = "";

    private static readonly Models.TextSourceType[] SourceTypeValues =
        Enum.GetValues<Models.TextSourceType>();

    public string DisplayName => SourceTypeValues[SourceTypeIndex] switch
    {
        Models.TextSourceType.None => "（空）",
        Models.TextSourceType.CustomText => string.IsNullOrWhiteSpace(CustomText) ? "自定义文本" : CustomText,
        Models.TextSourceType.SegmentName => "当前段名",
        Models.TextSourceType.RemainingTime => "剩余时间",
        Models.TextSourceType.ElapsedTime => "已过时间",
        Models.TextSourceType.ProgressPercent => "进度百分比",
        Models.TextSourceType.CurrentTime => "系统时间",
        Models.TextSourceType.NextSegment => "下一段名",
        Models.TextSourceType.CurrentDate => "当前日期",
        Models.TextSourceType.CurrentDayOfWeek => "星期几",
        _ => "未知"
    };

    public string DisplayDescription => SourceTypeValues[SourceTypeIndex] switch
    {
        Models.TextSourceType.None => "不显示任何内容",
        Models.TextSourceType.CustomText => string.IsNullOrWhiteSpace(CustomText) ? "自定义文本" : CustomText,
        Models.TextSourceType.SegmentName => "当前时间段名称",
        Models.TextSourceType.RemainingTime => "剩余时间",
        Models.TextSourceType.ElapsedTime => "已过时间",
        Models.TextSourceType.ProgressPercent => "进度百分比",
        Models.TextSourceType.CurrentTime => "系统时间",
        Models.TextSourceType.NextSegment => "下一段名称",
        Models.TextSourceType.CurrentDate => "当前日期",
        Models.TextSourceType.CurrentDayOfWeek => "星期几",
        _ => ""
    };

    public FontIconData? IconGlyph => SourceTypeValues[SourceTypeIndex] switch
    {
        Models.TextSourceType.None => FluentSystemIcons.Dismiss_24_Regular,
        Models.TextSourceType.CustomText => FluentSystemIcons.TextT_24_Regular,
        Models.TextSourceType.SegmentName => FluentSystemIcons.Tag_24_Regular,
        Models.TextSourceType.RemainingTime => FluentSystemIcons.Hourglass_24_Regular,
        Models.TextSourceType.ElapsedTime => FluentSystemIcons.History_24_Regular,
        Models.TextSourceType.ProgressPercent => FluentSystemIcons.DataUsage_24_Regular,
        Models.TextSourceType.CurrentTime => FluentSystemIcons.Clock_24_Regular,
        Models.TextSourceType.NextSegment => FluentSystemIcons.FastForward_24_Regular,
        Models.TextSourceType.CurrentDate => FluentSystemIcons.Calendar_24_Regular,
        Models.TextSourceType.CurrentDayOfWeek => FluentSystemIcons.CalendarWeekNumbers_24_Regular,
        _ => FluentSystemIcons.QuestionCircle_24_Regular
    };

    public bool IsSourceConfigurable => SourceTypeIndex > 0;

    public bool IsCustomTextRelevant => SourceTypeIndex == 1;

    public bool IsFormatRelevant => SourceTypeIndex is 6 or 8 or 9;

    public bool IsShowSecondsRelevant => SourceTypeIndex is 3 or 4;

    public bool IsDecimalPlacesRelevant => SourceTypeIndex == 5;

    public bool IsFallbackRelevant => SourceTypeIndex is 2 or 7;

    public bool IsShowTimeRelevant => SourceTypeIndex == 7;

    public TextSlotItemViewModel(Models.TextSlotConfig config, Action? saveCallback)
    {
        Config = config;
        _saveCallback = saveCallback;

        SourceTypeIndex = Array.IndexOf(SourceTypeValues, config.Source);
        CustomText = config.SourceSettings.Text ?? "";
        Format = config.SourceSettings.Format ?? "";
        ShowSeconds = config.SourceSettings.ShowSeconds ?? true;
        DecimalPlaces = config.SourceSettings.DecimalPlaces ?? 1;
        Fallback = config.SourceSettings.Fallback ?? "";
        ShowTime = config.SourceSettings.ShowTime ?? false;

        Visible = config.CommonSettings.Visible;
        Prefix = config.CommonSettings.Prefix ?? "";
        Suffix = config.CommonSettings.Suffix ?? "";
        FontSizeOverride = config.CommonSettings.FontSizeOverride;
        ColorOverride = config.CommonSettings.ColorOverride ?? "";

        _isInitializing = false;
    }

    public void WriteBack()
    {
        Config.Source = SourceTypeValues[SourceTypeIndex];
        Config.SourceSettings.Text = string.IsNullOrWhiteSpace(CustomText) ? null : CustomText;
        Config.SourceSettings.Format = string.IsNullOrWhiteSpace(Format) ? null : Format;
        Config.SourceSettings.ShowSeconds = ShowSeconds;
        Config.SourceSettings.DecimalPlaces = DecimalPlaces;
        Config.SourceSettings.Fallback = string.IsNullOrWhiteSpace(Fallback) ? null : Fallback;
        Config.SourceSettings.ShowTime = ShowTime;

        Config.CommonSettings.Visible = Visible;
        Config.CommonSettings.Prefix = string.IsNullOrWhiteSpace(Prefix) ? null : Prefix;
        Config.CommonSettings.Suffix = string.IsNullOrWhiteSpace(Suffix) ? null : Suffix;
        Config.CommonSettings.FontSizeOverride = FontSizeOverride;
        Config.CommonSettings.ColorOverride = string.IsNullOrWhiteSpace(ColorOverride) ? null : ColorOverride;
    }

    private void OnSave()
    {
        if (_isInitializing) return;
        WriteBack();
        _saveCallback?.Invoke();
    }

    partial void OnSourceTypeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DisplayDescription));
        OnPropertyChanged(nameof(IconGlyph));
        OnPropertyChanged(nameof(IsSourceConfigurable));
        OnPropertyChanged(nameof(IsCustomTextRelevant));
        OnPropertyChanged(nameof(IsFormatRelevant));
        OnPropertyChanged(nameof(IsShowSecondsRelevant));
        OnPropertyChanged(nameof(IsDecimalPlacesRelevant));
        OnPropertyChanged(nameof(IsFallbackRelevant));
        OnPropertyChanged(nameof(IsShowTimeRelevant));
        OnSave();
    }
    partial void OnCustomTextChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayName));
        OnSave();
    }
    partial void OnFormatChanged(string value) => OnSave();
    partial void OnShowSecondsChanged(bool value) => OnSave();
    partial void OnDecimalPlacesChanged(int value) => OnSave();
    partial void OnFallbackChanged(string value) => OnSave();
    partial void OnShowTimeChanged(bool value) => OnSave();
    partial void OnVisibleChanged(bool value) => OnSave();
    partial void OnPrefixChanged(string value) => OnSave();
    partial void OnSuffixChanged(string value) => OnSave();
    partial void OnFontSizeOverrideChanged(double? value)
    {
        OnPropertyChanged(nameof(FontSizeOverrideValue));
        OnSave();
    }
    partial void OnColorOverrideChanged(string value) => OnSave();
}