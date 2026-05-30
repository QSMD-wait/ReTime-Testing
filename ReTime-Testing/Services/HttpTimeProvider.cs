using System.Net.Http;
using System.Text.Json;

namespace ReTime_Testing.Services;

/// <summary>
/// HTTP时间提供者
/// 通过HTTP API获取时间
/// </summary>
public class HttpTimeProvider : ITimeProvider
{
    private readonly HttpClient _httpClient;
    private readonly string[] _serverUrls;

    public string Name => "HTTP API";
    public TimeProviderType Type => TimeProviderType.Http;

    public HttpTimeProvider(HttpClient httpClient, string[] serverUrls)
    {
        _httpClient = httpClient;
        _serverUrls = serverUrls;
    }

    public async Task<DateTime?> GetTimeAsync(TimeSpan timeout)
    {
        _httpClient.Timeout = timeout;

        foreach (var serverUrl in _serverUrls)
        {
            try
            {
                var response = await _httpClient.GetAsync(serverUrl);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var cloudTime = ParseTimeResponse(json, serverUrl);

                    if (cloudTime.HasValue)
                    {
                        Logger.Info("HttpTimeProvider",
                            $"成功从 {serverUrl} 获取云端时间: {cloudTime.Value:yyyy-MM-dd HH:mm:ss.fff}");
                        return cloudTime;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("HttpTimeProvider",
                    $"从 {serverUrl} 获取时间失败: {ex.Message}");
            }
        }

        Logger.Error("HttpTimeProvider", "所有HTTP时间服务器都失败");
        return null;
    }

    private DateTime? ParseTimeResponse(string json, string serverUrl)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("datetime", out var datetimeElement))
            {
                return DateTime.Parse(datetimeElement.GetString() ?? string.Empty);
            }
            else if (root.TryGetProperty("dateTime", out var dateTimeElement))
            {
                return DateTime.Parse(dateTimeElement.GetString() ?? string.Empty);
            }

            Logger.Warn("HttpTimeProvider", $"无法解析时间响应: {json.Substring(0, Math.Min(100, json.Length))}");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Warn("HttpTimeProvider", $"解析时间响应失败: {ex.Message}");
            return null;
        }
    }
}