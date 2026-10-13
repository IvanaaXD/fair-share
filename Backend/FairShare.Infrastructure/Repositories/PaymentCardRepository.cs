using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class PaymentCardRepository : GenericRepository<PaymentCard>, IPaymentCardRepository
{
    public PaymentCardRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<PaymentCard>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasPaymentsAsync(Guid paymentCardId, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(c => c.Id == paymentCardId && c.Payments.Any(), cancellationToken);
}
