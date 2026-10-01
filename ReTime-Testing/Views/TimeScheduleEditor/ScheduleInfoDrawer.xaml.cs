using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using ReTime_Testing.ViewModels.TimeScheduleEditor;

#nullable enable

namespace ReTime_Testing.Views.TimeScheduleEditor;

public partial class ScheduleInfoDrawer : UserControl
{
    private static readonly SolidColorBrush SecondaryBrush = new((Color)ColorConverter.ConvertFromString("#8B8B8B"));

    private TimeScheduleEditorViewModel? _viewModel;
    private ScheduleListItem? _schedule;
    private bool _isLoading;

    public event Action<string, string>? SaveCompleted;

    public ScheduleInfoDrawer()
    {
        InitializeComponent();
    }

    public void LoadSchedule(ScheduleListItem schedule)
    {
        _isLoading = true;
        _schedule = schedule;

        if (_viewModel == null)
        {
            var app = Application.Current as App;
            _viewModel = app?.Services.GetRequiredService<TimeScheduleEditorViewModel>();
        }

        var isEnabled = _viewModel!.GetScheduleRule(schedule.Id);

        NameBox.Text = schedule.Name;
        NameBox.IsReadOnly = true;
        NameEditButton.Visibility = Visibility.Visible;

        DescBox.Text = schedule.Description ?? "";
        DescBox.IsReadOnly = true;
        DescEditButton.Visibility = Visibility.Visible;

        EnableToggle.IsOn = isEnabled;

        IdBox.Text = schedule.Id;

        _isLoading = false;
    }

    private void OnNameEditClick(object sender, RoutedEventArgs e)
    {
        NameBox.IsReadOnly = false;
        NameBox.Focus();
        NameBox.SelectAll();
        NameEditButton.Visibility = Visibility.Collapsed;
    }

    private void OnDescEditClick(object sender, RoutedEventArgs e)
    {
        DescBox.IsReadOnly = false;
        DescBox.Focus();
        DescEditButton.Visibility = Visibility.Collapsed;
    }

    private void OnEnableToggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _schedule == null || _viewModel == null) return;
        Save();
    }

    private void OnFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading || _schedule == null || _viewModel == null) return;
        Save();
    }

    private void Save()
    {
        if (_schedule == null || _viewModel == null) return;

        string newName = NameBox.IsReadOnly ? _schedule.Name : (NameBox.Text?.Trim() ?? _schedule.Name);
        string? newDesc = DescBox.IsReadOnly ? _schedule.Description : (string.IsNullOrWhiteSpace(DescBox.Text) ? null : DescBox.Text.Trim());

        bool newIsEnabled = EnableToggle.IsOn;

        var scheduleManager = ((App)Application.Current).Services.GetRequiredService<ITimeScheduleManager>();
        var schedule = scheduleManager.LoadSchedule(_schedule.Id);
        if (schedule == null) return;

        schedule.Settings ??= new TimeScheduleSettings();
        schedule.Settings.Metadata ??= new TimeScheduleMetadata();
        schedule.Settings.Metadata.Name = newName;
        schedule.Settings.Metadata.Description = newDesc;
        schedule.Settings.Metadata.IsEnabled = newIsEnabled;
        schedule.Settings.Metadata.UpdatedAt = DateTime.UtcNow.ToString("o");
        scheduleManager.SaveSchedule(schedule);

        _schedule.Name = newName;
        _schedule.Description = newDesc;
        _schedule.IsEnabled = newIsEnabled;

        SaveCompleted?.Invoke(newName, _schedule.Id);
    }
}
