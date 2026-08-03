using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ReTime_Testing.Models;
using ReTime_Testing.ViewModels;

namespace ReTime_Testing.Views.TimeTopDesktop;

/// <summary>
/// 优先级文字覆盖面板
/// 三组文字（Left / Center / Right）按优先级布局
/// 优先级：Left > Right > Center
/// 低优先级项与高优先级项重叠时，低优先级项消失
/// </summary>
public class PriorityTextOverlayPanel : FrameworkElement
{
    private static readonly SolidColorBrush TextBrush = Brushes.White;

    static PriorityTextOverlayPanel()
    {
        TextBrush.Freeze();
    }

    #region Dependency Properties

    public static readonly DependencyProperty LeftSlotsProperty =
        DependencyProperty.Register(nameof(LeftSlots), typeof(ObservableCollection<TextSlotDisplay>),
            typeof(PriorityTextOverlayPanel),
            new FrameworkPropertyMetadata(null, OnSlotsChanged));

    public static readonly DependencyProperty CenterSlotsProperty =
        DependencyProperty.Register(nameof(CenterSlots), typeof(ObservableCollection<TextSlotDisplay>),
            typeof(PriorityTextOverlayPanel),
            new FrameworkPropertyMetadata(null, OnSlotsChanged));

    public static readonly DependencyProperty RightSlotsProperty =
        DependencyProperty.Register(nameof(RightSlots), typeof(ObservableCollection<TextSlotDisplay>),
            typeof(PriorityTextOverlayPanel),
            new FrameworkPropertyMetadata(null, OnSlotsChanged));

    public static readonly DependencyProperty StyleConfigProperty =
        DependencyProperty.Register(nameof(StyleConfig), typeof(TextOverlayStyleConfig),
            typeof(PriorityTextOverlayPanel),
            new FrameworkPropertyMetadata(new TextOverlayStyleConfig(), OnStyleChanged));

    public ObservableCollection<TextSlotDisplay>? LeftSlots
    {
        get => (ObservableCollection<TextSlotDisplay>?)GetValue(LeftSlotsProperty);
        set => SetValue(LeftSlotsProperty, value);
    }

    public ObservableCollection<TextSlotDisplay>? CenterSlots
    {
        get => (ObservableCollection<TextSlotDisplay>?)GetValue(CenterSlotsProperty);
        set => SetValue(CenterSlotsProperty, value);
    }

    public ObservableCollection<TextSlotDisplay>? RightSlots
    {
        get => (ObservableCollection<TextSlotDisplay>?)GetValue(RightSlotsProperty);
        set => SetValue(RightSlotsProperty, value);
    }

    public TextOverlayStyleConfig StyleConfig
    {
        get => (TextOverlayStyleConfig)GetValue(StyleConfigProperty);
        set => SetValue(StyleConfigProperty, value);
    }

