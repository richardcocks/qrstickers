namespace QRStickers.Meraki;

/// <summary>
/// Represents a Meraki OAuth connection for a user
/// Inherits from Connection base class using Table-per-Hierarchy pattern
/// </summary>
public class MerakiConnection : Connection
{
    public MerakiConnection()
    {
        ConnectionType = "Meraki";
    }

    // Future Meraki-specific properties can be added here
    // Examples:
    // - Meraki organization preferences
    // - API base URL overrides (for different Meraki shards)
    // - Webhook configurations
}
