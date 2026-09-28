using FeePlatform.Modules.Academic.Seed;
using FeePlatform.Modules.IdentityTenancy;
using FeePlatform.Modules.IdentityTenancy.Seed;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Infrastructure.Seed;

/// <summary>
/// Official pilot seed runner (CONTRACT §5 / R-A-017).
/// Ensures Identity tenant row then applies AcademicPilotSeed content.
/// Never calls Database.Migrate() — callers must use InMemory or already-migrated stores.
/// </summary>
public static class DevSeed
{
    public static async Task EnsureAppliedAsync(DbContext db, CancellationToken ct = default)
    {
        if (!await db.Set<Tenant>().IgnoreQueryFilters().AnyAsync(t => t.Id == PilotSeed.TenantId, ct))
        {
            db.Set<Tenant>().Add(new Tenant
            {
                Id = PilotSeed.TenantId,
                Code = PilotSeed.Slug,
                Name = PilotSeed.DisplayName,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(ct);
        }

        await AcademicPilotSeed.EnsureAppliedAsync(db, ct);
    }
}
