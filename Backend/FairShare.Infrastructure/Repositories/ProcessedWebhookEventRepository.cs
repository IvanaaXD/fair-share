using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class ProcessedWebhookEventRepository : GenericRepository<ProcessedWebhookEvent>, IProcessedWebhookEventRepository
{
    public ProcessedWebhookEventRepository(FairShareDbContext context) : base(context)
    {
    }

    public Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default)
        => DbSet.AsNoTracking().AnyAsync(e => e.EventId == eventId, cancellationToken);
}
