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

    public async Task<IReadOnlyList<PaymentCard>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking().Where(c => c.UserId == userId).ToListAsync(cancellationToken);

    public async Task<PaymentCard?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(c => c.Token == token, cancellationToken);
}
