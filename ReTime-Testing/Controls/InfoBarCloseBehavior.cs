using System.Windows;
using System.Windows.Input;
using iNKORE.UI.WPF.Modern.Controls;
using ReTime_Testing.Models.UI;

namespace ReTime_Testing.Controls;

public static class InfoBarCloseBehavior
{
    public static readonly DependencyProperty CloseCommandProperty =
        DependencyProperty.RegisterAttached(
            "CloseCommand",
            typeof(ICommand),
            typeof(InfoBarCloseBehavior),
            new PropertyMetadata(null, OnCloseCommandChanged));

    public static ICommand GetCloseCommand(DependencyObject obj) => (ICommand)obj.GetValue(CloseCommandProperty);
    public static void SetCloseCommand(DependencyObject obj, ICommand value) => obj.SetValue(CloseCommandProperty, value);

    private static void OnCloseCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InfoBar infoBar)
        {
            if (e.OldValue != null)
            {
                infoBar.CloseButtonClick -= OnCloseButtonClick;
            }
            if (e.NewValue != null)
            {
                infoBar.CloseButtonClick += OnCloseButtonClick;
            }
        }
    }

    private static void OnCloseButtonClick(InfoBar sender, object e)
    {
        var command = GetCloseCommand(sender);
        if (command?.CanExecute(sender.DataContext) == true)
        {
            command.Execute(sender.DataContext);
        }
    }
}