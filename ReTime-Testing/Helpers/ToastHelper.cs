using System.Windows;
using ReTime_Testing.Models.UI;

namespace ReTime_Testing.Helpers;

public static class ToastHelper
{
    public static void ShowToast(this FrameworkElement control, ToastMessage message)
    {
        control.RaiseEvent(new ShowToastEventArgs(message));
    }

    public static ToastMessage ShowToast(this FrameworkElement control, string message)
    {
        var msg = new ToastMessage(message);
        control.RaiseEvent(new ShowToastEventArgs(msg));
        return msg;
    }

    public static ToastMessage ShowToast(this FrameworkElement control, string title, string message)
    {
        var msg = new ToastMessage(title, message);
        control.RaiseEvent(new ShowToastEventArgs(msg));
        return msg;
    }

    public static ToastMessage ShowSuccessToast(this FrameworkElement control, string message)
    {
        var msg = new ToastMessage(message) { Severity = ToastSeverity.Success };
        control.RaiseEvent(new ShowToastEventArgs(msg));
        return msg;
    }

    public static ToastMessage ShowSuccessToast(this FrameworkElement control, string title, string message)
    {
        var msg = new ToastMessage(title, message) { Severity = ToastSeverity.Success };
        control.RaiseEvent(new ShowToastEventArgs(msg));
        return msg;
    }

    public static ToastMessage ShowWarningToast(this FrameworkElement control, string message)
    {
        var msg = new ToastMessage(message) { Severity = ToastSeverity.Warning, Duration = TimeSpan.FromSeconds(7) };
        control.RaiseEvent(new ShowToastEventArgs(msg));
        return msg;
    }

    public static ToastMessage ShowWarningToast(this FrameworkElement control, string title, string message)
    {
        var msg = new ToastMessage(title, message) { Severity = ToastSeverity.Warning, Duration = TimeSpan.FromSeconds(7) };
        control.RaiseEvent(new ShowToastEventArgs(msg));
        return msg;
    }

    public static ToastMessage ShowErrorToast(this FrameworkElement control, string message)
    {
        var msg = new ToastMessage(message) { Severity = ToastSeverity.Error, Duration = TimeSpan.FromSeconds(10) };
        control.RaiseEvent(new ShowToastEventArgs(msg));
        return msg;
    }

    public static ToastMessage ShowErrorToast(this FrameworkElement control, string title, string message)
    {
        var msg = new ToastMessage(title, message) { Severity = ToastSeverity.Error, Duration = TimeSpan.FromSeconds(10) };
        control.RaiseEvent(new ShowToastEventArgs(msg));
        return msg;
    }

    public static ToastMessage ShowErrorToast(this FrameworkElement control, string title, Exception exception)
    {
        var msg = new ToastMessage(title, exception.Message) { Severity = ToastSeverity.Error, AutoClose = false };
        control.RaiseEvent(new ShowToastEventArgs(msg));
        return msg;
    }
}