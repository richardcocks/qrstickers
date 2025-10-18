using System.Text.Json.Serialization;

namespace QRStickers;

public class Organization
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}