using FeePlatform.Modules.Academic.Seed;
using FeePlatform.Modules.Billing;
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
    public static readonly Guid PilotInvoiceStudent1Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbb0001");

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
        await EnsurePilotBillingAsync(db, ct);
    }

    private static async Task EnsurePilotBillingAsync(DbContext db, CancellationToken ct)
    {
        var tenantId = PilotSeed.TenantId;
        var now = DateTimeOffset.UtcNow;

        if (!await db.Set<TenantBranding>().IgnoreQueryFilters()
                .AnyAsync(b => b.TenantId == tenantId, ct))
        {
            db.Set<TenantBranding>().Add(new TenantBranding
            {
                TenantId = tenantId,
                BrandColourHex = PilotSeed.BrandPrimaryHex,
            });
        }

        if (!await db.Set<Invoice>().IgnoreQueryFilters()
                .AnyAsync(i => i.Id == PilotInvoiceStudent1Id, ct))
        {
            var invoice = new Invoice
            {
                Id = PilotInvoiceStudent1Id,
                TenantId = tenantId,
                StudentId = AcademicPilotSeed.Student1Id,
                TermId = AcademicPilotSeed.TermFall2026Id,
                Version = 1,
                Status = InvoiceStatuses.Issued,
                TotalAmount = 71250.00m,
                RemainingBalance = 71250.00m,
                Currency = "PKR",
                DueDate = new DateOnly(2026, 10, 14),
                CreatedAtUtc = now,
                IssuedAtUtc = now,
            };
            invoice.Lines.Add(new InvoiceLine
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001"),
                TenantId = tenantId,
                InvoiceId = invoice.Id,
                FeeHeadCode = "TUITION",
                Amount = 65000.00m,
                Trace = "Tuition — Fall 2026",
                Kind = "Charge",
                SortOrder = 1,
            });
            invoice.Lines.Add(new InvoiceLine
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0002"),
                TenantId = tenantId,
                InvoiceId = invoice.Id,
                FeeHeadCode = "LAB",
                Amount = 6250.00m,
                Trace = "Lab fee — Fall 2026",
                Kind = "Charge",
                SortOrder = 2,
            });
            db.Set<Invoice>().Add(invoice);
        }

        await db.SaveChangesAsync(ct);
    }
}
