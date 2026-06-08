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
        var fontSize = Math.Max(1, style.FontSize);
        var leftOffset = style.LeftOffset;
        var rightOffset = style.RightOffset;
        var centerOffset = style.CenterOffset;
        var verticalOffset = style.VerticalOffset;
        var itemSpacing = Math.Max(0, style.ItemSpacing);
        var opacity = Math.Clamp(style.Opacity, 0.0, 1.0);

        // 构建画刷
        var baseColor = ParseColor(style.TextColor, Colors.LightGray);
        var textColor = baseColor;
        textColor.A = (byte)(255 * opacity);
        var opaqueTextBrush = new SolidColorBrush(textColor);
        opaqueTextBrush.Freeze();

        var sepColor = baseColor;
        sepColor.A = (byte)(255 * opacity * 0.5);
        var separatorBrush = new SolidColorBrush(sepColor);
        separatorBrush.Freeze();

        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface(
            SystemFonts.MessageFontFamily,
            SystemFonts.MessageFontStyle,
            SystemFonts.MessageFontWeight,
            FontStretches.Normal);

        // 测量所有项
        var leftItems = MeasureGroup(LeftSlots, typeface, pixelsPerDip, fontSize);
        var rightItems = MeasureGroup(RightSlots, typeface, pixelsPerDip, fontSize);
        var centerItems = MeasureGroup(CenterSlots, typeface, pixelsPerDip, fontSize);

        // 1. 放置 Left（从左向右，优先级最高），正偏移→右移，基础边距16
        double x = 16 + leftOffset;
        for (int i = 0; i < leftItems.Count; i++)
        {
            leftItems[i].X = x;
            x += leftItems[i].TotalWidth;
            if (i < leftItems.Count - 1) x += itemSpacing;
        }
        double leftEnd = x;

        // 2. 放置 Right（从右向左，优先级居中），正偏移→右移，基础边距16
        double rightBound = width - 16 + rightOffset;
        x = rightBound;
        for (int i = rightItems.Count - 1; i >= 0; i--)
        {
            x -= rightItems[i].TotalWidth;
            rightItems[i].X = x;
            if (i > 0) x -= itemSpacing;
        }
        double rightStart = rightItems.Count > 0 ? rightItems[0].X : rightBound;

        // 3. Right 与 Left 重叠 → 隐藏重叠的 Right 项
        for (int i = 0; i < rightItems.Count; i++)
        {
            if (rightItems[i].X < leftEnd)
                rightItems[i].Visible = false;
            else
                break;
        }
        rightStart = rightItems.FirstOrDefault(m => m.Visible)?.X ?? rightBound;

        // 4. 放置 Center（居中 + 偏移，优先级最低），正偏移→右移
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

        // 5. Center 与 Left/Right 重叠 → 隐藏重叠的 Center 项
        foreach (var item in centerItems)
        {
            double itemEnd = item.X + item.TextWidth;
            if (item.X < leftEnd || itemEnd > rightStart)
                item.Visible = false;
        }

        // 6. 绘制所有可见项（正垂直偏移→上移，y=0 为面板顶部=进度条底边，不可越过）
        double baseY = (RenderSize.Height - fontSize) / 2 - 2;
        double y = Math.Max(0, baseY - verticalOffset);

        var textEffect = style.TextEffect ?? "none";

        foreach (var item in leftItems.Where(m => m.Visible))
            DrawItem(dc, item, y, typeface, pixelsPerDip, fontSize, opaqueTextBrush, separatorBrush, textEffect);
        foreach (var item in centerItems.Where(m => m.Visible))
            DrawItem(dc, item, y, typeface, pixelsPerDip, fontSize, opaqueTextBrush, separatorBrush, textEffect);
        foreach (var item in rightItems.Where(m => m.Visible))
            DrawItem(dc, item, y, typeface, pixelsPerDip, fontSize, opaqueTextBrush, separatorBrush, textEffect);
    }

    private static List<MeasuredSlot> MeasureGroup(
        ObservableCollection<TextSlotDisplay>? slots, Typeface typeface, double pixelsPerDip, double fontSize)
    {
        var result = new List<MeasuredSlot>();
        if (slots == null) return result;

        foreach (var slot in slots)
        {
            if (string.IsNullOrEmpty(slot.Text)) continue;

            var formattedText = new FormattedText(
                slot.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, fontSize, TextBrush, pixelsPerDip);

            double separatorWidth = 0;
            if (!string.IsNullOrEmpty(slot.Separator))
            {
                var formattedSep = new FormattedText(
                    slot.Separator, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, fontSize, TextBrush, pixelsPerDip);
                separatorWidth = formattedSep.Width;
            }

            result.Add(new MeasuredSlot(slot)
            {
                TextWidth = formattedText.Width,
                SeparatorWidth = separatorWidth,
                TotalWidth = formattedText.Width + separatorWidth,
            });
        }

        return result;
    }

    private static void DrawItem(DrawingContext dc, MeasuredSlot item, double y,
        Typeface typeface, double pixelsPerDip, double fontSize,
        Brush textBrush, Brush separatorBrush, string textEffect)
    {
        var mainText = new FormattedText(
            item.Slot.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, fontSize, textBrush, pixelsPerDip);

        switch (textEffect)
        {
            case "shadow":
                var shadowBrush = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
                shadowBrush.Freeze();
                dc.DrawText(new FormattedText(
                    item.Slot.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, fontSize, shadowBrush, pixelsPerDip),
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

        if (item.SeparatorWidth > 0)
        {
            var mainSep = new FormattedText(
                item.Slot.Separator, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, fontSize, separatorBrush, pixelsPerDip);

            switch (textEffect)
            {
                case "shadow":
                    var shadowBrush = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0));
                    shadowBrush.Freeze();
                    dc.DrawText(new FormattedText(
                        item.Slot.Separator, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        typeface, fontSize, shadowBrush, pixelsPerDip),
                        new Point(item.X + item.TextWidth + 1, y + 1));
                    dc.DrawText(mainSep, new Point(item.X + item.TextWidth, y));
                    break;

                case "outline":
                    var outlinePen = new Pen(new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)), 0.8);
                    outlinePen.Freeze();
                    var sepGeo = mainSep.BuildGeometry(new Point(item.X + item.TextWidth, y));
                    dc.DrawGeometry(null, outlinePen, sepGeo);
                    dc.DrawText(mainSep, new Point(item.X + item.TextWidth, y));
                    break;

                default:
                    dc.DrawText(mainSep, new Point(item.X + item.TextWidth, y));
                    break;
            }
        }
    }

    private class MeasuredSlot
    {
        public TextSlotDisplay Slot { get; }
        public double TextWidth { get; set; }
        public double SeparatorWidth { get; set; }
        public double TotalWidth { get; set; }
        public double X { get; set; }
        public bool Visible { get; set; } = true;

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