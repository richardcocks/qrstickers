using System.Collections.Concurrent;
using Meraki.Api;
using Microsoft.Extensions.Logging;

namespace QRStickers;

/// <summary>
/// Manages a pool of reusable MerakiClient instances, one per unique API key.
/// Thread-safe and suitable for dependency injection.
/// </summary>
public class MerakiClientPool : IDisposable
{
    private readonly ConcurrentDictionary<string, MerakiClient> _clients = new();
    private readonly ILogger<MerakiClientPool> _logger;
    private readonly Func<string, MerakiClientOptions> _optionsFactory;
    private bool _disposed;

    public MerakiClientPool(ILogger<MerakiClientPool> logger)
        : this(logger, CreateDefaultOptions)
    {
    }

    public MerakiClientPool(
        ILogger<MerakiClientPool> logger,
        Func<string, MerakiClientOptions> optionsFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _optionsFactory = optionsFactory ?? throw new ArgumentNullException(nameof(optionsFactory));
    }

    /// <summary>
    /// Gets or creates a MerakiClient for the specified API key.
    /// The client is cached and reused for subsequent calls with the same API key.
    /// </summary>
    /// <param name="apiKey">The Meraki API key (access token)</param>
    /// <returns>A reusable MerakiClient instance</returns>
    public MerakiClient GetClient(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MerakiClientPool));
        }

        return _clients.GetOrAdd(apiKey, key =>
        {
            _logger.LogInformation("Creating new MerakiClient for API key ending in ...{KeySuffix}",
                key.Substring(Math.Max(0, key.Length - 4)));

            var options = _optionsFactory(key);
            return new MerakiClient(options, _logger);
        });
    }

    /// <summary>
    /// Gets the number of cached clients
    /// </summary>
    public int ClientCount => _clients.Count;

    /// <summary>
    /// Removes and disposes a client for a specific API key.
    /// Useful when an API key is revoked or no longer needed.
    /// </summary>
    public bool RemoveClient(string apiKey)
    {
        if (_clients.TryRemove(apiKey, out var client))
        {
            _logger.LogInformation("Removing MerakiClient for API key ending in ...{KeySuffix}",
                apiKey.Substring(Math.Max(0, apiKey.Length - 4)));
            client.Dispose();
            return true;
        }
        return false;
    }

    private static MerakiClientOptions CreateDefaultOptions(string apiKey) => new()
    {
        ApiKey = apiKey,
        UserAgent = "QRStickers/1.0.0",
        HttpClientTimeoutSeconds = 600,
        MaxAttemptCount = 5
    };

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing MerakiClientPool with {Count} clients", _clients.Count);

        foreach (var kvp in _clients)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing MerakiClient for key ending in ...{KeySuffix}",
                    kvp.Key.Substring(Math.Max(0, kvp.Key.Length - 4)));
            }
        }

        _clients.Clear();
        _disposed = true;
    }
}
