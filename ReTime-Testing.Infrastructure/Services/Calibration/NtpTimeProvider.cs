using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace ReTime_Testing.Services;

/// <summary>
/// NTP时间提供者
/// 通过NTP协议获取时间，支持RTT计算和亚秒精度
/// </summary>
public class NtpTimeProvider : ITimeProvider
{
        private static readonly ILogger<NtpTimeProvider> _logger = AppLog.For<NtpTimeProvider>();
    private readonly string[] _serverAddresses;
    private const int NtpPort = 123;
    private const int NtpPacketSize = 48;

    public string Name => "NTP";
    public TimeProviderType Type => TimeProviderType.Ntp;

    public NtpTimeProvider(string[] serverAddresses)
    {
        _serverAddresses = serverAddresses;
    }

    public async Task<TimeProviderResult?> GetTimeAsync(TimeSpan timeout)
    {
        var perServerTimeout = TimeSpan.FromTicks(timeout.Ticks / Math.Max(1, _serverAddresses.Length));

        foreach (var serverAddress in _serverAddresses)
        {
            try
            {
                var result = await GetNtpTimeWithRttAsync(serverAddress, perServerTimeout);
                if (result != null)
                {
                    _logger.LogDebug("成功从 {ServerAddress} 获取NTP时间: UTC={UtcTime:yyyy-MM-dd HH:mm:ss.fff}, RTT={Rtt:F1}ms", serverAddress, result.UtcTime, result.RoundTripTime.TotalMilliseconds);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "从 {ServerAddress} 获取NTP时间失败", serverAddress);
            }
        }

        _logger.LogError("所有NTP服务器都失败");

        return null;
    }

    /// <summary>
    /// 获取NTP时间并计算RTT
    /// 使用 CancellationTokenSource 实现真正的异步超时（Socket.ReceiveTimeout 仅对同步 Receive 有效）
    /// </summary>
    private async Task<TimeProviderResult?> GetNtpTimeWithRttAsync(string serverAddress, TimeSpan timeout)
    {
        using var udpClient = new UdpClient();
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            var ntpData = new byte[NtpPacketSize];
            // LI = 0, VN = 3, Mode = 3 (Client)
            ntpData[0] = 0x1B;

            var dnsResult = await Dns.GetHostEntryAsync(serverAddress).ConfigureAwait(false);
            if (dnsResult.AddressList.Length == 0)
            {
                _logger.LogWarning("DNS解析无结果: {ServerAddress}", serverAddress);
                return null;
            }

            var endPoint = new IPEndPoint(dnsResult.AddressList[0], NtpPort);

            var sendTimestamp = Stopwatch.GetTimestamp();

            await udpClient.SendAsync(ntpData, ntpData.Length, endPoint).ConfigureAwait(false);

            var receiveTask = udpClient.ReceiveAsync();
            var completedTask = await Task.WhenAny(receiveTask, Task.Delay(timeout, cts.Token)).ConfigureAwait(false);

            if (completedTask != receiveTask)
            {
                _logger.LogWarning("NTP请求超时: 服务器={ServerAddress}, 超时={Timeout:F0}ms", serverAddress, timeout.TotalMilliseconds);
                // observe the abandoned receive task to prevent UnobservedTaskException when UdpClient is disposed
                _ = receiveTask.ContinueWith(static t => _ = t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                return null;
            }

            var receiveResult = receiveTask.Result;

            var receiveTimestamp = Stopwatch.GetTimestamp();

            var responseBytes = receiveResult.Buffer;

            if (responseBytes.Length >= NtpPacketSize)
            {
                var transmitTimestampSeconds = ExtractTimestampSeconds(responseBytes, 40);
                var transmitTimestampFraction = ExtractTimestampFraction(responseBytes, 40);

                var ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                var utcTime = ntpEpoch.AddSeconds(transmitTimestampSeconds + transmitTimestampFraction);

                var rttTicks = receiveTimestamp - sendTimestamp;
                var rtt = TimeSpan.FromTicks(rttTicks);

                return new TimeProviderResult(utcTime, rtt);
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("NTP请求已取消: 服务器={ServerAddress}", serverAddress);
            return null;
        }
        finally
        {
            udpClient.Close();
        }
    }

    /// <summary>
    /// 提取NTP时间戳的整数秒部分
    /// </summary>
    private static double ExtractTimestampSeconds(byte[] bytes, int offset)
    {
        ulong seconds = 0;
        for (int i = 0; i < 4; i++)
        {
            seconds = (seconds << 8) | bytes[offset + i];
        }
        return (double)seconds;
    }

    /// <summary>
    /// 提取NTP时间戳的小数部分（亚秒精度）
    /// </summary>
    private static double ExtractTimestampFraction(byte[] bytes, int offset)
    {
        ulong fraction = 0;
        for (int i = 4; i < 8; i++)
        {
            fraction = (fraction << 8) | bytes[offset + i];
        }
        // fraction 是 32位无符号整数，表示 0 ~ (2^32-1) / 2^32 秒
        return (double)fraction / uint.MaxValue;
    }
}