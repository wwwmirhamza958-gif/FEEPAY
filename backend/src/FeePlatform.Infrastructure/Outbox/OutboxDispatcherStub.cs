using FeePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FeePlatform.Infrastructure.Outbox;

/// <summary>P0 stub — polls unprocessed outbox rows. Hangfire will schedule this.</summary>
public sealed class OutboxDispatcherStub(AppDbContext db, ILogger<OutboxDispatcherStub> logger)
    : IOutboxDispatcher
{
    public async Task DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            // Stub: mark processed. Real handlers join in later phases.
            logger.LogInformation(
                "Outbox stub dispatch {EventName} {Id} tenant={TenantId}",
                message.EventName,
                message.Id,
                message.TenantId);

            message.ProcessedAtUtc = DateTimeOffset.UtcNow;
            message.AttemptCount += 1;
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
