using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;

namespace QRStickers;

/// <summary>
/// Factory for creating MerakiService instances with connection-specific context
/// Returns DemoMerakiService for demo connections, MerakiService for real connections
/// </summary>
public class MerakiServiceFactory
{
    private readonly MerakiApiClient _apiClient;
    private readonly QRStickersDbContext _db;
    private readonly MerakiAccessTokenCache _tokenCache;
    private readonly ILogger<MerakiService> _merakiLogger;
    private readonly ILogger<DemoMerakiService> _demoLogger;

    public MerakiServiceFactory(
        MerakiApiClient apiClient,
        QRStickersDbContext db,
        MerakiAccessTokenCache tokenCache,
        ILogger<MerakiService> merakiLogger,
        ILogger<DemoMerakiService> demoLogger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
        _merakiLogger = merakiLogger ?? throw new ArgumentNullException(nameof(merakiLogger));
        _demoLogger = demoLogger ?? throw new ArgumentNullException(nameof(demoLogger));
    }

    /// <summary>
    /// Creates a MerakiService or DemoMerakiService instance for the specified connection
    /// Returns DemoMerakiService if connection.IsDemo is true, otherwise MerakiService
    /// </summary>
    public IMerakiService CreateForConnection(int connectionId)
    {
        // Check if this is a demo connection
        var connection = _db.Connections.AsNoTracking().FirstOrDefault(c => c.Id == connectionId);

        if (connection == null)
        {
            throw new InvalidOperationException($"Connection {connectionId} not found");
        }

        if (connection.IsDemo)
        {
            // Return demo service for demo connections
            return new DemoMerakiService(connectionId, _demoLogger);
        }

        // Return real service for non-demo connections
        return new MerakiService(connectionId, _apiClient, _db, _tokenCache, _merakiLogger);
    }
}
