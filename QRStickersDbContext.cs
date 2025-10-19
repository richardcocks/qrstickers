using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;

namespace QRStickers;

/// <summary>
/// Entity Framework DbContext for QR Stickers with ASP.NET Identity
/// </summary>
public class QRStickersDbContext : IdentityDbContext<ApplicationUser>
{
    public QRStickersDbContext(DbContextOptions<QRStickersDbContext> options) : base(options) { }

    public DbSet<Connection> Connections { get; set; } = null!;
    public DbSet<MerakiOAuthToken> MerakiOAuthTokens { get; set; } = null!;
    public DbSet<CachedOrganization> CachedOrganizations { get; set; } = null!;
    public DbSet<CachedNetwork> CachedNetworks { get; set; } = null!;
    public DbSet<CachedDevice> CachedDevices { get; set; } = null!;
    public DbSet<SyncStatus> SyncStatuses { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Connection hierarchy (Table-per-Hierarchy with discriminator)
        modelBuilder.Entity<Connection>()
            .HasDiscriminator<string>("ConnectionType")
            .HasValue<MerakiConnection>("Meraki");

        modelBuilder.Entity<Connection>()
            .HasOne(c => c.User)
            .WithMany(u => u.Connections)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Connection>()
            .HasIndex(c => c.UserId);

        // Configure MerakiOAuthToken relationship with Connection
        modelBuilder.Entity<MerakiOAuthToken>()
            .HasOne(t => t.Connection)
            .WithMany()
            .HasForeignKey(t => t.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MerakiOAuthToken>()
            .HasIndex(t => t.ConnectionId)
            .IsUnique(); // One token per connection

        // Configure CachedOrganization relationships
        modelBuilder.Entity<CachedOrganization>()
            .HasOne(o => o.Connection)
            .WithMany()
            .HasForeignKey(o => o.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CachedOrganization>()
            .HasIndex(o => o.ConnectionId);

        modelBuilder.Entity<CachedOrganization>()
            .HasIndex(o => new { o.ConnectionId, o.OrganizationId })
            .IsUnique();

        // Configure CachedNetwork relationships
        modelBuilder.Entity<CachedNetwork>()
            .HasOne(n => n.Connection)
            .WithMany()
            .HasForeignKey(n => n.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CachedNetwork>()
            .HasOne(n => n.Organization)
            .WithMany(o => o.Networks)
            .HasForeignKey(n => n.OrganizationId)
            .HasPrincipalKey(o => o.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete networks when org is deleted

        modelBuilder.Entity<CachedNetwork>()
            .HasIndex(n => n.ConnectionId);

        modelBuilder.Entity<CachedNetwork>()
            .HasIndex(n => new { n.ConnectionId, n.NetworkId })
            .IsUnique();

        // Configure CachedDevice relationships
        modelBuilder.Entity<CachedDevice>()
            .HasOne(d => d.Connection)
            .WithMany()
            .HasForeignKey(d => d.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CachedDevice>()
            .HasOne(d => d.Network)
            .WithMany(n => n.Devices)
            .HasForeignKey(d => d.NetworkId)
            .HasPrincipalKey(n => n.NetworkId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete devices when network is deleted

        modelBuilder.Entity<CachedDevice>()
            .HasIndex(d => d.ConnectionId);

        modelBuilder.Entity<CachedDevice>()
            .HasIndex(d => new { d.ConnectionId, d.Serial })
            .IsUnique();

        // Configure SyncStatus relationship (1-to-1 with Connection)
        modelBuilder.Entity<SyncStatus>()
            .HasOne(s => s.Connection)
            .WithMany()
            .HasForeignKey(s => s.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SyncStatus>()
            .HasIndex(s => s.ConnectionId)
            .IsUnique(); // One sync status per connection
    }
}