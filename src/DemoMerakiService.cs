using QRStickers.Services;

namespace QRStickers;

/// <summary>
/// Demo version of MerakiService that returns fake/placeholder data
/// Used for demo accounts to showcase functionality without real API calls
/// Implements same interface as MerakiService so orchestrator is unaware
/// </summary>
public class DemoMerakiService : IMerakiService
{
    private readonly int _connectionId;
    private readonly ILogger<DemoMerakiService> _logger;

    public DemoMerakiService(
        int connectionId,
        ILogger<DemoMerakiService> logger)
    {
        _connectionId = connectionId;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns fake organizations for demo purposes
    /// </summary>
    public Task<List<Organization>?> GetOrganizationsAsync()
    {
        _logger.LogInformation("Returning demo organizations for connection {ConnectionId}", _connectionId);
        var orgs = DemoDataGenerator.GenerateOrganizations();
        return Task.FromResult<List<Organization>?>(orgs);
    }

    /// <summary>
    /// Returns fake networks for demo purposes
    /// </summary>
    public Task<List<Network>?> GetNetworksAsync(string organizationId)
    {
        _logger.LogInformation("Returning demo networks for organization {OrgId} (connection {ConnectionId})", organizationId, _connectionId);
        var networks = DemoDataGenerator.GenerateNetworks(organizationId);
        return Task.FromResult<List<Network>?>(networks);
    }

    /// <summary>
    /// Returns fake devices for demo purposes
    /// </summary>
    public Task<List<Device>?> GetOrganizationDevicesAsync(string organizationId)
    {
        _logger.LogInformation("Returning demo devices for organization {OrgId} (connection {ConnectionId})", organizationId, _connectionId);
        var devices = DemoDataGenerator.GenerateDevices(organizationId);
        return Task.FromResult<List<Device>?>(devices);
    }
}
