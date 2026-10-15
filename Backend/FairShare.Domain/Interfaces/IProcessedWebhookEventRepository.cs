using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IProcessedWebhookEventRepository : IRepository<ProcessedWebhookEvent>
{
    Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default);
}
