using QRStickers.Meraki;

namespace QRStickers;

/// <summary>
/// Factory for creating MerakiService instances with connection-specific context
/// </summary>
public class MerakiServiceFactory
{
    private readonly MerakiApiClient _apiClient;
    private readonly QRStickersDbContext _db;
    private readonly MerakiAccessTokenCache _tokenCache;
    private readonly ILogger<MerakiService> _logger;

    public MerakiServiceFactory(
        MerakiApiClient apiClient,
        QRStickersDbContext db,
        MerakiAccessTokenCache tokenCache,
        ILogger<MerakiService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a MerakiService instance for the specified connection
    /// </summary>
    public MerakiService CreateForConnection(int connectionId)
    {
        return new MerakiService(connectionId, _apiClient, _db, _tokenCache, _logger);
    }
}
