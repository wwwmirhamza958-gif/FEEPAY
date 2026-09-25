using FeePlatform.Infrastructure.Audit;
using FeePlatform.Infrastructure.Outbox;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace FeePlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFeePlatformInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ITenantContext, TenantContext>();

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=fee_platform;Username=fee;Password=fee_dev";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IOutboxDispatcher, OutboxDispatcherStub>();
        services.AddScoped<IAuditWriter, AuditWriterStub>();

        // Redis connection stub (OTP throttling / rate limits later).
        var redis = configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false";
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redis));

        return services;
    }

    public static string GetPostgresConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString("Postgres")
        ?? "Host=localhost;Port=5432;Database=fee_platform;Username=fee;Password=fee_dev";
}
