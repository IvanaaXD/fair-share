using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(p => p.PaymentCard)
            .Include(p => p.SettlementTransaction)
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<Payment?> GetBySettlementAsync(Guid settlementTransactionId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(p => p.PaymentCard)
            .FirstOrDefaultAsync(p => p.SettlementTransactionId == settlementTransactionId, cancellationToken);
}
