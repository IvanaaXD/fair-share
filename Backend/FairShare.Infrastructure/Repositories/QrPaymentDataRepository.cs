using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class QrPaymentDataRepository : GenericRepository<QrPaymentData>, IQrPaymentDataRepository
{
    public QrPaymentDataRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<QrPaymentData?> GetBySettlementTransactionAsync(
        Guid settlementTransactionId,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(
            q => q.SettlementTransactionId == settlementTransactionId,
            cancellationToken);
}
