using System.Text.Json.Serialization;

namespace ReTime_Testing.Core.Models.Theme;

public class ProgressBarThemeManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "default";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "默认";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("isBuiltIn")]
    public bool IsBuiltIn { get; set; } = true;

    [JsonPropertyName("minAppVersion")]
    public string? MinAppVersion { get; set; }

    [JsonPropertyName("supportsSettings")]
    public ThemeSupportsSettings SupportsSettings { get; set; } = new();
}

public class ThemeSupportsSettings
{
    [JsonPropertyName("cornerRadius")]
    public bool CornerRadius { get; set; } = true;

    [JsonPropertyName("glow")]
    public ThemeGlowSupport Glow { get; set; } = new() { Enabled = true, Color = true };

    [JsonPropertyName("gradient")]
    public ThemeGradientSupport Gradient { get; set; } = new() { Enabled = false, StartColor = false, EndColor = false };

    [JsonPropertyName("scale")]
    public bool Scale { get; set; } = true;

    [JsonPropertyName("trackColor")]
    public bool TrackColor { get; set; } = true;
}

public class ThemeGlowSupport
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("color")]
    public bool Color { get; set; } = true;
}

public class ThemeGradientSupport
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("startColor")]
    public bool StartColor { get; set; } = false;

    [JsonPropertyName("endColor")]
    public bool EndColor { get; set; } = false;
}