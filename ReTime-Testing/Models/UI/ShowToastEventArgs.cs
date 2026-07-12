using System.Windows;
using ReTime_Testing.Controls;

namespace ReTime_Testing.Models.UI;

public class ShowToastEventArgs : RoutedEventArgs
{
    public ToastMessage Message { get; }

    public ShowToastEventArgs(ToastMessage message) : base(ToastOverlay.ShowToastEvent)
    {
        Message = message;
    }

    public ShowToastEventArgs(RoutedEvent routedEvent, ToastMessage message) : base(routedEvent)
    {
        Message = message;
    }
}