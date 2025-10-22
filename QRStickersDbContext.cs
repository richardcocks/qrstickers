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
    public DbSet<StickerTemplate> StickerTemplates { get; set; } = null!;
    public DbSet<GlobalVariable> GlobalVariables { get; set; } = null!;
    public DbSet<TemplateDeviceModel> TemplateDeviceModels { get; set; } = null!;
    public DbSet<TemplateDeviceType> TemplateDeviceTypes { get; set; } = null!;
    public DbSet<ExportHistory> ExportHistory { get; set; } = null!;

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

        // Composite alternate key for navigation from CachedNetwork
        modelBuilder.Entity<CachedOrganization>()
            .HasAlternateKey(o => new { o.ConnectionId, o.OrganizationId });

        // Configure CachedNetwork relationships
        modelBuilder.Entity<CachedNetwork>()
            .HasOne(n => n.Connection)
            .WithMany()
            .HasForeignKey(n => n.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CachedNetwork>()
            .HasOne(n => n.Organization)
            .WithMany(o => o.Networks)
            .HasForeignKey(n => new { n.ConnectionId, n.OrganizationId })
            .HasPrincipalKey(o => new { o.ConnectionId, o.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete networks when org is deleted

        modelBuilder.Entity<CachedNetwork>()
            .HasIndex(n => n.ConnectionId);

        modelBuilder.Entity<CachedNetwork>()
            .HasIndex(n => new { n.ConnectionId, n.NetworkId })
            .IsUnique();

        // Composite alternate key for navigation from CachedDevice
        modelBuilder.Entity<CachedNetwork>()
            .HasAlternateKey(n => new { n.ConnectionId, n.NetworkId });

        // Configure CachedDevice relationships
        modelBuilder.Entity<CachedDevice>()
            .HasOne(d => d.Connection)
            .WithMany()
            .HasForeignKey(d => d.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CachedDevice>()
            .HasOne(d => d.Network)
            .WithMany(n => n.Devices)
            .HasForeignKey(d => new { d.ConnectionId, d.NetworkId })
            .HasPrincipalKey(n => new { n.ConnectionId, n.NetworkId })
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

        // Configure StickerTemplate relationships
        modelBuilder.Entity<StickerTemplate>()
            .HasOne(t => t.Connection)
            .WithMany()
            .HasForeignKey(t => t.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for StickerTemplate performance
        modelBuilder.Entity<StickerTemplate>()
            .HasIndex(t => t.ConnectionId);

        modelBuilder.Entity<StickerTemplate>()
            .HasIndex(t => t.ProductTypeFilter);

        modelBuilder.Entity<StickerTemplate>()
            .HasIndex(t => t.IsDefault);

        modelBuilder.Entity<StickerTemplate>()
            .HasIndex(t => t.IsRackMount);

        // Configure GlobalVariable relationships
        modelBuilder.Entity<GlobalVariable>()
            .HasOne(v => v.Connection)
            .WithMany()
            .HasForeignKey(v => v.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint on ConnectionId + VariableName (no duplicate variable names per connection)
        modelBuilder.Entity<GlobalVariable>()
            .HasIndex(v => new { v.ConnectionId, v.VariableName })
            .IsUnique();

        modelBuilder.Entity<GlobalVariable>()
            .HasIndex(v => v.ConnectionId);

        // Configure TemplateDeviceModel relationships
        modelBuilder.Entity<TemplateDeviceModel>()
            .HasOne(m => m.Template)
            .WithMany()
            .HasForeignKey(m => m.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: One template can have multiple models, but not duplicates
        modelBuilder.Entity<TemplateDeviceModel>()
            .HasIndex(m => new { m.TemplateId, m.DeviceModel })
            .IsUnique();

        modelBuilder.Entity<TemplateDeviceModel>()
            .HasIndex(m => m.TemplateId);

        modelBuilder.Entity<TemplateDeviceModel>()
            .HasIndex(m => m.DeviceModel);

        // Configure TemplateDeviceType relationships
        modelBuilder.Entity<TemplateDeviceType>()
            .HasOne(t => t.Template)
            .WithMany()
            .HasForeignKey(t => t.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: One template can have multiple types, but not duplicates
        modelBuilder.Entity<TemplateDeviceType>()
            .HasIndex(t => new { t.TemplateId, t.DeviceType })
            .IsUnique();

        modelBuilder.Entity<TemplateDeviceType>()
            .HasIndex(t => t.TemplateId);

        modelBuilder.Entity<TemplateDeviceType>()
            .HasIndex(t => t.DeviceType);

        // Configure ExportHistory relationships
        modelBuilder.Entity<ExportHistory>()
            .HasOne(h => h.User)
            .WithMany()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ExportHistory>()
            .HasOne(h => h.Template)
            .WithMany()
            .HasForeignKey(h => h.TemplateId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ExportHistory>()
            .HasOne(h => h.Device)
            .WithMany()
            .HasForeignKey(h => h.DeviceId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ExportHistory>()
            .HasOne(h => h.Connection)
            .WithMany()
            .HasForeignKey(h => h.ConnectionId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes for ExportHistory queries
        modelBuilder.Entity<ExportHistory>()
            .HasIndex(h => h.UserId);

        modelBuilder.Entity<ExportHistory>()
            .HasIndex(h => h.ExportedAt);

        modelBuilder.Entity<ExportHistory>()
            .HasIndex(h => h.TemplateId);

        modelBuilder.Entity<ExportHistory>()
            .HasIndex(h => h.DeviceId);

        modelBuilder.Entity<ExportHistory>()
            .HasIndex(h => h.ConnectionId);
    }
}