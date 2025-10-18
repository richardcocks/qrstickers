namespace QRStickers;

/// <summary>
/// Factory for creating MerakiService instances with user-specific context
/// </summary>
public class MerakiServiceFactory
{
    private readonly MerakiApiClient _apiClient;
    private readonly QRStickersDbContext _db;
    private readonly ILogger<MerakiService> _logger;

    public MerakiServiceFactory(
        MerakiApiClient apiClient,
        QRStickersDbContext db,
        ILogger<MerakiService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a MerakiService instance for the specified user
    /// </summary>
    public MerakiService CreateForUser(string userId)
    {
        return new MerakiService(userId, _apiClient, _db, _logger);
    }
}
