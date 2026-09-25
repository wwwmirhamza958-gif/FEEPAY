using FeePlatform.Api.Auth;
using FeePlatform.Infrastructure;
using FeePlatform.Infrastructure.Outbox;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .Enrich.FromLogContext());

    builder.Services.AddFeePlatformInfrastructure(builder.Configuration);
    builder.Services.AddValidatorsFromAssemblyContaining<OtpChallengeRequestValidator>();
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
    builder.Services.AddOpenApi();

    // OpenTelemetry stub — exporters wire when observability stack is chosen (D5).
    builder.Services.AddOpenTelemetry();

    var postgres = DependencyInjection.GetPostgresConnectionString(builder.Configuration);

    // Hangfire on PostgreSQL storage — do NOT call Database.Migrate() at startup.
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(postgres)));

    builder.Services.AddHangfireServer(options => { options.WorkerCount = 1; });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi(); // Serves /openapi/v1.json — feed for @fee/api-client generate
    }

    // FORBIDDEN: Database.Migrate() at startup — migrations run via CI/CD or explicit CLI.

    app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "FeePlatform.Api" }))
        .WithName("Health")
        .WithTags("System");

    app.MapAuthStubs();

    app.MapPost("/internal/outbox/dispatch", async (IOutboxDispatcher dispatcher, CancellationToken ct) =>
    {
        await dispatcher.DispatchPendingAsync(ct);
        return Results.Accepted();
    }).WithTags("System");

    // Hangfire dashboard — lock down before any non-local deploy.
    app.UseHangfireDashboard("/hangfire");

    RecurringJob.AddOrUpdate<IOutboxDispatcher>(
        "outbox-dispatch",
        d => d.DispatchPendingAsync(CancellationToken.None),
        Cron.Minutely);

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
