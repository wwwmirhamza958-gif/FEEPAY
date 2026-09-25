using FeePlatform.Infrastructure.Outbox;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.Academic;
using FeePlatform.Modules.Audit;
using FeePlatform.Modules.Billing;
using FeePlatform.Modules.IdentityTenancy;
using FeePlatform.Modules.Integrations;
using Microsoft.EntityFrameworkCore;
using AcademicProgram = FeePlatform.Modules.Academic.Program;

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

    // Academic
    public DbSet<AcademicProgram> Programs => Set<AcademicProgram>();
    public DbSet<Intake> Intakes => Set<Intake>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    // Billing
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<InstalmentPlan> InstalmentPlans => Set<InstalmentPlan>();
    public DbSet<InstalmentPlanPart> InstalmentPlanParts => Set<InstalmentPlanPart>();
    public DbSet<Instalment> Instalments => Set<Instalment>();
    public DbSet<Deferment> Deferments => Set<Deferment>();
    public DbSet<StudentCredit> StudentCredits => Set<StudentCredit>();
    public DbSet<PreviewRun> PreviewRuns => Set<PreviewRun>();
    public DbSet<PreviewStudentResult> PreviewStudentResults => Set<PreviewStudentResult>();

    // Integrations
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ImportRowError> ImportRowErrors => Set<ImportRowError>();
    public DbSet<FieldMapping> FieldMappings => Set<FieldMapping>();
    public DbSet<ConnectorConfig> ConnectorConfigs => Set<ConnectorConfig>();
    public DbSet<SyncRun> SyncRuns => Set<SyncRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureIdentity(modelBuilder);
        ConfigureAcademic(modelBuilder);
        ConfigureBilling(modelBuilder);
        ConfigureIntegrations(modelBuilder);
    }

    private void ConfigureIdentity(ModelBuilder modelBuilder)
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
            if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
                e.Property(x => x.Id).UseIdentityAlwaysColumn();
            else
                e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.Action).HasMaxLength(128);
            e.Property(x => x.ActorType).HasMaxLength(64);
            e.Property(x => x.EntityType).HasMaxLength(128);
        });
    }

    private void ConfigureAcademic(ModelBuilder modelBuilder)
    {
        void TenantFilter<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e)
            where T : class, ITenantScoped
        {
            e.HasQueryFilter(x =>
                tenantContext.IsPlatformAdminBypass
                || (tenantContext.TenantId != null && x.TenantId == tenantContext.TenantId));
        }

        modelBuilder.Entity<AcademicProgram>(e =>
        {
            e.ToTable("programs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.ExternalId).HasMaxLength(128);
            TenantFilter(e);
        });

        modelBuilder.Entity<Intake>(e =>
        {
            e.ToTable("intakes");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.ExternalId).HasMaxLength(128);
            TenantFilter(e);
        });

        modelBuilder.Entity<Term>(e =>
        {
            e.ToTable("terms");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.ExternalId).HasMaxLength(128);
            TenantFilter(e);
        });

        modelBuilder.Entity<Course>(e =>
        {
            e.ToTable("courses");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(256);
            e.Property(x => x.ExternalId).HasMaxLength(128);
            e.Property(x => x.CreditHours).HasPrecision(18, 2);
            e.Property(x => x.Category).HasMaxLength(64);
            TenantFilter(e);
        });

        modelBuilder.Entity<Student>(e =>
        {
            e.ToTable("students");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.ExternalId }).IsUnique();
            e.Property(x => x.ExternalId).HasMaxLength(128);
            e.Property(x => x.DisplayName).HasMaxLength(256);
            TenantFilter(e);
        });

        modelBuilder.Entity<Guardian>(e =>
        {
            e.ToTable("guardians");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.ExternalId });
            e.Property(x => x.ExternalId).HasMaxLength(128);
            e.Property(x => x.DisplayName).HasMaxLength(256);
            e.Property(x => x.MobileE164).HasMaxLength(20);
            TenantFilter(e);
        });

        modelBuilder.Entity<StudentGuardian>(e =>
        {
            e.ToTable("student_guardians");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.StudentId, x.GuardianId }).IsUnique();
            e.Property(x => x.Relation).HasMaxLength(64);
            TenantFilter(e);
        });

        modelBuilder.Entity<Enrolment>(e =>
        {
            e.ToTable("enrolments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.StudentId, x.CourseId, x.TermId }).IsUnique();
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.ExternalId).HasMaxLength(128);
            TenantFilter(e);
        });
    }

    private void ConfigureBilling(ModelBuilder modelBuilder)
    {
        void TenantFilter<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e)
            where T : class, ITenantScoped
        {
            e.HasQueryFilter(x =>
                tenantContext.IsPlatformAdminBypass
                || (tenantContext.TenantId != null && x.TenantId == tenantContext.TenantId));
        }

        modelBuilder.Entity<Invoice>(e =>
        {
            e.ToTable("invoices");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.StudentId, x.TermId, x.Version });
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.RemainingBalance).HasPrecision(18, 2);
            e.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.InvoiceId);
            e.HasMany(x => x.Instalments).WithOne().HasForeignKey(i => i.InvoiceId);
            TenantFilter(e);
        });

        modelBuilder.Entity<InvoiceLine>(e =>
        {
            e.ToTable("invoice_lines");
            e.HasKey(x => x.Id);
            e.Property(x => x.FeeHeadCode).HasMaxLength(64);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Trace).HasMaxLength(1024);
            e.Property(x => x.Kind).HasMaxLength(32);
            e.Property(x => x.RuleRef).HasMaxLength(128);
            TenantFilter(e);
        });

        modelBuilder.Entity<InstalmentPlan>(e =>
        {
            e.ToTable("instalment_plans");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Name).HasMaxLength(256);
            e.HasMany(x => x.Parts).WithOne().HasForeignKey(p => p.InstalmentPlanId);
            TenantFilter(e);
        });

        modelBuilder.Entity<InstalmentPlanPart>(e =>
        {
            e.ToTable("instalment_plan_parts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Share).HasPrecision(18, 4);
            e.Property(x => x.Label).HasMaxLength(128);
            TenantFilter(e);
        });

        modelBuilder.Entity<Instalment>(e =>
        {
            e.ToTable("instalments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.PaidAmount).HasPrecision(18, 2);
            e.Property(x => x.Label).HasMaxLength(128);
            TenantFilter(e);
        });

        modelBuilder.Entity<Deferment>(e =>
        {
            e.ToTable("deferments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.ItemType).HasMaxLength(64);
            e.Property(x => x.Reason).HasMaxLength(1024);
            TenantFilter(e);
        });

        modelBuilder.Entity<StudentCredit>(e =>
        {
            e.ToTable("student_credits");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3);
            TenantFilter(e);
        });

        modelBuilder.Entity<PreviewRun>(e =>
        {
            e.ToTable("preview_runs");
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.HasMany(x => x.Results).WithOne().HasForeignKey(r => r.PreviewRunId);
            TenantFilter(e);
        });

        modelBuilder.Entity<PreviewStudentResult>(e =>
        {
            e.ToTable("preview_student_results");
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            TenantFilter(e);
        });
    }

    private void ConfigureIntegrations(ModelBuilder modelBuilder)
    {
        void TenantFilter<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e)
            where T : class, ITenantScoped
        {
            e.HasQueryFilter(x =>
                tenantContext.IsPlatformAdminBypass
                || (tenantContext.TenantId != null && x.TenantId == tenantContext.TenantId));
        }

        modelBuilder.Entity<ImportJob>(e =>
        {
            e.ToTable("import_jobs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Entity).HasMaxLength(64);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.OriginalFileName).HasMaxLength(512);
            e.Property(x => x.ContentType).HasMaxLength(128);
            e.HasMany(x => x.RowErrors).WithOne().HasForeignKey(r => r.ImportJobId);
            TenantFilter(e);
        });

        modelBuilder.Entity<ImportRowError>(e =>
        {
            e.ToTable("import_row_errors");
            e.HasKey(x => x.Id);
            e.Property(x => x.Field).HasMaxLength(128);
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.Message).HasMaxLength(1024);
            TenantFilter(e);
        });

        modelBuilder.Entity<FieldMapping>(e =>
        {
            e.ToTable("field_mappings");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Entity }).IsUnique();
            e.Property(x => x.Entity).HasMaxLength(64);
            TenantFilter(e);
        });

        modelBuilder.Entity<ConnectorConfig>(e =>
        {
            e.ToTable("connector_configs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            e.Property(x => x.Code).HasMaxLength(64);
            e.Property(x => x.DisplayName).HasMaxLength(256);
            TenantFilter(e);
        });

        modelBuilder.Entity<SyncRun>(e =>
        {
            e.ToTable("sync_runs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Direction).HasMaxLength(16);
            e.Property(x => x.Entity).HasMaxLength(64);
            TenantFilter(e);
        });
    }
}
