using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ReTime_Testing.Controls;

/// <summary>
/// 抽屉状态变更事件参数
/// </summary>
public sealed class DrawerStateChangedEventArgs : RoutedEventArgs
{
    public bool IsOpen { get; }

    public DrawerStateChangedEventArgs(bool isOpen) : base()
    {
        IsOpen = isOpen;
    }
}

/// <summary>
/// 整数索引转可见性转换器：参数为索引值，当绑定值等于参数时返回 Visible，否则 Collapsed。
/// </summary>
public sealed class IndexToVisibilityConverter : IValueConverter
{
    public static readonly IndexToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && parameter is string s && int.TryParse(s, out int target))
            return index == target ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// 侧拉抽屉控件：从右侧滑入/滑出的内容面板，带遮罩层和动画。
/// 可应用于任意窗口，通过 IsDrawerOpen 控制开关，DrawerContent 设置抽屉内容。
/// </summary>
[TemplatePart(Name = "PART_DrawerBorder", Type = typeof(Border))]
[TemplatePart(Name = "PART_OverlayBorder", Type = typeof(Border))]
public class DrawerHost : ContentControl
{
    public static readonly IndexToVisibilityConverter IndexToVisibilityConverterInstance =
        IndexToVisibilityConverter.Instance;

    /// <summary>
    /// 抽屉状态变更时触发，无论变更来源是绑定、Escape 还是点击遮罩。
    /// </summary>
    public event EventHandler<DrawerStateChangedEventArgs>? DrawerStateChanged;

    private Border? _drawerBorder;
    private Border? _overlayBorder;
    private TranslateTransform? _drawerTransform;
    private int _animationGeneration;

    public static readonly DependencyProperty IsDrawerOpenProperty =
        DependencyProperty.Register(
            nameof(IsDrawerOpen), typeof(bool), typeof(DrawerHost),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnIsDrawerOpenChanged));

    public bool IsDrawerOpen
    {
        get => (bool)GetValue(IsDrawerOpenProperty);
        set => SetValue(IsDrawerOpenProperty, value);
    }

    public static readonly DependencyProperty DrawerContentProperty =
        DependencyProperty.Register(
            nameof(DrawerContent), typeof(object), typeof(DrawerHost),
            new PropertyMetadata(null));

    public object? DrawerContent
    {
        get => GetValue(DrawerContentProperty);
        set => SetValue(DrawerContentProperty, value);
    }

    public static readonly DependencyProperty DrawerWidthProperty =
        DependencyProperty.Register(
            nameof(DrawerWidth), typeof(double), typeof(DrawerHost),
            new PropertyMetadata(320.0, null, CoerceDrawerWidth));

    public double DrawerWidth
    {
        get => (double)GetValue(DrawerWidthProperty);
        set => SetValue(DrawerWidthProperty, value);
    }

    private static object CoerceDrawerWidth(DependencyObject d, object baseValue)
    {
        double val = (double)baseValue;
        return val < 100 ? 100.0 : val;
    }

    public DrawerHost()
    {
        DefaultStyleKey = typeof(DrawerHost);
        KeyDown += OnKeyDown;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _drawerBorder = GetTemplateChild("PART_DrawerBorder") as Border;
        _overlayBorder = GetTemplateChild("PART_OverlayBorder") as Border;

        if (_overlayBorder != null)
            _overlayBorder.MouseLeftButtonDown += OnOverlayClicked;

        if (_drawerBorder != null)
        {
            _drawerTransform = new TranslateTransform(DrawerWidth, 0);
            _drawerBorder.RenderTransform = _drawerTransform;
        }

        SyncToState();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsDrawerOpen)
        {
            IsDrawerOpen = false;
            e.Handled = true;
        }
    }

    private void OnOverlayClicked(object sender, MouseButtonEventArgs e)
    {
        if (IsDrawerOpen)
            IsDrawerOpen = false;
    }

    private static void OnIsDrawerOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DrawerHost host)
        {
            host.SyncToState();
            host.DrawerStateChanged?.Invoke(host, new DrawerStateChangedEventArgs((bool)e.NewValue));
        }
    }

    /// <summary>
    /// 根据 IsDrawerOpen 直接设置元素状态并运行动画。
    /// 不依赖 FillBehavior：动画完成后清除动画、设置基础值。
    /// </summary>
    private void SyncToState()
    {
        if (_drawerBorder == null || _overlayBorder == null || _drawerTransform == null) return;

        // 递增代次，旧动画的 Completed 回调将被丢弃
        var generation = ++_animationGeneration;

        // 先读取当前动画中的实际值，再清除动画
        var currentX = _drawerTransform.X;
        var currentOpacity = _overlayBorder.Opacity;

        // 停止所有进行中的动画，恢复基础值
        _drawerTransform.BeginAnimation(TranslateTransform.XProperty, null);
        _overlayBorder.BeginAnimation(OpacityProperty, null);

        var duration = new Duration(TimeSpan.FromMilliseconds(250));
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        if (IsDrawerOpen)
        {
            _drawerBorder.IsHitTestVisible = true;
            _overlayBorder.IsHitTestVisible = true;
            _overlayBorder.Visibility = Visibility.Visible;

            _drawerTransform.X = currentX;
            _overlayBorder.Opacity = currentOpacity;

            var slideIn = new DoubleAnimation(currentX, 0, duration) { EasingFunction = ease };
            var fadeIn = new DoubleAnimation(currentOpacity, 1, duration) { EasingFunction = ease };

            _drawerTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);
            _overlayBorder.BeginAnimation(OpacityProperty, fadeIn);
        }
        else
        {
            _drawerBorder.IsHitTestVisible = false;
            _overlayBorder.IsHitTestVisible = false;

            _drawerTransform.X = currentX;
            _overlayBorder.Opacity = currentOpacity;

            var slideOut = new DoubleAnimation(currentX, DrawerWidth, duration)
            {
                EasingFunction = ease,
                FillBehavior = FillBehavior.Stop
            };
            slideOut.Completed += (_, _) =>
            {
                if (generation != _animationGeneration) return;
                _drawerTransform.X = DrawerWidth;
                _overlayBorder.Visibility = Visibility.Collapsed;
            };

            var fadeOut = new DoubleAnimation(currentOpacity, 0, duration)
            {
                EasingFunction = ease,
                FillBehavior = FillBehavior.Stop
            };
            fadeOut.Completed += (_, _) =>
            {
                if (generation != _animationGeneration) return;
                _overlayBorder.Opacity = 0;
            };

            _drawerTransform.BeginAnimation(TranslateTransform.XProperty, slideOut);
            _overlayBorder.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
