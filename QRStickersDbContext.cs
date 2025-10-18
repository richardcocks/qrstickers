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
    public DbSet<CachedOrganization> CachedOrganizations { get; set; } = null!;
    public DbSet<CachedNetwork> CachedNetworks { get; set; } = null!;
    public DbSet<CachedDevice> CachedDevices { get; set; } = null!;
    public DbSet<SyncStatus> SyncStatuses { get; set; } = null!;

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

        // Configure CachedOrganization relationships
        modelBuilder.Entity<CachedOrganization>()
            .HasOne(o => o.User)
            .WithMany(u => u.CachedOrganizations)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CachedOrganization>()
            .HasIndex(o => o.UserId);

        modelBuilder.Entity<CachedOrganization>()
            .HasIndex(o => new { o.UserId, o.OrganizationId })
            .IsUnique();

        // Configure CachedNetwork relationships
        modelBuilder.Entity<CachedNetwork>()
            .HasOne(n => n.User)
            .WithMany(u => u.CachedNetworks)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CachedNetwork>()
            .HasOne(n => n.Organization)
            .WithMany(o => o.Networks)
            .HasForeignKey(n => n.OrganizationId)
            .HasPrincipalKey(o => o.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete networks when org is deleted

        modelBuilder.Entity<CachedNetwork>()
            .HasIndex(n => n.UserId);

        modelBuilder.Entity<CachedNetwork>()
            .HasIndex(n => new { n.UserId, n.NetworkId })
            .IsUnique();

        // Configure CachedDevice relationships
        modelBuilder.Entity<CachedDevice>()
            .HasOne(d => d.User)
            .WithMany(u => u.CachedDevices)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CachedDevice>()
            .HasOne(d => d.Network)
            .WithMany(n => n.Devices)
            .HasForeignKey(d => d.NetworkId)
            .HasPrincipalKey(n => n.NetworkId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete devices when network is deleted

        modelBuilder.Entity<CachedDevice>()
            .HasIndex(d => d.UserId);

        modelBuilder.Entity<CachedDevice>()
            .HasIndex(d => new { d.UserId, d.Serial })
            .IsUnique();

        // Configure SyncStatus relationship
        modelBuilder.Entity<SyncStatus>()
            .HasOne(s => s.User)
            .WithOne(u => u.SyncStatus)
            .HasForeignKey<SyncStatus>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}