    private static void OnSlotsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (PriorityTextOverlayPanel)d;
        if (e.OldValue is ObservableCollection<TextSlotDisplay> oldCol)
            oldCol.CollectionChanged -= panel.OnCollectionChanged;
        if (e.NewValue is ObservableCollection<TextSlotDisplay> newCol)
            newCol.CollectionChanged += panel.OnCollectionChanged;
        panel.InvalidateVisual();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    private static void OnStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PriorityTextOverlayPanel)d).InvalidateVisual();
    }

    #endregion

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var width = RenderSize.Width;
        if (width <= 0) return;

        var style = StyleConfig ?? new TextOverlayStyleConfig();
        var baseFontSize = Math.Max(1, style.FontSize);
        var leftOffset = style.LeftOffset;
        var rightOffset = style.RightOffset;
        var centerOffset = style.CenterOffset;
        var verticalOffset = style.VerticalOffset;
        var itemSpacing = Math.Max(0, style.ItemSpacing);
        var opacity = Math.Clamp(style.Opacity, 0.0, 1.0);

        var baseColor = ParseColor(style.TextColor, Colors.LightGray);
        var textColor = baseColor;
        textColor.A = (byte)(255 * opacity);
        var opaqueTextBrush = new SolidColorBrush(textColor);
        opaqueTextBrush.Freeze();

        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface(
            SystemFonts.MessageFontFamily,
            SystemFonts.MessageFontStyle,
            SystemFonts.MessageFontWeight,
            FontStretches.Normal);

        var leftItems = MeasureGroup(LeftSlots, typeface, pixelsPerDip, baseFontSize, textColor, opacity);
        var rightItems = MeasureGroup(RightSlots, typeface, pixelsPerDip, baseFontSize, textColor, opacity);
        var centerItems = MeasureGroup(CenterSlots, typeface, pixelsPerDip, baseFontSize, textColor, opacity);

        double x = 16 + leftOffset;
        for (int i = 0; i < leftItems.Count; i++)
        {
            leftItems[i].X = x;
            x += leftItems[i].TotalWidth;
            if (i < leftItems.Count - 1) x += itemSpacing;
        }
        double leftEnd = x;

        double rightBound = width - 16 + rightOffset;
        x = rightBound;
        for (int i = rightItems.Count - 1; i >= 0; i--)
        {
            x -= rightItems[i].TotalWidth;
            rightItems[i].X = x;
            if (i > 0) x -= itemSpacing;
        }
        double rightStart = rightItems.Count > 0 ? rightItems[0].X : rightBound;

        for (int i = 0; i < rightItems.Count; i++)
        {
            if (rightItems[i].X < leftEnd)
                rightItems[i].Visible = false;
            else
                break;
        }
        rightStart = rightItems.FirstOrDefault(m => m.Visible)?.X ?? rightBound;

        double centerTotalWidth = centerItems.Sum(m => m.TotalWidth)
            + Math.Max(0, centerItems.Count - 1) * itemSpacing;
        double centerStart = (width - centerTotalWidth) / 2 + centerOffset;
        x = centerStart;
        for (int i = 0; i < centerItems.Count; i++)
        {
            centerItems[i].X = x;
            x += centerItems[i].TotalWidth;
            if (i < centerItems.Count - 1) x += itemSpacing;
        }

        foreach (var item in centerItems)
        {
            double itemEnd = item.X + item.TextWidth;
            if (item.X < leftEnd || itemEnd > rightStart)
                item.Visible = false;
        }

        double baseY = (RenderSize.Height - baseFontSize) / 2 - 2;
        double y = Math.Max(0, baseY - verticalOffset);

        var textEffect = style.TextEffect ?? "none";

        foreach (var item in leftItems.Where(m => m.Visible))
            DrawItem(dc, item, y, typeface, pixelsPerDip, opaqueTextBrush, textEffect);
        foreach (var item in centerItems.Where(m => m.Visible))
            DrawItem(dc, item, y, typeface, pixelsPerDip, opaqueTextBrush, textEffect);
        foreach (var item in rightItems.Where(m => m.Visible))
            DrawItem(dc, item, y, typeface, pixelsPerDip, opaqueTextBrush, textEffect);
    }

    private static List<MeasuredSlot> MeasureGroup(
        ObservableCollection<TextSlotDisplay>? slots, Typeface baseTypeface, double pixelsPerDip, double baseFontSize, Color baseTextColor, double opacity)
    {
        var result = new List<MeasuredSlot>();
        if (slots == null) return result;

        foreach (var slot in slots)
        {
            if (string.IsNullOrEmpty(slot.Text)) continue;

            var fontSize = slot.FontSizeOverride ?? baseFontSize;
            var itemColor = ParseColor(slot.ColorOverride, baseTextColor);
            itemColor.A = (byte)(255 * opacity);
            var itemBrush = new SolidColorBrush(itemColor);
            itemBrush.Freeze();

            var itemTypeface = ResolveTypeface(slot.FontFamily, baseTypeface);

            var formattedText = new FormattedText(
                slot.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                itemTypeface, fontSize, TextBrush, pixelsPerDip);

            result.Add(new MeasuredSlot(slot)
            {
                TextWidth = formattedText.Width,
                TotalWidth = formattedText.Width,
                FontSize = fontSize,
                ItemBrush = itemBrush,
                Typeface = itemTypeface,
            });
        }

        return result;
    }

    /// <summary>
    /// 解析字体系列名称为 Typeface，无效或为空时回退到全局 typeface
    /// </summary>
    private static Typeface ResolveTypeface(string? fontFamilyName, Typeface fallback)
    {
        if (string.IsNullOrWhiteSpace(fontFamilyName)) return fallback;

        try
        {
            var family = new FontFamily(fontFamilyName);
            var faces = family.GetTypefaces();
            if (faces.Any()) return faces.First();
        }
        catch
        {
            // 字体名称无效时回退
        }
        return fallback;
    }

    private static void DrawItem(DrawingContext dc, MeasuredSlot item, double y,
        Typeface baseTypeface, double pixelsPerDip,
        Brush fallbackBrush, string textEffect)
    {
        var brush = item.ItemBrush ?? fallbackBrush;
        var itemTypeface = item.Typeface;

        var mainText = new FormattedText(
            item.Slot.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            itemTypeface, item.FontSize, brush, pixelsPerDip);

        switch (textEffect)
        {
            case "shadow":
                var shadowBrush = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
                shadowBrush.Freeze();
                dc.DrawText(new FormattedText(
                    item.Slot.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    itemTypeface, item.FontSize, shadowBrush, pixelsPerDip),
                    new Point(item.X + 1, y + 1));
                dc.DrawText(mainText, new Point(item.X, y));
                break;

            case "outline":
                var outlinePen = new Pen(new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)), 0.8);
                outlinePen.Freeze();
                var textGeo = mainText.BuildGeometry(new Point(item.X, y));
                dc.DrawGeometry(null, outlinePen, textGeo);
                dc.DrawText(mainText, new Point(item.X, y));
                break;

            default:
                dc.DrawText(mainText, new Point(item.X, y));
                break;
        }
    }

    private class MeasuredSlot
    {
        public TextSlotDisplay Slot { get; }
        public double TextWidth { get; set; }
        public double TotalWidth { get; set; }
        public double X { get; set; }
        public bool Visible { get; set; } = true;
        public double FontSize { get; set; }
        public Brush? ItemBrush { get; set; }
        public Typeface Typeface { get; set; } = SystemFonts.MessageFontFamily.GetTypefaces().First();

        public MeasuredSlot(TextSlotDisplay slot) => Slot = slot;
    }

    private static Color ParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try
        {
            var obj = ColorConverter.ConvertFromString(hex);
            return obj is Color c ? c : fallback;
        }
        catch
        {
            return fallback;
        }
    }
}