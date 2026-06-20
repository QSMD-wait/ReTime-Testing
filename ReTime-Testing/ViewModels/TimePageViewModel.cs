using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using System.Windows.Threading;

namespace ReTime_Testing.ViewModels;

/// <summary>
/// NTP服务器选项
/// </summary>
public class NtpServerOption
{
    public string DisplayName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

/// <summary>
/// 时间设置页面 ViewModel
/// </summary>
public partial class TimePageViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly DispatcherTimer _timer;
    private readonly ITimeService? _timeService;
    private readonly ITimeCalibrationService? _timeCalibrationService;
    private TimeTopSetting _setting;
    private bool _isInitializing = true;

    [ObservableProperty]
    private string _currentTime = string.Empty;

    [ObservableProperty]
    private string _currentDate = string.Empty;

    [ObservableProperty]
    private bool _isCalibrationEnabled = false;

    [ObservableProperty]
    private bool _isCloudCalibrationEnabled = false;

    [ObservableProperty]
    private NtpServerOption? _selectedNtpServer = null;

    [ObservableProperty]
    private string _syncInfoText = "立即同步云端时间";

    [ObservableProperty]
    private double _userOffsetSeconds = 0;

    public List<NtpServerOption> NtpServers { get; } = new()
    {
        new NtpServerOption { DisplayName = "阿里云公共NTP", Address = NtpServerDefaults.Servers[0] },
        new NtpServerOption { DisplayName = "国家授时中心NTP", Address = NtpServerDefaults.Servers[1] },
        new NtpServerOption { DisplayName = "Microsoft NTP", Address = NtpServerDefaults.Servers[2] }
    };

    public TimePageViewModel(ISettingsService settingsService, ITimeService? timeService = null, ITimeCalibrationService? timeCalibrationService = null)
    {
        _settingsService = settingsService;
        _timeService = timeService;
        _timeCalibrationService = timeCalibrationService;
        _setting = _settingsService.GetTimeTopSetting();

        LoadSettings();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        UpdateTime();

        _isInitializing = false;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateTime();
    }

    private void UpdateTime()
    {
        var now = _timeService?.GetCurrentTime() ?? DateTime.Now;
        CurrentTime = now.ToString("HH:mm:ss");
        CurrentDate = now.ToString("yyyy年MM月dd日 dddd");
    }

    [RelayCommand]
    private async Task CalibrateNow()
    {
        if (_timeCalibrationService == null) return;

        if (!_timeCalibrationService.IsEnabled || !_timeCalibrationService.IsRunning)
        {
            SyncInfoText = "请先启用时间校准";
            return;
        }

        SyncInfoText = "正在同步...";

        try
        {
            var success = await _timeCalibrationService.CalibrateAsync();
            if (success)
            {
                var time = _timeCalibrationService.LastCalibrationTime;
                var rtt = _timeCalibrationService.LastRttMs;
                SyncInfoText = $"上次同步: {time:yyyy-MM-dd HH:mm:ss} · RTT {rtt:F0}ms";
            }
            else
            {
                SyncInfoText = $"同步失败 · 连续失败 {_timeCalibrationService.FailureCount} 次";
            }
        }
        catch
        {
            SyncInfoText = "同步异常";
        }
    }

    private void LoadSettings()
    {
        IsCalibrationEnabled = _setting.Calibration.Enabled;
        IsCloudCalibrationEnabled = _setting.Calibration.Source == CalibrationSource.Cloud;

        var address = _setting.Calibration.Cloud.SelectedServerAddress;
        SelectedNtpServer = NtpServers.FirstOrDefault(s => s.Address == address) ?? NtpServers[0];

        if (_timeCalibrationService != null && _timeCalibrationService.LastCalibrationTime != DateTime.MinValue)
        {
            var time = _timeCalibrationService.LastCalibrationTime;
            var rtt = _timeCalibrationService.LastRttMs;
            SyncInfoText = $"上次同步: {time:yyyy-MM-dd HH:mm:ss} · RTT {rtt:F0}ms";
        }

        UserOffsetSeconds = _setting.Calibration.UserOffsetSeconds;
    }

    private void SyncSettingsToService()
    {
        if (_timeCalibrationService == null) return;

        _timeCalibrationService.ApplyConfig(_setting.Calibration);
    }

    partial void OnIsCalibrationEnabledChanged(bool value)
    {
        if (_isInitializing) return;
        _setting.Calibration.Enabled = value;
        SaveSettings();
        SyncSettingsToService();
    }

    partial void OnIsCloudCalibrationEnabledChanged(bool value)
    {
        if (_isInitializing) return;
        _setting.Calibration.Source = value ? CalibrationSource.Cloud : CalibrationSource.System;
        SaveSettings();
        SyncSettingsToService();
    }

    partial void OnSelectedNtpServerChanged(NtpServerOption? value)
    {
        if (_isInitializing) return;
        if (value == null) return;

        _setting.Calibration.Cloud.SelectedServerAddress = value.Address;
        SaveSettings();
        SyncSettingsToService();
    }

    private bool _isClampingOffset;

    partial void OnUserOffsetSecondsChanged(double value)
    {
        if (_isInitializing || _isClampingOffset) return;

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            _isClampingOffset = true;
            UserOffsetSeconds = 0;
            _isClampingOffset = false;
            return;
        }

        var clamped = Math.Clamp(value, -86400, 86400);
        if (clamped != value)
        {
            _isClampingOffset = true;
            UserOffsetSeconds = clamped;
            _isClampingOffset = false;
            return;
        }

        _setting.Calibration.UserOffsetSeconds = clamped;
        SaveSettings();

        _timeService?.ApplyUserOffset(TimeSpan.FromSeconds(clamped));
    }

    private void SaveSettings()
    {
        _settingsService.SaveTimeTopSetting(_setting);
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Tick -= Timer_Tick;
    }

    public void ResumeTimer()
    {
        if (_timer != null && !_timer.IsEnabled)
        {
            _timer.Start();
            UpdateTime();
        }
    }

    public void PauseTimer()
    {
        _timer?.Stop();
    }
}