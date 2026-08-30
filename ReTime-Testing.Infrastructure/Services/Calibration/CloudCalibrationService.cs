using ReTime_Testing.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ReTime_Testing.Services;

/// <summary>
/// 云端校准服务（NTP数据源）
/// 仅负责从NTP服务器获取时间（含RTT补偿），不负责校准策略和调度
/// 校准策略和调度由 TimeCalibrationService 统一管理
/// </summary>
public class CloudCalibrationService : ICloudCalibrationService
{
        private readonly ILogger<CloudCalibrationService> _logger;
    private ITimeProvider? _currentTimeProvider;
    private List<string> _ntpServers = new();
    private int _selectedNtpServerIndex = 0;

    /// <summary>
    /// 当前使用的时间提供者名称
    /// </summary>
    public string CurrentProviderName => _currentTimeProvider?.Name ?? "未初始化";

    /// <summary>
    /// 上次请求的RTT（毫秒）
    /// </summary>
    public double LastRttMs { get; private set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public CloudCalibrationService(ILogger<CloudCalibrationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 初始化NTP时间提供者
    /// </summary>
    private void InitializeTimeProvider()
    {
        var orderedServers = new List<string>();
        if (_selectedNtpServerIndex >= 0 && _selectedNtpServerIndex < _ntpServers.Count)
        {
            orderedServers.Add(_ntpServers[_selectedNtpServerIndex]);
            foreach (var server in _ntpServers)
            {
                if (server != _ntpServers[_selectedNtpServerIndex])
                {
                    orderedServers.Add(server);
                }
            }
        }
        else
        {
            orderedServers.AddRange(_ntpServers);
        }
        _currentTimeProvider = new NtpTimeProvider(orderedServers.ToArray());

        _logger.LogInformation("NTP时间提供者已初始化: 提供者={Provider}, 服务器={Servers}", _currentTimeProvider.Name, string.Join(", ", orderedServers));
    }

    /// <summary>
    /// 配置NTP服务器
    /// </summary>
    /// <param name="ntpServers">NTP服务器列表</param>
    /// <param name="selectedNtpServerIndex">选中的NTP服务器索引</param>
    public void ConfigureNtpServers(List<string> ntpServers, int selectedNtpServerIndex = 0)
    {
        _ntpServers = ntpServers ?? NtpServerDefaults.Servers.ToList();
        _selectedNtpServerIndex = selectedNtpServerIndex;

        InitializeTimeProvider();

        _logger.LogInformation("NTP服务器已配置: 服务器={Servers}, 选中索引={SelectedIndex}", string.Join(", ", _ntpServers), selectedNtpServerIndex);
    }

    /// <summary>
    /// 获取云端时间（含RTT补偿信息）
    /// 将UTC时间转换为本地时间后返回
    /// </summary>
    /// <param name="timeout">请求超时时间</param>
    /// <returns>时间提供结果（含RTT），失败返回null</returns>
    public async Task<TimeProviderResult?> GetCloudTimeAsync(TimeSpan timeout)
    {
        if (_currentTimeProvider == null)
        {
            _logger.LogError("时间提供者未初始化");
            return null;
        }

        try
        {
            var result = await _currentTimeProvider.GetTimeAsync(timeout);

            if (result != null)
            {
                LastRttMs = result.RoundTripTime.TotalMilliseconds;

                _logger.LogDebug("获取云端时间成功: UTC={UtcTime:yyyy-MM-dd HH:mm:ss.fff}, RTT={Rtt:F1}ms", result.UtcTime, result.RoundTripTime.TotalMilliseconds);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取云端时间失败: {Message}", ex.Message);
            return null;
        }
    }
}