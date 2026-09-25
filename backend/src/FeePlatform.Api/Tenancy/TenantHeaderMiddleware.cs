using FeePlatform.Infrastructure.Tenancy;

namespace FeePlatform.Api.Tenancy;

/// <summary>
/// P1 portal stub: sets ambient tenant from X-Tenant-Id (UUID).
/// Full session→tenant binding lands with auth hardening.
/// </summary>
public sealed class TenantHeaderMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Tenant-Id";

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values)
            && Guid.TryParse(values.FirstOrDefault(), out var tenantId))
        {
            tenantContext.SetTenant(tenantId);
        }

        try
        {
            await next(context);
        }
        finally
        {
            tenantContext.Clear();
        }
    }
}
