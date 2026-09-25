using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.IdentityTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

            // Seed without filter (bypass) so both tenants land in the store.
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

            // No tenant + no platform bypass → filter requires TenantId match → empty (fail closed).
            Assert.Null(tenant.TenantId);
            Assert.False(tenant.IsPlatformAdminBypass);
            var visible = await db.Users.AsNoTracking().ToListAsync();
            Assert.Empty(visible);
        }
    }
}
