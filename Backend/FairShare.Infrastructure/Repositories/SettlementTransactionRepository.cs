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

    // ИЗМЈЕНА: додати Include-ови за DebtorUser/CreditorUser/QrPaymentData - потребно
    // SettlementService.MapToResponse-у за имена и QR податке, без додатних упита.
    public async Task<IReadOnlyList<SettlementTransaction>> GetByGroupAsync(
        Guid groupId,
        SettlementStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking()
            .Include(s => s.DebtorUser)
            .Include(s => s.CreditorUser)
            .Include(s => s.QrPaymentData)
            .Where(s => s.GroupId == groupId);

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

    // НОВО
    public async Task<SettlementTransaction?> GetByIdWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(s => s.DebtorUser)
            .Include(s => s.CreditorUser)
            .Include(s => s.QrPaymentData)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
}
