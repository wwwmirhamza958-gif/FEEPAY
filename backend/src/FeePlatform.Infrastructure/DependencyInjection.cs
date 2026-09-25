using FeePlatform.Infrastructure.Audit;
using FeePlatform.Infrastructure.Auth;
using FeePlatform.Infrastructure.Billing;
using FeePlatform.Infrastructure.Integrations;
using FeePlatform.Infrastructure.Outbox;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.Audit;
using FeePlatform.Modules.FeeEngine.Api;
using FeePlatform.Modules.Integrations.Erp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using FeeEngineImpl = FeePlatform.Modules.FeeEngine.Api.FeeEngine;

namespace FeePlatform.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseProviderKey = "Database:Provider";
    public const string InMemoryProvider = "InMemory";
    public const string NpgsqlProvider = "Npgsql";

    public static IServiceCollection AddFeePlatformInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ITenantContext, TenantContext>();
        services.AddSingleton<IOtpRateLimiter, InMemoryOtpRateLimiter>();

        var provider = configuration.GetValue<string>(DatabaseProviderKey) ?? NpgsqlProvider;
        if (string.Equals(provider, InMemoryProvider, StringComparison.OrdinalIgnoreCase))
        {
            var dbName = configuration.GetValue<string>("Database:InMemoryName") ?? "fee_platform_tests";
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        }
        else
        {
            var connectionString = GetPostgresConnectionString(configuration);
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        services.AddScoped<IOutboxDispatcher, OutboxDispatcherStub>();
        services.AddScoped<IAuditWriter, AuditWriterStub>();
        services.AddSingleton<IFeeEngine, FeeEngineImpl>();
        services.AddScoped<BillingService>();
        services.AddScoped<ImportWizardService>();
        services.AddSingleton<IErpConnector, StubErpConnector>();

        var redisEnabled = configuration.GetValue("Redis:Enabled", defaultValue: true);
        if (redisEnabled)
        {
            var redis = configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false";
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redis));
        }

        return services;
    }

    public static string GetPostgresConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=fee_platform;Username=fee;Password=fee_dev";

    public static bool IsHangfireEnabled(IConfiguration configuration) =>
        configuration.GetValue("Hangfire:Enabled", defaultValue: true);
}
