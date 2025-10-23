using Microsoft.EntityFrameworkCore;

namespace QRStickers.Tests.Helpers;

/// <summary>
/// Factory for creating in-memory database contexts for testing
/// Each context is isolated with a unique database name
/// </summary>
public static class InMemoryDbContextFactory
{
    /// <summary>
    /// Creates a new QRStickersDbContext with an in-memory database
    /// Database is isolated per call (unique GUID-based name)
    /// </summary>
    public static QRStickersDbContext Create()
    {
        var options = new DbContextOptionsBuilder<QRStickersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging() // Helpful for debugging test failures
            .Options;

        var context = new QRStickersDbContext(options);

        // Ensure database is created
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates a context with a specific database name (for shared state tests)
    /// Use sparingly - prefer isolated databases for test independence
    /// </summary>
    public static QRStickersDbContext CreateWithName(string databaseName)
    {
        var options = new DbContextOptionsBuilder<QRStickersDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new QRStickersDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }
}
