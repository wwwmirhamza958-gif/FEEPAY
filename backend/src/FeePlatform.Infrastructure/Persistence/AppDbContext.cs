using FeePlatform.Infrastructure.Outbox;
using FeePlatform.Modules.Audit;
using FeePlatform.Modules.IdentityTenancy;
using FeePlatform.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
    : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantBranding> TenantBrandings => Set<TenantBranding>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<PayerProfile> PayerProfiles => Set<PayerProfile>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(e =>
        {
            e.ToTable("tenants");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(256);
        });

        modelBuilder.Entity<TenantBranding>(e =>
        {
            e.ToTable("tenant_brandings");
            e.HasKey(x => x.TenantId);
            e.HasOne(x => x.Tenant).WithOne(x => x.Branding).HasForeignKey<TenantBranding>(x => x.TenantId);
            e.Property(x => x.BrandColourHex).HasMaxLength(16);
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            e.Property(x => x.Email).HasMaxLength(320);
            e.HasQueryFilter(x =>
                tenantContext.IsPlatformAdminBypass
                || (tenantContext.TenantId != null && x.TenantId == tenantContext.TenantId));
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.HasQueryFilter(x =>
                tenantContext.IsPlatformAdminBypass
                || (tenantContext.TenantId != null && x.TenantId == tenantContext.TenantId));
        });

        modelBuilder.Entity<Permission>(e =>
        {
            e.ToTable("permissions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(128);
        });

        modelBuilder.Entity<RolePermission>(e =>
        {
            e.ToTable("role_permissions");
            e.HasKey(x => new { x.RoleId, x.PermissionId });
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(x => new { x.UserId, x.RoleId });
        });

        modelBuilder.Entity<OtpChallenge>(e =>
        {
            e.ToTable("otp_challenges");
            e.HasKey(x => x.Id);
            e.Property(x => x.MobileE164).HasMaxLength(20);
            e.HasIndex(x => new { x.MobileE164, x.CreatedAtUtc });
        });

        modelBuilder.Entity<Session>(e =>
        {
            e.ToTable("sessions");
            e.HasKey(x => x.Id);
        });

        modelBuilder.Entity<PayerProfile>(e =>
        {
            e.ToTable("payer_profiles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MobileE164).IsUnique();
            e.Property(x => x.MobileE164).HasMaxLength(20);
        });

        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProcessedAtUtc);
            e.Property(x => x.EventName).HasMaxLength(200).HasColumnName("event_name");
        });

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.ToTable("audit_events");
            e.HasKey(x => x.Id);
            // IdentityAlways is PostgreSQL-specific; InMemory/tests use ValueGeneratedOnAdd.
            if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                e.Property(x => x.Id).UseIdentityAlwaysColumn();
            else
                e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Action).HasMaxLength(128);
            e.Property(x => x.ActorType).HasMaxLength(64);
            e.Property(x => x.EntityType).HasMaxLength(128);
            // Append-only enforced in app code + DB trigger stub (see migrations/notes).
        });
    }
}
