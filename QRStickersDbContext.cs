using Microsoft.EntityFrameworkCore;

/// <summary>
/// Entity Framework DbContext for QR Stickers
/// </summary>
public class QRStickersDbContext : DbContext
{
    public QRStickersDbContext(DbContextOptions<QRStickersDbContext> options) : base(options) { }

    public DbSet<OAuthToken> OAuthTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OAuthToken>()
            .HasIndex(t => t.UserId);
    }
}