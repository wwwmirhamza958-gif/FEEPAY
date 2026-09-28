using System.Net;
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
using FeePlatform.Modules.Payments;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FeePlatform.Api.Tests;

public sealed class PaymentTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Create_attempt_is_idempotent_on_double_post()
    {
        await using var factory = new PaymentsTestFactory();
        var invoiceId = await SeedIssuedInvoiceAsync(factory);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", PilotSeed.TenantId.ToString("D"));

        var body = new
        {
            payableType = "Invoice",
            payableId = invoiceId,
            adapterCode = AdapterCodes.RaastP2mPilot,
            idempotencyKey = "idem-double-create-1",
        };

        var first = await client.PostAsJsonAsync("/api/v1/payments/attempts", body);
        var firstText = await first.Content.ReadAsStringAsync();
        Assert.True(first.IsSuccessStatusCode, firstText);
        var a1 = JsonSerializer.Deserialize<PaymentAttemptDto>(firstText, JsonOptions)!;

        var second = await client.PostAsJsonAsync("/api/v1/payments/attempts", body);
        var secondText = await second.Content.ReadAsStringAsync();
        Assert.True(second.IsSuccessStatusCode, secondText);
        var a2 = JsonSerializer.Deserialize<PaymentAttemptDto>(secondText, JsonOptions)!;

        Assert.Equal(a1.AttemptId, a2.AttemptId);
        Assert.Equal(a1.IdempotencyKey, a2.IdempotencyKey);
    }

    [Fact]
    public async Task Duplicate_success_callback_is_idempotent()
    {
        await using var factory = new PaymentsTestFactory();
        var invoiceId = await SeedIssuedInvoiceAsync(factory);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", PilotSeed.TenantId.ToString("D"));

        var create = await client.PostAsJsonAsync("/api/v1/payments/attempts", new
        {
            payableType = "Invoice",
            payableId = invoiceId,
            adapterCode = AdapterCodes.JazzCashPilot,
            idempotencyKey = "idem-dup-callback-1",
        });
        create.EnsureSuccessStatusCode();
        var attempt = await create.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);

        var payload = $$"""{"attemptId":"{{attempt!.AttemptId:D}}","amount":"{{attempt.Amount}}","gatewayTxnId":"gw-dup-1","gatewayRef":"{{attempt.GatewayRef}}","sandboxFake":true}""";
        using var req1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payments/callbacks/{AdapterCodes.JazzCashPilot}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        req1.Headers.Add("X-Sandbox-Fake", "succeed");
        var cb1 = await client.SendAsync(req1);
        var cb1Text = await cb1.Content.ReadAsStringAsync();
        Assert.True(cb1.IsSuccessStatusCode, cb1Text);
        var s1 = JsonSerializer.Deserialize<PaymentAttemptDto>(cb1Text, JsonOptions)!;
        Assert.Equal(PaymentAttemptStatuses.Succeeded, s1.Status);

        using var req2 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payments/callbacks/{AdapterCodes.JazzCashPilot}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        req2.Headers.Add("X-Sandbox-Fake", "succeed");
        var cb2 = await client.SendAsync(req2);
        cb2.EnsureSuccessStatusCode();
        var s2 = await cb2.Content.ReadFromJsonAsync<PaymentAttemptDto>(JsonOptions);
        Assert.Equal(s1.AttemptId, s2!.AttemptId);
        Assert.Equal(PaymentAttemptStatuses.Succeeded, s2.Status);
        Assert.Equal(s1.ReceiptId, s2.ReceiptId);
    }

    [Fact]
    public async Task Unsigned_callback_is_rejected()
    {
        await using var factory = new PaymentsTestFactory(allowSandboxFake: false, withHmacSecret: true);
        await SeedPilotAsync(factory);
        var client = factory.CreateClient();

        var payload = """{"attemptId":"11111111-1111-1111-1111-111111111111","status":"Succeeded","gatewayTxnId":"x"}""";
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/payments/callbacks/{AdapterCodes.RaastP2mPilot}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        // No X-Signature header
        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var err = await res.Content.ReadAsStringAsync();
        Assert.Contains("CALLBACK_SIGNATURE_INVALID", err);
    }

    [Fact]
    public async Task Fake_sandbox_success_writes_receipt_and_outbox()
    {
        await using var factory = new PaymentsTestFactory();
        var invoiceId = await SeedIssuedInvoiceAsync(factory);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", PilotSeed.TenantId.ToString("D"));

        var create = await client.PostAsJsonAsync("/api/v1/payments/attempts", new
        {
            payableType = "Invoice",
            payableId = invoiceId,
            adapterCode = AdapterCodes.EasypaisaPilot,
            idempotencyKey = "idem-fake-receipt-1",
        });
        var createText = await create.Content.ReadAsStringAsync();
        Assert.True(create.IsSuccessStatusCode, createText);
        var attempt = JsonSerializer.Deserialize<PaymentAttemptDto>(createText, JsonOptions)!;
        Assert.Equal(PaymentAttemptStatuses.Pending, attempt.Status);
        Assert.False(string.IsNullOrWhiteSpace(attempt.CheckoutUrl));

        var complete = await client.PostAsync($"/api/v1/payments/sandbox/fake-complete/{attempt.AttemptId:D}", null);
        var completeText = await complete.Content.ReadAsStringAsync();
        Assert.True(complete.IsSuccessStatusCode, completeText);
        var succeeded = JsonSerializer.Deserialize<PaymentAttemptDto>(completeText, JsonOptions)!;
        Assert.Equal(PaymentAttemptStatuses.Succeeded, succeeded.Status);
        Assert.NotNull(succeeded.ReceiptId);

        var receipt = await client.GetAsync($"/api/v1/payments/receipts/{succeeded.ReceiptId:D}");
        receipt.EnsureSuccessStatusCode();
        var receiptDto = await receipt.Content.ReadFromJsonAsync<PaymentReceiptDto>(JsonOptions);
        Assert.NotNull(receiptDto);
        Assert.True(receiptDto.ReceiptNumber >= 1);
        Assert.True(receiptDto.HasPdf);

        var pdf = await client.GetAsync($"/api/v1/payments/receipts/{succeeded.ReceiptId:D}/pdf");
        pdf.EnsureSuccessStatusCode();
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        var bytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);

        await using var dbScope = factory.Services.CreateAsyncScope();
        var db = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenant = dbScope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenant.SetTenant(PilotSeed.TenantId);
        var events = await db.OutboxMessages.AsNoTracking()
            .Where(o => o.EventName == DomainEvents.PaymentSucceeded)
            .ToListAsync();
        Assert.Contains(events, e => e.PayloadJson.Contains(succeeded.AttemptId.ToString("D")));
    }

    [Fact]
    public async Task Channels_return_highest_tier_default_not_flat_only()
    {
        await using var factory = new PaymentsTestFactory();
        await SeedPilotAsync(factory);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", PilotSeed.TenantId.ToString("D"));

        var res = await client.GetAsync($"/api/v1/payments/channels?tenantId={PilotSeed.TenantId:D}");
        res.EnsureSuccessStatusCode();
        var channels = await res.Content.ReadFromJsonAsync<PaymentChannelsResponse>(JsonOptions);
        Assert.NotNull(channels);
        Assert.Equal(1, channels.HighestEnabledTier);
        Assert.NotNull(channels.DefaultChannel);
        Assert.Equal(1, channels.DefaultChannel.Tier);
        Assert.True(channels.DefaultChannel.IsDefault);
        Assert.NotEmpty(channels.OtherChannels);
        Assert.DoesNotContain(channels.OtherChannels, c => c.AdapterCode == channels.DefaultChannel.AdapterCode);
    }

    private static async Task<Guid> SeedIssuedInvoiceAsync(PaymentsTestFactory factory)
    {
        await SeedPilotAsync(factory);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", PilotSeed.TenantId.ToString("D"));

        var issue = await client.PostAsJsonAsync("/api/v1/billing/statements/issue", new
        {
            termId = AcademicPilotSeed.TermFall2026Id,
            dueDate = "2026-10-15",
            reason = "Payment test issue",
        });
        var issueBody = await issue.Content.ReadAsStringAsync();
        Assert.True(issue.IsSuccessStatusCode, issueBody);
        var batch = JsonSerializer.Deserialize<IssueBatchResultDto>(issueBody, JsonOptions)!;
        Assert.NotEmpty(batch.Invoices);
        return batch.Invoices[0].InvoiceId;
    }

    private static async Task SeedPilotAsync(WebApplicationFactory<Program> factory)
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

    /// <summary>
    /// Development + AllowSandboxFake for FakeTier1 paths. InMemory, no Hangfire/Redis.
    /// </summary>
    private sealed class PaymentsTestFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"fee_pay_tests_{Guid.NewGuid():N}";
        private readonly bool _allowSandboxFake;
        private readonly bool _withHmacSecret;

        public PaymentsTestFactory(bool allowSandboxFake = true, bool withHmacSecret = false)
        {
            _allowSandboxFake = allowSandboxFake;
            _withHmacSecret = withHmacSecret;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // FakeTier1 requires Development (not Testing).
            builder.UseEnvironment("Development");
            builder.UseSetting("Database:Provider", FeePlatform.Infrastructure.DependencyInjection.InMemoryProvider);
            builder.UseSetting("Database:InMemoryName", _dbName);
            builder.UseSetting("Hangfire:Enabled", "false");
            builder.UseSetting("Redis:Enabled", "false");
            builder.UseSetting("Auth:AllowTotpBypass", "true");
            builder.UseSetting("OpenApi:Enabled", "true");
            builder.UseSetting("Payments:AllowSandboxFake", _allowSandboxFake ? "true" : "false");
            builder.UseSetting("Payments:PublicBaseUrl", "http://localhost");
            if (_withHmacSecret)
            {
                builder.UseSetting($"Payments:Adapters:{AdapterCodes.RaastP2mPilot}:CallbackHmacSecret", "test-hmac-secret");
                builder.UseSetting($"Payments:Adapters:{AdapterCodes.RaastP2mPilot}:Enabled", "true");
            }
        }
    }
}
