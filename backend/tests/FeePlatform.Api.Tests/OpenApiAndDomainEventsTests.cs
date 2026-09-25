using System.Text.Json;
using FeePlatform.Infrastructure.Outbox;

namespace FeePlatform.Api.Tests;

public class DomainEventsTests
{
    [Fact]
    public void DomainEvents_include_contract_section4_names()
    {
        Assert.Equal("PaymentReversed", DomainEvents.PaymentReversed);
        Assert.Equal("DefermentRequested", DomainEvents.DefermentRequested);
        Assert.Equal("DefermentApproved", DomainEvents.DefermentApproved);
        Assert.Equal("DefermentRejected", DomainEvents.DefermentRejected);
        Assert.Equal("InvoiceIssued", DomainEvents.InvoiceIssued);
        Assert.Equal("PaymentSucceeded", DomainEvents.PaymentSucceeded);
    }
}

public class OpenApiTests : IClassFixture<FeeApiFactory>
{
    private readonly FeeApiFactory _factory;

    public OpenApiTests(FeeApiFactory factory) => _factory = factory;

    [Fact]
    public async Task OpenApi_document_includes_ApiError_schemas()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/openapi/v1.json");
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(schemas.TryGetProperty("ApiError", out _));
        Assert.True(schemas.TryGetProperty("ApiErrorDetail", out _));
        Assert.True(schemas.TryGetProperty("ApiErrorEnvelope", out _));
    }

    /// <summary>
    /// When EXPORT_OPENAPI=1, writes packages/api-client/openapi.json for codegen.
    /// Run: $env:EXPORT_OPENAPI=1; dotnet test --filter OpenApi_document_includes_ApiError_schemas
    /// </summary>
    [Fact]
    public async Task OpenApi_export_to_api_client_when_flag_set()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("EXPORT_OPENAPI"), "1", StringComparison.Ordinal))
        {
            return; // no-op unless explicitly exporting
        }

        var client = _factory.CreateClient();
        var json = await client.GetStringAsync("/openapi/v1.json");

        var repoRoot = FindRepoRoot();
        var outPath = Path.Combine(repoRoot, "packages", "api-client", "openapi.json");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        await File.WriteAllTextAsync(outPath, json);
        Assert.True(File.Exists(outPath));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "pnpm-workspace.yaml")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (pnpm-workspace.yaml).");
    }
}
