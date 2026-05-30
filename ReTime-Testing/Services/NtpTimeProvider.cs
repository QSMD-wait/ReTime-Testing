using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ReTime_Testing.Services;

/// <summary>
/// NTP时间提供者
/// 通过NTP协议获取时间
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

    public async Task<DateTime?> GetTimeAsync(TimeSpan timeout)
    {
        foreach (var serverAddress in _serverAddresses)
        {
            try
            {
                var ntpTime = await GetNtpTimeAsync(serverAddress, timeout);
                if (ntpTime.HasValue)
                {
                    Logger.Info("NtpTimeProvider",
                        $"成功从 {serverAddress} 获取NTP时间: {ntpTime.Value:yyyy-MM-dd HH:mm:ss.fff} (UTC)");
                    return ntpTime.Value;
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

    private async Task<DateTime?> GetNtpTimeAsync(string serverAddress, TimeSpan timeout)
    {
        using var udpClient = new UdpClient();
        udpClient.Client.ReceiveTimeout = (int)timeout.TotalMilliseconds;

        try
        {
            var ntpData = new byte[NtpPacketSize];

            ntpData[0] = 0x1B;

            var endPoint = new IPEndPoint(Dns.GetHostEntry(serverAddress).AddressList[0], NtpPort);
            await udpClient.SendAsync(ntpData, ntpData.Length, endPoint);

            var receiveResult = await udpClient.ReceiveAsync();
            var responseBytes = receiveResult.Buffer;

            if (responseBytes.Length >= NtpPacketSize)
            {
                var transmitTimestamp = ExtractTimestamp(responseBytes, 40);

                var ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var utcTime = ntpEpoch.AddSeconds(transmitTimestamp);

                return utcTime;
            }

            return null;
        }
        finally
        {
            udpClient.Close();
        }
    }

    private ulong ExtractTimestamp(byte[] bytes, int offset)
    {
        ulong seconds = 0;
        ulong fraction = 0;

        for (int i = 0; i < 4; i++)
        {
            seconds = (seconds << 8) | bytes[offset + i];
        }

        for (int i = 4; i < 8; i++)
        {
            fraction = (fraction << 8) | bytes[offset + i];
        }

        return seconds;
    }
}