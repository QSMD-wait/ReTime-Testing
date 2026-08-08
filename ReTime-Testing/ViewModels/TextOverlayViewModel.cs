using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using ReTime_Testing.Models;
using ReTime_Testing.Services;

namespace ReTime_Testing.ViewModels;

/// <summary>
/// 文字插槽显示项
/// </summary>
public class TextSlotDisplay
{
    /// <summary>
    /// 显示文本
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// 单项字体大小覆盖（null 表示使用全局字体大小）
    /// </summary>
    public double? FontSizeOverride { get; }

    /// <summary>
    /// 单项颜色覆盖（null 表示使用全局文字颜色）
    /// </summary>
    public string? ColorOverride { get; }

    /// <summary>
    /// 单项字体系列覆盖（null 表示使用全局字体）
    /// </summary>
    public string? FontFamily { get; }

    public TextSlotDisplay(string text, double? fontSizeOverride = null, string? colorOverride = null, string? fontFamily = null)
    {
        Text = text;
        FontSizeOverride = fontSizeOverride;
        ColorOverride = colorOverride;
        FontFamily = fontFamily;
    }
}

/// <summary>
/// 文字覆盖 ViewModel
/// 管理进度条下方三栏文字信息的显示和刷新
/// </summary>
public partial class TextOverlayViewModel : ObservableObject, IDisposable
{
    private readonly ITextSlotResolver _resolver;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherTimer _refreshTimer;

    private TextOverlayConfig _config = new();

    /// <summary>
    /// 左侧文字列表
    /// </summary>
    public ObservableCollection<TextSlotDisplay> LeftSlots { get; } = new();

    /// <summary>
    /// 中间文字列表
    /// </summary>
    public ObservableCollection<TextSlotDisplay> CenterSlots { get; } = new();

    /// <summary>
    /// 右侧文字列表
    /// </summary>
    public ObservableCollection<TextSlotDisplay> RightSlots { get; } = new();

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private TextOverlayStyleConfig _style = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public TextOverlayViewModel(ITextSlotResolver resolver, ISettingsService settingsService)
    {
        _resolver = resolver;
        _settingsService = settingsService;

        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _refreshTimer.Tick += OnRefreshTick;

        LoadConfig();
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public void LoadConfig()
    {
        var setting = _settingsService.GetTimeTopSetting();
        _config = setting.TextOverlay ?? new TextOverlayConfig();
        IsVisible = _config.Enabled;
        Style = _config.Style.Clone();

        if (_config.Enabled && !_refreshTimer.IsEnabled)
            _refreshTimer.Start();
        else if (!_config.Enabled && _refreshTimer.IsEnabled)
            _refreshTimer.Stop();
    }

    /// <summary>
    /// 启动刷新
    /// </summary>
    public void Start()
    {
        if (_config.Enabled && !_refreshTimer.IsEnabled)
        {
            _refreshTimer.Start();
        }
    }

    /// <summary>
    /// 停止刷新
    /// </summary>
    public void Stop()
    {
        _refreshTimer.Stop();
    }

    private void OnRefreshTick(object? sender, EventArgs e)
    {
        RefreshSlots();
    }

    /// <summary>
    /// 刷新所有插槽文本
    /// </summary>
    private void RefreshSlots()
    {
        if (!_config.Enabled) return;

        if (_config.Layout.Left.Visible)
            UpdateSlotList(LeftSlots, _config.Layout.Left.Slots);
        else
            LeftSlots.Clear();

        if (_config.Layout.Center.Visible)
            UpdateSlotList(CenterSlots, _config.Layout.Center.Slots);
        else
            CenterSlots.Clear();

        if (_config.Layout.Right.Visible)
            UpdateSlotList(RightSlots, _config.Layout.Right.Slots);
        else
            RightSlots.Clear();
    }

    /// <summary>
    /// 更新插槽列表（复用已有对象减少 GC）
    /// </summary>
    private void UpdateSlotList(ObservableCollection<TextSlotDisplay> slots, List<TextSlotConfig> configs)
    {
        while (slots.Count < configs.Count)
        {
            slots.Add(new TextSlotDisplay(""));
        }
        while (slots.Count > configs.Count)
        {
            slots.RemoveAt(slots.Count - 1);
        }

        for (int i = 0; i < configs.Count; i++)
        {
            var config = configs[i];
            var text = _resolver.Resolve(config.Source, config.SourceSettings, config.CommonSettings);
            var oldSlot = slots[i];

            if (oldSlot.Text != text
                || oldSlot.FontSizeOverride != config.CommonSettings.FontSizeOverride
                || oldSlot.ColorOverride != config.CommonSettings.ColorOverride
                || oldSlot.FontFamily != config.CommonSettings.FontFamily)
            {
                slots[i] = new TextSlotDisplay(text, config.CommonSettings.FontSizeOverride, config.CommonSettings.ColorOverride, config.CommonSettings.FontFamily);
            }
        }
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTick;
    }
}