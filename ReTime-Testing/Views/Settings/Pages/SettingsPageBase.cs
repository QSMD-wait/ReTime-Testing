using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReTime_Testing.Views.Settings.Pages;

public abstract class SettingsPageBase : UserControl
{
    public static readonly RoutedCommand RequestRestartCommand = new(nameof(RequestRestartCommand), typeof(SettingsPageBase));

    public static readonly RoutedCommand OpenDrawerCommand = new(nameof(OpenDrawerCommand), typeof(SettingsPageBase));

    public static readonly RoutedCommand CloseDrawerCommand = new(nameof(CloseDrawerCommand), typeof(SettingsPageBase));

    public static readonly string DialogHostIdentifier = "SettingsWindow";

    private SettingsNavigationContext? _navigationContext;

    public SettingsNavigationContext? NavigationContext
    {
        get => _navigationContext;
        internal set
        {
            if (_navigationContext != null && value != _navigationContext)
            {
                OnPageNavigatedFrom(_navigationContext);
            }

            _navigationContext = value;

            if (value != null)
            {
                OnPageNavigatedTo(value);
            }
        }
    }

    protected SettingsPageBase()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    protected virtual void OnPageNavigatedTo(SettingsNavigationContext context) { }

    protected virtual void OnPageNavigatedFrom(SettingsNavigationContext context) { }

    protected virtual void OnPageLoaded() { }

    protected virtual void OnPageUnloaded() { }

    protected void RequestRestart()
    {
        RequestRestartCommand.Execute(null, this);
    }

    protected void OpenDrawer(object content, object? dataContext = null)
    {
        if (content is FrameworkElement element && dataContext != null)
        {
            element.DataContext = dataContext;
        }

        OpenDrawerCommand.Execute(content, this);
    }

    protected void CloseDrawer()
    {
        CloseDrawerCommand.Execute(null, this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        OnPageLoaded();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        OnPageUnloaded();

        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

public class SettingsNavigationContext
{
    public string PageTag { get; init; } = string.Empty;

    public object? Parameter { get; init; }
}