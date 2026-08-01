using CommunityToolkit.Mvvm.ComponentModel;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels;

public partial class TextOverlayPageViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IDesktopWindowManager _desktopWindowManager;
    private Models.TimeTopSetting _setting;
    private bool _isInitializing = true;

    #region 全局开关

    [ObservableProperty]
    private bool _isEnabled;

    #endregion

    #region 全局样式

    [ObservableProperty]
    private double _fontSize = 12;

    [ObservableProperty]
    private double _opacity = 0.8;

    [ObservableProperty]
    private string _textColor = "#E0E0E0";

    [ObservableProperty]
    private double _itemSpacing = 8;

    [ObservableProperty]
    private string _selectedTextEffect = "none";

    #endregion

    #region 布局偏移

    [ObservableProperty]
    private double _leftOffset;

    [ObservableProperty]
    private double _centerOffset;

    [ObservableProperty]
    private double _rightOffset;

    [ObservableProperty]
    private double _verticalOffset;

    #endregion

    public TextOverlayPageViewModel(ISettingsService settingsService, IDesktopWindowManager desktopWindowManager)
    {
        _settingsService = settingsService;
        _desktopWindowManager = desktopWindowManager;
        _setting = _settingsService.GetTimeTopSetting();

        LoadFromConfig();
        _isInitializing = false;
    }

    private void LoadFromConfig()
    {
        var overlay = _setting.TextOverlay;
        IsEnabled = overlay.Enabled;

        var style = overlay.Style;
        FontSize = style.FontSize;
        Opacity = style.Opacity;
        TextColor = style.TextColor ?? "#E0E0E0";
        ItemSpacing = style.ItemSpacing;
        SelectedTextEffect = style.TextEffect;
        LeftOffset = style.LeftOffset;
        CenterOffset = style.CenterOffset;
        RightOffset = style.RightOffset;
        VerticalOffset = style.VerticalOffset;
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
        var overlay = _setting.TextOverlay;
        overlay.Enabled = IsEnabled;

        var style = overlay.Style;
        style.FontSize = FontSize;
        style.Opacity = Opacity;
        style.TextColor = TextColor;
        style.ItemSpacing = ItemSpacing;
        style.TextEffect = SelectedTextEffect;
        style.LeftOffset = LeftOffset;
        style.CenterOffset = CenterOffset;
        style.RightOffset = RightOffset;
        style.VerticalOffset = VerticalOffset;
    }

    #region 属性变更回调

    partial void OnIsEnabledChanged(bool value)
    {
        if (_isInitializing) return;
        _setting.TextOverlay.Enabled = value;
        _settingsService.SaveTimeTopSetting(_setting);
        _desktopWindowManager.RefreshTextOverlay();
    }

    partial void OnFontSizeChanged(double value) => SaveAndRefresh();
    partial void OnOpacityChanged(double value) => SaveAndRefresh();
    partial void OnTextColorChanged(string value) => SaveAndRefresh();
    partial void OnItemSpacingChanged(double value) => SaveAndRefresh();
    partial void OnLeftOffsetChanged(double value) => SaveAndRefresh();
    partial void OnCenterOffsetChanged(double value) => SaveAndRefresh();
    partial void OnRightOffsetChanged(double value) => SaveAndRefresh();
    partial void OnVerticalOffsetChanged(double value) => SaveAndRefresh();

    partial void OnSelectedTextEffectChanged(string value)
    {
        if (_isInitializing) return;
        _setting.TextOverlay.Style.TextEffect = value;
        _settingsService.SaveTimeTopSetting(_setting);
        _desktopWindowManager.RefreshTextOverlay();
    }

    #endregion
}