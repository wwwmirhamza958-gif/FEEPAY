using FeePlatform.Api.Auth;
using FeePlatform.Api.Billing;
using FeePlatform.Api.Integrations;
using FeePlatform.Api.OpenApi;
using FeePlatform.Api.Payments;
using FeePlatform.Api.Tenancy;
using FeePlatform.Infrastructure;
using FeePlatform.Infrastructure.Outbox;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Infrastructure.Seed;
using FeePlatform.Modules.Payments.Receipts;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // QuestPDF 2026 community license (required before generating receipts).
    ReceiptPdfGenerator.EnsureCommunityLicense();

    // preserveStaticLogger: avoid "logger is already frozen" under WebApplicationFactory (multiple hosts).
    builder.Host.UseSerilog((ctx, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .WriteTo.Console()
            .Enrich.FromLogContext(),
        preserveStaticLogger: true);

    builder.Services.AddFeePlatformInfrastructure(builder.Configuration);
    builder.Services.AddValidatorsFromAssemblyContaining<OtpChallengeRequestValidator>();
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<ApiErrorDocumentTransformer>();
    });

    // OpenTelemetry stub — exporters wire when observability stack is chosen (D5).
    builder.Services.AddOpenTelemetry();

    var hangfireEnabled = DependencyInjection.IsHangfireEnabled(builder.Configuration);
    if (hangfireEnabled)
    {
        var postgres = DependencyInjection.GetPostgresConnectionString(builder.Configuration);
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(postgres)));
        builder.Services.AddHangfireServer(options => { options.WorkerCount = 1; });
    }

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseMiddleware<TenantHeaderMiddleware>();

    // OpenAPI for client generation (CONTRACT §9.1). Available in Development/Testing; Production opts in via config.
    var mapOpenApi = !app.Environment.IsProduction()
        || app.Configuration.GetValue("OpenApi:Enabled", defaultValue: false);
    if (mapOpenApi)
    {
        app.MapOpenApi(); // Serves /openapi/v1.json — feed for @fee/api-client generate
    }

    // FORBIDDEN: Database.Migrate() at startup — migrations run via CI/CD or explicit CLI.

    // Dev-only pilot seed on InMemory (R-A-017). Never runs against Postgres here.
    var dbProvider = app.Configuration.GetValue<string>(DependencyInjection.DatabaseProviderKey)
        ?? DependencyInjection.NpgsqlProvider;
    if (app.Environment.IsDevelopment()
        && string.Equals(dbProvider, DependencyInjection.InMemoryProvider, StringComparison.OrdinalIgnoreCase))
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DevSeed.EnsureAppliedAsync(db);
    }

    app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "FeePlatform.Api" }))
        .WithName("Health")
        .WithTags("System");

    app.MapAuthStubs();
    app.MapBillingEndpoints();
    app.MapImportEndpoints();
    app.MapPaymentEndpoints();

    app.MapPost("/internal/outbox/dispatch", async (IOutboxDispatcher dispatcher, CancellationToken ct) =>
    {
        await dispatcher.DispatchPendingAsync(ct);
        return Results.Accepted();
    }).WithTags("System");

    if (hangfireEnabled)
    {
        // Hangfire dashboard — lock down before any non-local deploy.
        app.UseHangfireDashboard("/hangfire");

        RecurringJob.AddOrUpdate<IOutboxDispatcher>(
            "outbox-dispatch",
            d => d.DispatchPendingAsync(CancellationToken.None),
            Cron.Minutely);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Expose for WebApplicationFactory tests.
public partial class Program;
