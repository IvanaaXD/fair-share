using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class SettlementTransactionRepository : GenericRepository<SettlementTransaction>, ISettlementTransactionRepository
{
    public SettlementTransactionRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<SettlementTransaction>> GetByGroupAsync(
        Guid groupId,
        SettlementStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(s => s.GroupId == groupId);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<SettlementTransaction?> GetWithPaymentAsync(
        Guid settlementTransactionId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(s => s.Payment)
            .FirstOrDefaultAsync(s => s.Id == settlementTransactionId, cancellationToken);
}
