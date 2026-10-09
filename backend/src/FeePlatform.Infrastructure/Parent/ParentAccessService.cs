using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Modules.Academic;
using FeePlatform.Modules.Billing;
using FeePlatform.Modules.IdentityTenancy;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Infrastructure.Parent;

/// <summary>
/// Resolves Guardian → Student → Tenant authorization for parent/guardian flows.
/// Never trusts client-supplied tenant or student without this check.
/// </summary>
public sealed class ParentAccessService(AppDbContext db)
{
    public const string SessionHeaderName = "X-Parent-Session-Id";

    public async Task<ParentSession?> ResolveSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await db.Sessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.ExpiresAtUtc > DateTimeOffset.UtcNow, ct);
        if (session?.PayerProfileId is null) return null;

        var payer = await db.PayerProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == session.PayerProfileId.Value, ct);
        if (payer is null) return null;

        return new ParentSession(session.Id, payer.Id, payer.MobileE164, payer.DisplayName);
    }

    /// <summary>
    /// All verified student links for this mobile across tenants (IgnoreQueryFilters — mobile is the join key).
    /// </summary>
    public async Task<IReadOnlyList<AuthorizedStudentLink>> ListAuthorizedStudentsAsync(
        string mobileE164,
        CancellationToken ct = default)
    {
        var guardians = await db.Guardians.IgnoreQueryFilters().AsNoTracking()
            .Where(g => g.MobileE164 == mobileE164)
            .ToListAsync(ct);
        if (guardians.Count == 0) return [];

        var guardianIds = guardians.Select(g => g.Id).ToList();
        var links = await db.StudentGuardians.IgnoreQueryFilters().AsNoTracking()
            .Where(sg => guardianIds.Contains(sg.GuardianId) && sg.Verified)
            .ToListAsync(ct);
        if (links.Count == 0) return [];

        var studentIds = links.Select(l => l.StudentId).Distinct().ToList();
        var students = await db.Students.IgnoreQueryFilters().AsNoTracking()
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync(ct);
        var programIds = students.Select(s => s.ProgramId).Distinct().ToList();
        var programs = await db.Programs.IgnoreQueryFilters().AsNoTracking()
            .Where(p => programIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
        var tenantIds = students.Select(s => s.TenantId).Distinct().ToList();
        var tenants = await db.Tenants.IgnoreQueryFilters().AsNoTracking()
            .Where(t => tenantIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);
        var brandings = await db.TenantBrandings.IgnoreQueryFilters().AsNoTracking()
            .Where(b => tenantIds.Contains(b.TenantId))
            .ToDictionaryAsync(b => b.TenantId, ct);

        var result = new List<AuthorizedStudentLink>();
        foreach (var link in links)
        {
            var student = students.FirstOrDefault(s => s.Id == link.StudentId);
            if (student is null) continue;
            // Enforce StudentGuardian.TenantId == Student.TenantId (institute isolation).
            if (link.TenantId != student.TenantId) continue;
            var guardian = guardians.FirstOrDefault(g => g.Id == link.GuardianId);
            if (guardian is null || guardian.TenantId != student.TenantId) continue;

            tenants.TryGetValue(student.TenantId, out var tenant);
            brandings.TryGetValue(student.TenantId, out var branding);
            programs.TryGetValue(student.ProgramId, out var program);

            result.Add(new AuthorizedStudentLink(
                student.Id,
                student.TenantId,
                student.ExternalId,
                student.DisplayName,
                program?.Name ?? "—",
                tenant?.Name ?? "Institution",
                branding?.BrandColourHex ?? "#0B3D91",
                link.GuardianId,
                guardian.DisplayName));
        }

        return result;
    }

    public async Task<AuthorizedStudentLink?> RequireStudentAccessAsync(
        string mobileE164,
        Guid studentId,
        CancellationToken ct = default)
    {
        var all = await ListAuthorizedStudentsAsync(mobileE164, ct);
        return all.FirstOrDefault(s => s.StudentId == studentId);
    }

    public async Task<Invoice?> RequireAuthorizedInvoiceAsync(
        AuthorizedStudentLink link,
        Guid invoiceId,
        CancellationToken ct = default)
    {
        var invoice = await db.Invoices.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice is null) return null;
        if (invoice.TenantId != link.TenantId) return null;
        if (invoice.StudentId != link.StudentId) return null;
        return invoice;
    }

    public async Task<Invoice?> GetOpenInvoiceForStudentAsync(
        Guid tenantId,
        Guid studentId,
        CancellationToken ct = default)
    {
        return await db.Invoices.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.TenantId == tenantId
                        && i.StudentId == studentId
                        && (i.Status == InvoiceStatuses.Issued || i.Status == InvoiceStatuses.PartiallyPaid))
            .OrderBy(i => i.DueDate)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Invoice?> GetLatestInvoiceForStudentAsync(
        Guid tenantId,
        Guid studentId,
        CancellationToken ct = default)
    {
        return await db.Invoices.IgnoreQueryFilters().AsNoTracking()
            .Where(i => i.TenantId == tenantId
                        && i.StudentId == studentId
                        && i.Status != InvoiceStatuses.Draft
                        && i.Status != InvoiceStatuses.Superseded)
            .OrderByDescending(i => i.IssuedAtUtc ?? i.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }
}

public sealed record ParentSession(
    Guid SessionId,
    Guid PayerProfileId,
    string MobileE164,
    string? DisplayName);

public sealed record AuthorizedStudentLink(
    Guid StudentId,
    Guid TenantId,
    string ExternalId,
    string DisplayName,
    string ClassLabel,
    string InstitutionName,
    string TenantBrandHex,
    Guid GuardianId,
    string GuardianDisplayName);
