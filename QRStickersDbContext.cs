using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace QRStickers;

/// <summary>
/// Entity Framework DbContext for QR Stickers with ASP.NET Identity
/// </summary>
public class QRStickersDbContext : IdentityDbContext<ApplicationUser>
{
    public QRStickersDbContext(DbContextOptions<QRStickersDbContext> options) : base(options) { }

    public DbSet<OAuthToken> OAuthTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure OAuthToken relationship with ApplicationUser
        modelBuilder.Entity<OAuthToken>()
            .HasOne(t => t.User)
            .WithMany(u => u.OAuthTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OAuthToken>()
            .HasIndex(t => t.UserId);
    }
}