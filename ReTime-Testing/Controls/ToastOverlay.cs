using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern.Controls;
using ReTime_Testing.Models.UI;

namespace ReTime_Testing.Controls;

[TemplatePart(Name = PartItemsControl, Type = typeof(ItemsControl))]
public class ToastOverlay : Control
{
    private const string PartItemsControl = "PART_ItemsControl";

    public ObservableCollection<ToastMessage> Messages { get; } = new();

    public static readonly RoutedEvent ShowToastEvent =
        EventManager.RegisterRoutedEvent(nameof(ShowToast), RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(ToastOverlay));

    public event RoutedEventHandler ShowToast
    {
        add => AddHandler(ShowToastEvent, value);
        remove => RemoveHandler(ShowToastEvent, value);
    }

    public IRelayCommand<ToastMessage> CloseToastCommand { get; }

    private FrameworkElement? _eventHost;

    static ToastOverlay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ToastOverlay),
            new FrameworkPropertyMetadata(typeof(ToastOverlay)));
    }

    public ToastOverlay()
    {
        CloseToastCommand = new RelayCommand<ToastMessage>(CloseToast);
    }

    public void AttachToHost(FrameworkElement host)
    {
        if (_eventHost != null)
        {
            _eventHost.RemoveHandler(ShowToastEvent, new RoutedEventHandler(OnShowToast));
        }
        _eventHost = host;
        host.AddHandler(ShowToastEvent, new RoutedEventHandler(OnShowToast));
    }

    private void OnShowToast(object sender, RoutedEventArgs e)
    {
        if (e is not ShowToastEventArgs args) return;
        e.Handled = true;

        var message = args.Message;

        Messages.Insert(0, message);

        message.ClosedCts.Token.Register(() =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                DispatcherTimerHelper.RunOnce(() => Messages.Remove(message), TimeSpan.FromSeconds(0.35));
            });
        });

        if (message.AutoClose)
        {
            DispatcherTimerHelper.RunOnce(() => message.Close(), message.Duration);
        }
    }

    public void CloseToast(ToastMessage? message)
    {
        message?.Close();
    }
}

public class ToastSeverityToInfoBarSeverityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ToastSeverity severity)
        {
            return severity switch
            {
                ToastSeverity.Success => InfoBarSeverity.Success,
                ToastSeverity.Warning => InfoBarSeverity.Warning,
                ToastSeverity.Error => InfoBarSeverity.Error,
                _ => InfoBarSeverity.Informational
            };
        }
        return InfoBarSeverity.Informational;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ToastSeverityToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ToastSeverity severity)
        {
            var key = severity == ToastSeverity.Informational
                ? iNKORE.UI.WPF.Modern.ThemeKeys.CardBackgroundFillColorDefaultBrushKey
                : iNKORE.UI.WPF.Modern.ThemeKeys.CardBackgroundFillColorSecondaryBrushKey;

            var brush = Application.Current.TryFindResource(key);
            if (brush != null) return brush;
        }
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

file static class DispatcherTimerHelper
{
    public static DispatcherTimer RunOnce(Action callback, TimeSpan interval)
    {
        var timer = new DispatcherTimer { Interval = interval };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            callback();
        };
        timer.Start();
        return timer;
    }
}