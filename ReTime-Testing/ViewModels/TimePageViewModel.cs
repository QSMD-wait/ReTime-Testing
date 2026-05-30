using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReTime_Testing.Models;
using ReTime_Testing.Services;
using System.Windows.Threading;

namespace ReTime_Testing.ViewModels;

/// <summary>
/// 时间服务器选项
/// </summary>
public class TimeServerOption
{
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

/// <summary>
/// 时间设置页面 ViewModel
/// </summary>
public partial class TimePageViewModel : ObservableObject, IDisposable
{
    private readonly IConfigurationManager _configManager;
    private readonly DispatcherTimer _timer;
    private readonly ICloudCalibrationService? _cloudCalibrationService;
    private TimeTopSetting _setting;

    [ObservableProperty]
    private string _currentTime = string.Empty;

    [ObservableProperty]
    private string _currentDate = string.Empty;

    [ObservableProperty]
    private bool _isCalibrationEnabled = false;

    [ObservableProperty]
    private TimeServerOption? _selectedTimeServer = null;

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

    public List<TimeServerOption> TimeServers { get; } = new()
    {
        new TimeServerOption { DisplayName = "NTP协议 - 阿里云NTP", Type = "ntp", Address = "ntp.aliyun.com" },
        new TimeServerOption { DisplayName = "NTP协议 - 国家授时中心", Type = "ntp", Address = "ntp.ntsc.ac.cn" },
        new TimeServerOption { DisplayName = "NTP协议 - Windows时间", Type = "ntp", Address = "time.windows.com" },
        new TimeServerOption { DisplayName = "HTTP API - WorldTimeAPI", Type = "http", Address = "https://worldtimeapi.org/api/timezone/Etc/UTC" },
        new TimeServerOption { DisplayName = "HTTP API - TimeAPI.io", Type = "http", Address = "https://timeapi.io/api/Time/current/zone?timeZone=UTC" },
        new TimeServerOption { DisplayName = "HTTP API - TimeAPI.io (备用)", Type = "http", Address = "https://www.timeapi.io/api/Time/current/zone?timeZone=UTC" }
    };

    public TimePageViewModel(IConfigurationManager? configManager = null, ICloudCalibrationService? cloudCalibrationService = null)
    {
        _configManager = configManager ?? ConfigurationManager.Instance;
        _cloudCalibrationService = cloudCalibrationService;
        _setting = _configManager.LoadTimeTopSetting();

        LoadSettings();

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _timer.Tick += Timer_Tick;
        _timer.Start();

        UpdateTime();
        UpdateCalibrationStatus();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateTime();
    }

    private void UpdateTime()
    {
        var now = DateTime.Now;
        CurrentTime = now.ToString("HH:mm:ss");
        CurrentDate = now.ToString("yyyy年MM月dd日 dddd");
    }

    private void UpdateCalibrationStatus()
    {
        if (_cloudCalibrationService != null)
        {
            LastCalibrationTime = _cloudCalibrationService.LastCalibrationTime == DateTime.MinValue
                ? "从未校准"
                : _cloudCalibrationService.LastCalibrationTime.ToString("yyyy-MM-dd HH:mm:ss");

            if (_cloudCalibrationService.IsRunning)
            {
                CalibrationStatus = "运行中";
            }
            else if (_cloudCalibrationService.IsEnabled)
            {
                CalibrationStatus = "已启用（未运行）";
            }
            else
            {
                CalibrationStatus = "已禁用";
            }
        }
        else
        {
            CalibrationStatus = "服务未初始化";
        }
    }

    /// <summary>
    /// 立即校准
    /// </summary>
    [RelayCommand]
    private async Task CalibrateNow()
    {
        if (_cloudCalibrationService == null)
        {
            CalibrationStatus = "服务未初始化";
            return;
        }

        IsCalibrating = true;
        CalibrateButtonText = "校准中...";
        CalibrationStatus = "校准中...";

        try
        {
            var success = await _cloudCalibrationService.CalibrateAsync();
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

        var timeSourceType = _setting.Calibration.TimeSourceType.ToLower();
        var address = _setting.Calibration.SelectedServerAddress;

        SelectedTimeServer = TimeServers.FirstOrDefault(s =>
            s.Type == timeSourceType && s.Address == address) ?? TimeServers[0];
    }

    partial void OnIsCalibrationEnabledChanged(bool value)
    {
        _setting.Calibration.Enabled = value;
        SaveSettings();
    }

    partial void OnSelectedTimeServerChanged(TimeServerOption? value)
    {
        if (value == null) return;

        _setting.Calibration.TimeSourceType = value.Type;
        _setting.Calibration.SelectedServerAddress = value.Address;

        SaveSettings();
    }

    partial void OnIntervalSecondsChanged(int value)
    {
        _setting.Calibration.IntervalSeconds = value;
        SaveSettings();
    }

    partial void OnTriggerSecondsChanged(int value)
    {
        _setting.Calibration.TriggerSeconds = value;
        SaveSettings();
    }

    private void SaveSettings()
    {
        _configManager.SaveTimeTopSetting(_setting);
    }

    public void Dispose()
    {
        _timer?.Stop();
        _timer?.Tick -= Timer_Tick;
    }
}