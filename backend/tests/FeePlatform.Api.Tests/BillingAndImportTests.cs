using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FeePlatform.Infrastructure.Outbox;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.Academic.Seed;
using FeePlatform.Modules.Billing;
using FeePlatform.Modules.IdentityTenancy;
using FeePlatform.Modules.IdentityTenancy.Seed;
using FeePlatform.Modules.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FeePlatform.Api.Tests;

public sealed class BillingAndImportTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Issue_statements_happy_path_writes_InvoiceIssued_outbox()
    {
        await using var factory = new FeeApiFactory();
        await SeedPilotAsync(factory);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", PilotSeed.TenantId.ToString("D"));

        var issue = await client.PostAsJsonAsync("/api/v1/billing/statements/issue", new
        {
            termId = AcademicPilotSeed.TermFall2026Id,
            dueDate = "2026-10-15",
            reason = "Fall 2026 term issue",
            instalmentPlanId = (Guid?)null,
        });
        var issueBody = await issue.Content.ReadAsStringAsync();
        Assert.True(issue.IsSuccessStatusCode, issueBody);

        var batch = JsonSerializer.Deserialize<IssueBatchResultDto>(issueBody, JsonOptions);
        Assert.NotNull(batch);
        Assert.True(batch.IssuedCount >= 2);
        Assert.All(batch.Invoices, i => Assert.Equal(InvoiceStatuses.Issued, i.Status));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenant.SetTenant(PilotSeed.TenantId);

        var outbox = await db.OutboxMessages.AsNoTracking()
            .Where(o => o.EventName == DomainEvents.InvoiceIssued)
            .ToListAsync();
        Assert.True(outbox.Count >= 2);

        var invoiceId = batch.Invoices[0].InvoiceId;
        var get = await client.GetAsync($"/api/v1/billing/invoices/{invoiceId}");
        get.EnsureSuccessStatusCode();
        var detail = await get.Content.ReadFromJsonAsync<InvoiceDetailDto>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(InvoiceStatuses.Issued, detail.Status);
        Assert.NotEmpty(detail.Lines);
        Assert.DoesNotContain(detail.Status, new[] { "Overdue", "Processing", "Open" });
    }

    [Fact]
    public async Task Adjust_supersedes_original_and_emits_InvoiceSuperseded()
    {
        await using var factory = new FeeApiFactory();
        await SeedPilotAsync(factory);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", PilotSeed.TenantId.ToString("D"));

        var issue = await client.PostAsJsonAsync("/api/v1/billing/statements/issue", new
        {
            termId = AcademicPilotSeed.TermFall2026Id,
            dueDate = "2026-11-01",
            reason = "Issue before adjust",
        });
        issue.EnsureSuccessStatusCode();
        var batch = await issue.Content.ReadFromJsonAsync<IssueBatchResultDto>(JsonOptions);
        var originalId = batch!.Invoices[0].InvoiceId;

        var adjust = await client.PostAsJsonAsync("/api/v1/billing/adjustments", new
        {
            invoiceId = originalId,
            reason = "Scholarship granted late",
            lines = new[]
            {
                new { feeHeadCode = "TUITION", amount = "50000.00", trace = "Adjusted tuition" },
            },
        });
        var adjustBody = await adjust.Content.ReadAsStringAsync();
        Assert.True(adjust.IsSuccessStatusCode, adjustBody);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenant.SetTenant(PilotSeed.TenantId);

        var original = await db.Invoices.FirstAsync(i => i.Id == originalId);
        Assert.Equal(InvoiceStatuses.Superseded, original.Status);

        var supersededEvents = await db.OutboxMessages
            .Where(o => o.EventName == DomainEvents.InvoiceSuperseded)
            .ToListAsync();
        Assert.NotEmpty(supersededEvents);
    }

    [Fact]
    public async Task Import_validate_counts_create_and_update_rows()
    {
        await using var factory = new FeeApiFactory();
        await SeedPilotAsync(factory);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", PilotSeed.TenantId.ToString("D"));

        var csv = "external_id,display_name,program_code,intake_code\nSTU-PILOT-0099,Student Ninety Nine,BSCS,BSCS-2024\nSTU-PILOT-0001,Student One Updated,BSCS,BSCS-2024\n";
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("students"), "entity");
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", "students.csv");

        var upload = await client.PostAsync("/api/v1/imports", form);
        var uploadBody = await upload.Content.ReadAsStringAsync();
        Assert.True(upload.IsSuccessStatusCode, uploadBody);
        var job = JsonSerializer.Deserialize<ImportJobDto>(uploadBody, JsonOptions);
        Assert.NotNull(job);
        Assert.Equal(ImportJobStatuses.Uploaded, job.Status);

        var dry = await client.PostAsync($"/api/v1/imports/{job.JobId}/validate", null);
        var dryBody = await dry.Content.ReadAsStringAsync();
        Assert.True(dry.IsSuccessStatusCode, dryBody);
        var result = JsonSerializer.Deserialize<ImportDryRunResultDto>(dryBody, JsonOptions);
        Assert.NotNull(result);
        Assert.Equal(ImportJobStatuses.Validated, result.Status);
        Assert.Equal(1, result.WouldCreate);
        Assert.Equal(1, result.WouldUpdate);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public async Task Import_validate_reports_validation_errors()
    {
        await using var factory = new FeeApiFactory();
        await SeedPilotAsync(factory);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", PilotSeed.TenantId.ToString("D"));

        var csv = "external_id,display_name,program_code\n,Missing Id,BSCS\n";
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("students"), "entity");
        form.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(csv)), "file", "bad.csv");

        var upload = await client.PostAsync("/api/v1/imports", form);
        upload.EnsureSuccessStatusCode();
        var job = await upload.Content.ReadFromJsonAsync<ImportJobDto>(JsonOptions);

        var dry = await client.PostAsync($"/api/v1/imports/{job!.JobId}/validate", null);
        dry.EnsureSuccessStatusCode();
        var result = await dry.Content.ReadFromJsonAsync<ImportDryRunResultDto>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.ErrorCount >= 1);
        Assert.Equal(ImportJobStatuses.Validated, result.Status);
    }

    private static async Task SeedPilotAsync(FeeApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenant = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenant.SetPlatformAdminBypass(true);

        if (!await db.Tenants.AnyAsync(t => t.Id == PilotSeed.TenantId))
        {
            db.Tenants.Add(new Tenant
            {
                Id = PilotSeed.TenantId,
                Code = PilotSeed.Slug,
                Name = PilotSeed.DisplayName,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await AcademicPilotSeed.EnsureAppliedAsync(db);
        tenant.Clear();
    }
}
