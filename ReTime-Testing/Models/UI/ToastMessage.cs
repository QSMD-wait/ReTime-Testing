using System.Threading;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ReTime_Testing.Models.UI;

public enum ToastSeverity
{
    Informational,
    Success,
    Warning,
    Error
}

public class ToastMessage : ObservableObject
{
    private bool _isOpen = true;

    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public ToastSeverity Severity { get; init; } = ToastSeverity.Informational;
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(5);
    public bool AutoClose { get; init; } = true;
    public bool CanUserClose { get; init; } = true;
    public object? ActionContent { get; init; }

    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (value == _isOpen) return;
            SetProperty(ref _isOpen, value);
            if (!value)
            {
                ClosedCts.Cancel();
            }
        }
    }

    public void Close()
    {
        IsOpen = false;
    }

    internal CancellationTokenSource ClosedCts { get; } = new();

    public ToastMessage() { }

    public ToastMessage(string message) : this("", message) { }

    public ToastMessage(string title, string message)
    {
        Title = title;
        Message = message;
    }
}