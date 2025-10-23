using System.Text.Json.Serialization;

namespace QRStickers;

public class Network
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("organizationId")]
    public string OrganizationId { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("productTypes")]
    public List<string>? ProductTypes { get; set; }

    [JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
