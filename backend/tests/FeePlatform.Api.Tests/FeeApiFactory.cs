using FeePlatform.Infrastructure;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Modules.IdentityTenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FeePlatform.Api.Tests;

/// <summary>
/// Test host: EF InMemory, no Hangfire, no Redis — works without Docker/Postgres.
/// </summary>
public sealed class FeeApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"fee_tests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", DependencyInjection.InMemoryProvider);
        builder.UseSetting("Database:InMemoryName", _dbName);
        builder.UseSetting("Hangfire:Enabled", "false");
        builder.UseSetting("Redis:Enabled", "false");
        builder.UseSetting("Auth:AllowTotpBypass", "true");
        builder.UseSetting("OpenApi:Enabled", "true");

        builder.ConfigureTestServices(services =>
        {
            // Ensure any leftover Redis/Hangfire registrations from misconfig don't break tests.
            services.RemoveAll(typeof(StackExchange.Redis.IConnectionMultiplexer));
        });
    }

    public async Task SeedPortalUserAsync(
        Guid tenantId,
        string email,
        string password,
        bool totpEnabled = false)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Tenants.AnyAsync(t => t.Id == tenantId))
        {
            db.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Code = $"t-{tenantId:N}"[..16],
                Name = "Test Tenant",
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = "Test Officer",
            IsActive = true,
            TotpEnabled = totpEnabled,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
}
