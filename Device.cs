using System.Text.Json.Serialization;

namespace QRStickers;

public class Device
{
    [JsonPropertyName("serial")]
    public string? Serial { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("networkId")]
    public string? NetworkId { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }
}
