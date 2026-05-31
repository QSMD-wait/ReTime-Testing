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
        foreach (var serverAddress in _serverAddresses)
        {
            try
            {
                var result = await GetNtpTimeWithRttAsync(serverAddress, timeout);
                if (result != null)
                {
                    Logger.Info("NtpTimeProvider",
                        $"成功从 {serverAddress} 获取NTP时间: UTC={result.UtcTime:yyyy-MM-dd HH:mm:ss.fff}, RTT={result.RoundTripTime.TotalMilliseconds:F1}ms");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("NtpTimeProvider",
                    $"从 {serverAddress} 获取NTP时间失败: {ex.Message}");
            }
        }

        Logger.Error("NtpTimeProvider", "所有NTP服务器都失败");
        return null;
    }

    /// <summary>
    /// 获取NTP时间并计算RTT
    /// </summary>
    private async Task<TimeProviderResult?> GetNtpTimeWithRttAsync(string serverAddress, TimeSpan timeout)
    {
        using var udpClient = new UdpClient();
        udpClient.Client.ReceiveTimeout = (int)timeout.TotalMilliseconds;
        udpClient.Client.SendTimeout = (int)timeout.TotalMilliseconds;

        try
        {
            var ntpData = new byte[NtpPacketSize];
            // LI = 0, VN = 3, Mode = 3 (Client)
            ntpData[0] = 0x1B;

            var endPoint = new IPEndPoint(Dns.GetHostEntry(serverAddress).AddressList[0], NtpPort);

            // 记录请求发送时间
            var sendTimestamp = Stopwatch.GetTimestamp();

            await udpClient.SendAsync(ntpData, ntpData.Length, endPoint);

            var receiveResult = await udpClient.ReceiveAsync();

            // 记录响应接收时间
            var receiveTimestamp = Stopwatch.GetTimestamp();

            var responseBytes = receiveResult.Buffer;

            if (responseBytes.Length >= NtpPacketSize)
            {
                // 解析NTP时间戳（使用完整精度，包含fraction部分）
                var transmitTimestampSeconds = ExtractTimestampSeconds(responseBytes, 40);
                var transmitTimestampFraction = ExtractTimestampFraction(responseBytes, 40);

                var ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var utcTime = ntpEpoch.AddSeconds(transmitTimestampSeconds + transmitTimestampFraction);

                // 计算RTT
                var rttTicks = receiveTimestamp - sendTimestamp;
                var rtt = TimeSpan.FromTicks(rttTicks);

                return new TimeProviderResult(utcTime, rtt);
            }

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