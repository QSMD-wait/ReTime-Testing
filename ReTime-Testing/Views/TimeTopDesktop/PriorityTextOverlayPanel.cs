using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
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
    private const double FontSize = 12;
    private const double LeftMargin = 10;
    private const double RightMargin = 10;
    private const double ItemSpacing = 8; // 组件之间的间隔

    private static readonly SolidColorBrush TextBrush;
    private static readonly SolidColorBrush SeparatorBrush;
    private static readonly SolidColorBrush OpaqueTextBrush; // Opacity=0.8

    static PriorityTextOverlayPanel()
    {
        TextBrush = Brushes.White;
        TextBrush.Freeze();

        var sepColor = Colors.White;
        sepColor.A = (byte)(255 * 0.4);
        SeparatorBrush = new SolidColorBrush(sepColor);
        SeparatorBrush.Freeze();

        var textColor = Colors.White;
        textColor.A = (byte)(255 * 0.8);
        OpaqueTextBrush = new SolidColorBrush(textColor);
        OpaqueTextBrush.Freeze();
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

    #endregion

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var width = RenderSize.Width;
        if (width <= 0) return;

        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface(
            SystemFonts.MessageFontFamily,
            SystemFonts.MessageFontStyle,
            SystemFonts.MessageFontWeight,
            FontStretches.Normal);

        // 测量所有项
        var leftItems = MeasureGroup(LeftSlots, typeface, pixelsPerDip);
        var rightItems = MeasureGroup(RightSlots, typeface, pixelsPerDip);
        var centerItems = MeasureGroup(CenterSlots, typeface, pixelsPerDip);

        // 1. 放置 Left（从左向右，优先级最高）
        double x = LeftMargin;
        for (int i = 0; i < leftItems.Count; i++)
        {
            leftItems[i].X = x;
            x += leftItems[i].TotalWidth;
            if (i < leftItems.Count - 1) x += ItemSpacing;
        }
        double leftEnd = x; // Left 占据的右边界

        // 2. 放置 Right（从右向左，优先级居中）
        x = width - RightMargin;
        for (int i = rightItems.Count - 1; i >= 0; i--)
        {
            x -= rightItems[i].TotalWidth;
            rightItems[i].X = x;
            if (i > 0) x -= ItemSpacing;
        }
        double rightStart = rightItems.Count > 0 ? rightItems[0].X : width - RightMargin;

        // 3. Right 与 Left 重叠 → 隐藏重叠的 Right 项
        for (int i = 0; i < rightItems.Count; i++)
        {
            if (rightItems[i].X < leftEnd)
                rightItems[i].Visible = false;
            else
                break; // 从左到右排列，第一个不重叠则后续都不重叠
        }
        rightStart = rightItems.FirstOrDefault(m => m.Visible)?.X ?? width - RightMargin;

        // 4. 放置 Center（绝对居中，优先级最低）
        double centerTotalWidth = centerItems.Sum(m => m.TotalWidth)
            + Math.Max(0, centerItems.Count - 1) * ItemSpacing;
        double centerStart = (width - centerTotalWidth) / 2;
        x = centerStart;
        for (int i = 0; i < centerItems.Count; i++)
        {
            centerItems[i].X = x;
            x += centerItems[i].TotalWidth;
            if (i < centerItems.Count - 1) x += ItemSpacing;
        }

        // 5. Center 与 Left/Right 重叠 → 隐藏重叠的 Center 项
        foreach (var item in centerItems)
        {
            double itemEnd = item.X + item.TextWidth; // 文本右边界（不含尾部分隔符）
            if (item.X < leftEnd || itemEnd > rightStart)
                item.Visible = false;
        }

        // 6. 绘制所有可见项
        double y = Math.Max(0, (RenderSize.Height - FontSize) / 2);

        foreach (var item in leftItems.Where(m => m.Visible))
            DrawItem(dc, item, y, typeface, pixelsPerDip);
        foreach (var item in centerItems.Where(m => m.Visible))
            DrawItem(dc, item, y, typeface, pixelsPerDip);
        foreach (var item in rightItems.Where(m => m.Visible))
            DrawItem(dc, item, y, typeface, pixelsPerDip);
    }

    private static List<MeasuredSlot> MeasureGroup(
        ObservableCollection<TextSlotDisplay>? slots, Typeface typeface, double pixelsPerDip)
    {
        var result = new List<MeasuredSlot>();
        if (slots == null) return result;

        foreach (var slot in slots)
        {
            if (string.IsNullOrEmpty(slot.Text)) continue;

            var formattedText = new FormattedText(
                slot.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, FontSize, TextBrush, pixelsPerDip);

            double separatorWidth = 0;
            if (!string.IsNullOrEmpty(slot.Separator))
            {
                var formattedSep = new FormattedText(
                    slot.Separator, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, FontSize, SeparatorBrush, pixelsPerDip);
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
        Typeface typeface, double pixelsPerDip)
    {
        // 文本（Opacity=0.8）
        var formattedText = new FormattedText(
            item.Slot.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
            typeface, FontSize, OpaqueTextBrush, pixelsPerDip);
        dc.DrawText(formattedText, new Point(item.X, y));

        // 分隔符（Opacity=0.4）
        if (item.SeparatorWidth > 0)
        {
            var formattedSep = new FormattedText(
                item.Slot.Separator, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                typeface, FontSize, SeparatorBrush, pixelsPerDip);
            dc.DrawText(formattedSep, new Point(item.X + item.TextWidth, y));
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
}
