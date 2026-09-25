using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.Academic;
using FeePlatform.Modules.Billing;
using FeePlatform.Modules.IdentityTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AcademicProgram = FeePlatform.Modules.Academic.Program;

namespace FeePlatform.Api.Tests;

/// <summary>
/// Proves global query filter tenant isolation (EF Core).
/// PostgreSQL RLS still needs a later Postgres-backed test when infra exists.
/// </summary>
public class TenantIsolationTests
{
    private static (ServiceProvider Sp, AppDbContext Db, ITenantContext Tenant) CreateContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITenantContext, TenantContext>();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"iso_{Guid.NewGuid():N}"));

        var sp = services.BuildServiceProvider();
        var tenant = sp.GetRequiredService<ITenantContext>();
        var db = sp.GetRequiredService<AppDbContext>();
        return (sp, db, tenant);
    }

    [Fact]
    public async Task QueryFilter_hides_other_tenant_rows()
    {
        var (sp, db, tenant) = CreateContext();
        await using (sp)
        {
            var tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var tenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            tenant.SetPlatformAdminBypass(true);
            db.Users.AddRange(
                new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    Email = "a@example.com",
                    PasswordHash = "x",
                    DisplayName = "A",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantB,
                    Email = "b@example.com",
                    PasswordHash = "x",
                    DisplayName = "B",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                });
            await db.SaveChangesAsync();
            tenant.Clear();
            db.ChangeTracker.Clear();

            tenant.SetTenant(tenantA);
            var forA = await db.Users.AsNoTracking().ToListAsync();
            Assert.Single(forA);
            Assert.Equal(tenantA, forA[0].TenantId);
            Assert.DoesNotContain(forA, u => u.TenantId == tenantB);

            tenant.Clear();
            tenant.SetTenant(tenantB);
            var forB = await db.Users.AsNoTracking().ToListAsync();
            Assert.Single(forB);
            Assert.Equal(tenantB, forB[0].TenantId);
        }
    }

    [Fact]
    public async Task QueryFilter_fails_closed_when_tenant_context_missing()
    {
        var (sp, db, tenant) = CreateContext();
        await using (sp)
        {
            var tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            tenant.SetPlatformAdminBypass(true);
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                Email = "a@example.com",
                PasswordHash = "x",
                DisplayName = "A",
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
            tenant.Clear();
            db.ChangeTracker.Clear();

            Assert.Null(tenant.TenantId);
            Assert.False(tenant.IsPlatformAdminBypass);
            var visible = await db.Users.AsNoTracking().ToListAsync();
            Assert.Empty(visible);
        }
    }

    [Fact]
    public async Task QueryFilter_hides_other_tenant_students_and_invoices()
    {
        var (sp, db, tenant) = CreateContext();
        await using (sp)
        {
            var tenantA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var tenantB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var programA = Guid.NewGuid();
            var programB = Guid.NewGuid();
            var studentA = Guid.NewGuid();
            var studentB = Guid.NewGuid();
            var termA = Guid.NewGuid();

            tenant.SetPlatformAdminBypass(true);
            db.Programs.AddRange(
                new AcademicProgram { Id = programA, TenantId = tenantA, Code = "BSCS", Name = "CS", CreatedAtUtc = DateTimeOffset.UtcNow },
                new AcademicProgram { Id = programB, TenantId = tenantB, Code = "BSCS", Name = "CS", CreatedAtUtc = DateTimeOffset.UtcNow });
            db.Terms.Add(new Term
            {
                Id = termA,
                TenantId = tenantA,
                Code = "FALL-2026",
                Name = "Fall 2026",
                StartDate = new DateOnly(2026, 9, 1),
                EndDate = new DateOnly(2027, 1, 1),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            db.Students.AddRange(
                new Student
                {
                    Id = studentA,
                    TenantId = tenantA,
                    ExternalId = "STU-A",
                    DisplayName = "A",
                    ProgramId = programA,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                },
                new Student
                {
                    Id = studentB,
                    TenantId = tenantB,
                    ExternalId = "STU-B",
                    DisplayName = "B",
                    ProgramId = programB,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                });
            db.Invoices.AddRange(
                new Invoice
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantA,
                    StudentId = studentA,
                    TermId = termA,
                    Status = InvoiceStatuses.Issued,
                    TotalAmount = 100m,
                    RemainingBalance = 100m,
                    DueDate = new DateOnly(2026, 10, 1),
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    IssuedAtUtc = DateTimeOffset.UtcNow,
                },
                new Invoice
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantB,
                    StudentId = studentB,
                    TermId = Guid.NewGuid(),
                    Status = InvoiceStatuses.Issued,
                    TotalAmount = 200m,
                    RemainingBalance = 200m,
                    DueDate = new DateOnly(2026, 10, 1),
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    IssuedAtUtc = DateTimeOffset.UtcNow,
                });
            await db.SaveChangesAsync();
            tenant.Clear();
            db.ChangeTracker.Clear();

            tenant.SetTenant(tenantA);
            Assert.Single(await db.Students.AsNoTracking().ToListAsync());
            Assert.Single(await db.Invoices.AsNoTracking().ToListAsync());
            Assert.Equal(studentA, (await db.Students.AsNoTracking().SingleAsync()).Id);
        }
    }
}
