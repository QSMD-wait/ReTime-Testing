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
    /// 分隔符
    /// </summary>
    public string Separator { get; }

    public TextSlotDisplay(string text, string separator = "  ")
    {
        Text = text;
        Separator = separator;
    }
}

/// <summary>
/// 文字覆盖 ViewModel
/// 管理进度条下方三栏文字信息的显示和刷新
/// </summary>
public partial class TextOverlayViewModel : ObservableObject, IDisposable
{
    private readonly ITextSlotResolver _resolver;
    private readonly IConfigurationManager _configManager;
    private readonly DispatcherTimer _refreshTimer;

    private TextOverlayConfig _config = new();

    /// <summary>
    /// 左侧文字列表
    /// </summary>
    public ObservableCollection<TextSlotDisplay> LeftSlots { get; } = [];

    /// <summary>
    /// 中间文字列表
    /// </summary>
    public ObservableCollection<TextSlotDisplay> CenterSlots { get; } = [];

    /// <summary>
    /// 右侧文字列表
    /// </summary>
    public ObservableCollection<TextSlotDisplay> RightSlots { get; } = [];

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private TextOverlayStyleConfig _style = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public TextOverlayViewModel(ITextSlotResolver resolver, IConfigurationManager configManager)
    {
        _resolver = resolver;
        _configManager = configManager;

        LoadConfig();

        // 100ms 刷新定时器
        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _refreshTimer.Tick += OnRefreshTick;

        if (_config.Enabled)
        {
            _refreshTimer.Start();
        }
    }

    /// <summary>
    /// 加载配置
    /// </summary>
    public void LoadConfig()
    {
        var setting = _configManager.LoadTimeTopSetting();
        _config = setting.TextOverlay ?? new TextOverlayConfig();
        IsVisible = _config.Enabled;
        Style = _config.Style;
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
        // 确保列表长度一致
        while (slots.Count < configs.Count)
        {
            slots.Add(new TextSlotDisplay(""));
        }
        while (slots.Count > configs.Count)
        {
            slots.RemoveAt(slots.Count - 1);
        }

        // 更新文本
        for (int i = 0; i < configs.Count; i++)
        {
            var config = configs[i];
            var text = _resolver.Resolve(config.Source, config.CustomText);
            var oldSlot = slots[i];

            if (oldSlot.Text != text || oldSlot.Separator != config.Separator)
            {
                slots[i] = new TextSlotDisplay(text, config.Separator);
            }
        }
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTick;
    }
}