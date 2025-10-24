namespace QRStickers.Meraki;

/// <summary>
/// Interface for Meraki service implementations (real and demo)
/// Allows MerakiService and DemoMerakiService to be used interchangeably
/// </summary>
public interface IMerakiService
{
    /// <summary>
    /// Get organizations for the authenticated user
    /// </summary>
    Task<List<Organization>?> GetOrganizationsAsync();

    /// <summary>
    /// Get networks for a specific organization
    /// </summary>
    Task<List<Network>?> GetNetworksAsync(string organizationId);

    /// <summary>
    /// Get devices for a specific organization
    /// </summary>
    Task<List<Device>?> GetOrganizationDevicesAsync(string organizationId);
}
