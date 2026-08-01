using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels;

public partial class TextOverlayLayoutPageViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IDesktopWindowManager _desktopWindowManager;
    private Models.TimeTopSetting _setting;
    private bool _isInitializing = true;

    #region 组可见性

    [ObservableProperty]
    private bool _leftGroupVisible = true;

    [ObservableProperty]
    private bool _centerGroupVisible = true;

    [ObservableProperty]
    private bool _rightGroupVisible = true;

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
        new("自定义文本", "显示固定的自定义文本内容", "TextT", Models.TextSourceType.CustomText, "文本类"),
        new("当前段名", "显示当前时间段的名称", "Tag", Models.TextSourceType.SegmentName, "文本类"),
        new("剩余时间", "显示当前段的剩余时间", "Hourglass", Models.TextSourceType.RemainingTime, "时间类"),
        new("已过时间", "显示当前段已过的时间", "History", Models.TextSourceType.ElapsedTime, "时间类"),
        new("进度百分比", "显示当前段的进度百分比", "DataUsage", Models.TextSourceType.ProgressPercent, "进度类"),
        new("系统时间", "显示当前系统时间", "Clock", Models.TextSourceType.CurrentTime, "时间类"),
        new("下一段名", "显示下一个时间段的名称", "FastForward", Models.TextSourceType.NextSegment, "文本类"),
        new("当前日期", "显示当前日期", "Calendar", Models.TextSourceType.CurrentDate, "日期类"),
        new("星期几", "显示当前是星期几", "CalendarWeekNumbers", Models.TextSourceType.CurrentDayOfWeek, "日期类"),
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

    #region 状态提示

    [ObservableProperty]
    private string _statusMessage = "提示: 在左侧选择插槽后，可在此编辑其属性。";

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
        LeftGroupVisible = layout.Left.Visible;
        CenterGroupVisible = layout.Center.Visible;
        RightGroupVisible = layout.Right.Visible;

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
        layout.Left.Visible = LeftGroupVisible;
        layout.Center.Visible = CenterGroupVisible;
        layout.Right.Visible = RightGroupVisible;

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

    partial void OnLeftGroupVisibleChanged(bool value) => SaveAndRefresh();
    partial void OnCenterGroupVisibleChanged(bool value) => SaveAndRefresh();
    partial void OnRightGroupVisibleChanged(bool value) => SaveAndRefresh();

    #endregion

    #region 插槽操作

    public void AddSlot(int groupIndex)
    {
        var collection = GetCollection(groupIndex);
        var newSlot = new Models.TextSlotConfig();
        var vm = new TextSlotItemViewModel(newSlot, SaveAndRefresh);
        collection.Add(vm);
        SelectedSlot = vm;
        SelectedGroupIndex = groupIndex;
        SelectedTabIndex = 1;
        StatusMessage = $"已在{GetGroupName(groupIndex)}添加新插槽";
        SaveAndRefresh();
    }

    public void AddSlotFromComponent(int groupIndex, Models.TextSourceType sourceType)
    {
        var collection = GetCollection(groupIndex);
        var newSlot = new Models.TextSlotConfig { Source = sourceType };
        var vm = new TextSlotItemViewModel(newSlot, SaveAndRefresh);
        collection.Add(vm);
        SelectedSlot = vm;
        SelectedGroupIndex = groupIndex;
        SelectedTabIndex = 1;
        StatusMessage = $"已从组件库添加「{sourceType}」到{GetGroupName(groupIndex)}";
        SaveAndRefresh();
    }

    public void RemoveSlot(int groupIndex, TextSlotItemViewModel item)
    {
        var collection = GetCollection(groupIndex);
        if (collection.Remove(item))
        {
            if (SelectedSlot == item)
                SelectedSlot = null;
            StatusMessage = $"已从{GetGroupName(groupIndex)}移除插槽";
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
            StatusMessage = $"已上移插槽";
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
            StatusMessage = $"已下移插槽";
            SaveAndRefresh();
        }
    }

    public void SelectSlot(int groupIndex, TextSlotItemViewModel item)
    {
        SelectedSlot = item;
        SelectedGroupIndex = groupIndex;
        SelectedTabIndex = 1;
        StatusMessage = $"正在编辑{GetGroupName(groupIndex)}的「{item.DisplayName}」";
    }

    private static string GetGroupName(int index) => index switch { 0 => "左组", 1 => "中组", 2 => "右组", _ => "" };

    private ObservableCollection<TextSlotItemViewModel> GetCollection(int groupIndex) => groupIndex switch
    {
        0 => LeftSlots,
        1 => CenterSlots,
        2 => RightSlots,
        _ => LeftSlots
    };

    #endregion
}

/// <summary>
/// 组件库项数据模型
/// </summary>
public class ComponentLibraryItem
{
    public string Name { get; }
    public string Description { get; }
    public string IconGlyph { get; }
    public Models.TextSourceType SourceType { get; }
    public string Category { get; }

    public ComponentLibraryItem(string name, string description, string iconGlyph, Models.TextSourceType sourceType, string category)
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

    [ObservableProperty]
    private string _colorOverride = "";

    private static readonly Models.TextSourceType[] SourceTypeValues =
        Enum.GetValues<Models.TextSourceType>();

    public string DisplayName => SourceTypeValues[SourceTypeIndex] switch
    {
        Models.TextSourceType.None => "（空）",
        Models.TextSourceType.CustomText => string.IsNullOrWhiteSpace(CustomText) ? "自定义" : CustomText,
        _ => SourceTypeValues[SourceTypeIndex].ToString()
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

    public string IconGlyph => SourceTypeValues[SourceTypeIndex] switch
    {
        Models.TextSourceType.None => "Cancel",
        Models.TextSourceType.CustomText => "TextT",
        Models.TextSourceType.SegmentName => "Tag",
        Models.TextSourceType.RemainingTime => "Hourglass",
        Models.TextSourceType.ElapsedTime => "History",
        Models.TextSourceType.ProgressPercent => "DataUsage",
        Models.TextSourceType.CurrentTime => "Clock",
        Models.TextSourceType.NextSegment => "FastForward",
        Models.TextSourceType.CurrentDate => "Calendar",
        Models.TextSourceType.CurrentDayOfWeek => "CalendarWeekNumbers",
        _ => "QuestionCircle"
    };

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

    partial void OnSourceTypeIndexChanged(int value) => OnSave();
    partial void OnCustomTextChanged(string value) => OnSave();
    partial void OnFormatChanged(string value) => OnSave();
    partial void OnShowSecondsChanged(bool value) => OnSave();
    partial void OnDecimalPlacesChanged(int value) => OnSave();
    partial void OnFallbackChanged(string value) => OnSave();
    partial void OnShowTimeChanged(bool value) => OnSave();
    partial void OnVisibleChanged(bool value) => OnSave();
    partial void OnPrefixChanged(string value) => OnSave();
    partial void OnSuffixChanged(string value) => OnSave();
    partial void OnFontSizeOverrideChanged(double? value) => OnSave();
    partial void OnColorOverrideChanged(string value) => OnSave();
}