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
    private readonly IConfigurationManager _configManager;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _statusRefreshTimer;
    private readonly ITimeService? _timeService;
    private readonly ITimeCalibrationService? _timeCalibrationService;
    private TimeTopSetting _setting;

    [ObservableProperty]
    private string _currentTime = string.Empty;

    [ObservableProperty]
    private string _currentDate = string.Empty;

    [ObservableProperty]
    private bool _isCalibrationEnabled = false;

    [ObservableProperty]
    private NtpServerOption? _selectedNtpServer = null;

    [ObservableProperty]
    private int _intervalSeconds = 300;

    [ObservableProperty]
    private int _triggerSeconds = 5;

    [ObservableProperty]
    private bool _isCalibrating = false;

    [ObservableProperty]
    private string _lastCalibrationTime = "从未校准";

    [ObservableProperty]
    private string _calibrationStatus = "就绪";

    [ObservableProperty]
    private string _calibrateButtonText = "立即校准";

    [ObservableProperty]
    private string _calibrationInfo = string.Empty;

    public List<NtpServerOption> NtpServers { get; } = new()
    {
        new NtpServerOption { DisplayName = "阿里云NTP", Address = NtpServerDefaults.Servers[0] },
        new NtpServerOption { DisplayName = "国家授时中心", Address = NtpServerDefaults.Servers[1] },
        new NtpServerOption { DisplayName = "Windows时间", Address = NtpServerDefaults.Servers[2] }
    };

    public TimePageViewModel(IConfigurationManager? configManager = null, ITimeService? timeService = null, ITimeCalibrationService? timeCalibrationService = null)
    {
        _configManager = configManager ?? ConfigurationManager.Instance;
        _timeService = timeService;
        _timeCalibrationService = timeCalibrationService;
        _setting = _configManager.LoadTimeTopSetting();

        LoadSettings();

        // 时间显示定时器（100ms刷新）
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        // 校准状态刷新定时器（2秒刷新）
        _statusRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _statusRefreshTimer.Tick += StatusRefreshTimer_Tick;
        _statusRefreshTimer.Start();

        UpdateTime();
        UpdateCalibrationStatus();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateTime();
    }

    private void StatusRefreshTimer_Tick(object? sender, EventArgs e)
    {
        UpdateCalibrationStatus();
    }

    private void UpdateTime()
    {
        // 优先使用校准后的绝对时间，回退到系统时间
        var now = _timeService?.GetCurrentTime() ?? DateTime.Now;
        CurrentTime = now.ToString("HH:mm:ss");
        CurrentDate = now.ToString("yyyy年MM月dd日 dddd");
    }

    private void UpdateCalibrationStatus()
    {
        if (_timeCalibrationService != null)
        {
            LastCalibrationTime = _timeCalibrationService.LastCalibrationTime == DateTime.MinValue
                ? "从未校准"
                : _timeCalibrationService.LastCalibrationTime.ToString("yyyy-MM-dd HH:mm:ss");

            if (_timeCalibrationService.IsRunning)
            {
                CalibrationStatus = "运行中";
            }
            else if (_timeCalibrationService.IsEnabled)
            {
                CalibrationStatus = "已启用（未运行）";
            }
            else
            {
                CalibrationStatus = "已禁用";
            }

            // 显示详细校准信息
            var rttInfo = _timeCalibrationService.CurrentSource == CalibrationSource.Cloud
                ? $" | RTT: {_timeCalibrationService.LastRttMs:F0}ms"
                : "";
            CalibrationInfo = $"源: {_timeCalibrationService.CurrentProviderName}{rttInfo} | 失败: {_timeCalibrationService.FailureCount}次";
        }
        else
        {
            CalibrationStatus = "服务未初始化";
            CalibrationInfo = string.Empty;
        }
    }

    /// <summary>
    /// 立即校准
    /// </summary>
    [RelayCommand]
    private async Task CalibrateNow()
    {
        if (_timeCalibrationService == null)
        {
            CalibrationStatus = "服务未初始化";
            return;
        }

        IsCalibrating = true;
        CalibrateButtonText = "校准中...";
        CalibrationStatus = "校准中...";

        try
        {
            var success = await _timeCalibrationService.CalibrateAsync();
            CalibrationStatus = success ? "校准成功" : "校准失败";
            UpdateCalibrationStatus();
        }
        catch (Exception ex)
        {
            CalibrationStatus = $"校准异常: {ex.Message}";
        }
        finally
        {
            IsCalibrating = false;
            CalibrateButtonText = "立即校准";
        }
    }

    private void LoadSettings()
    {
        IsCalibrationEnabled = _setting.Calibration.Enabled;
        IntervalSeconds = _setting.Calibration.IntervalSeconds;
        TriggerSeconds = _setting.Calibration.TriggerSeconds;

        var address = _setting.Calibration.Cloud.SelectedServerAddress;
        SelectedNtpServer = NtpServers.FirstOrDefault(s => s.Address == address) ?? NtpServers[0];
    }

    /// <summary>
    /// 将当前设置同步到运行中的 TimeCalibrationService 实例
    /// </summary>
    private void SyncSettingsToService()
    {
        if (_timeCalibrationService == null) return;

        _timeCalibrationService.ApplyConfig(_setting.Calibration);
    }

    partial void OnIsCalibrationEnabledChanged(bool value)
    {
        _setting.Calibration.Enabled = value;
        SaveSettings();
        SyncSettingsToService();
    }

    partial void OnSelectedNtpServerChanged(NtpServerOption? value)
    {
        if (value == null) return;

        _setting.Calibration.Cloud.SelectedServerAddress = value.Address;
        SaveSettings();
        SyncSettingsToService();
    }

    partial void OnIntervalSecondsChanged(int value)
    {
        _setting.Calibration.IntervalSeconds = value;
        SaveSettings();
        SyncSettingsToService();
    }

    partial void OnTriggerSecondsChanged(int value)
    {
        _setting.Calibration.TriggerSeconds = value;
        SaveSettings();
        SyncSettingsToService();
    }

    private void SaveSettings()
    {
        _configManager.SaveTimeTopSetting(_setting);
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Tick -= Timer_Tick;
        _statusRefreshTimer?.Stop();
        _statusRefreshTimer?.Tick -= StatusRefreshTimer_Tick;
    }
}