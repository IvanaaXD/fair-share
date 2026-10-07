using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Models;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class StatisticsRepository : IStatisticsRepository
{
    private readonly FairShareDbContext _context;

    public StatisticsRepository(FairShareDbContext context)
    {
        _context = context;
    }

    public async Task<SystemTotals> GetTotalsAsync(CancellationToken cancellationToken = default)
    {
        // Сваки број је један COUNT упит у бази - редови се не учитавају у меморију.
        var users = _context.Set<User>().AsNoTracking();
        var settlements = _context.Set<SettlementTransaction>().AsNoTracking();

        var paymentsByStatus = await _context.Set<Payment>().AsNoTracking()
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new SystemTotals(
            Users: await users.CountAsync(cancellationToken),
            BlockedUsers: await users.CountAsync(u => u.IsBlocked, cancellationToken),
            Admins: await users.CountAsync(u => u.Role == UserRole.Admin, cancellationToken),
            Groups: await _context.Set<Group>().CountAsync(cancellationToken),
            PersonalExpenses: await _context.Set<Expense>().CountAsync(cancellationToken),
            GroupExpenses: await _context.Set<GroupExpense>().CountAsync(cancellationToken),
            ProposedSettlements: await settlements.CountAsync(s => s.Status == SettlementStatus.Proposed, cancellationToken),
            SettledSettlements: await settlements.CountAsync(s => s.Status == SettlementStatus.Settled, cancellationToken),
            PaymentsByStatus: paymentsByStatus.ToDictionary(p => p.Status, p => p.Count));
    }

    public Task<IReadOnlyList<DailyCount>> GetDailyCountsAsync(
        StatisticSeries series,
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken = default)
    {
        var dates = series switch
        {
            StatisticSeries.NewUsers => _context.Set<User>().Select(u => u.CreatedAt),
            StatisticSeries.NewGroups => _context.Set<Group>().Select(g => g.CreatedAt),
            StatisticSeries.PersonalExpenses => _context.Set<Expense>().Select(e => e.Date),
            StatisticSeries.GroupExpenses => _context.Set<GroupExpense>().Select(e => e.Date),
            StatisticSeries.Settlements => _context.Set<SettlementTransaction>().Select(s => s.CreatedAt),
            StatisticSeries.Payments => _context.Set<Payment>().Select(p => p.CreatedAt),
            _ => throw new ArgumentOutOfRangeException(nameof(series))
        };

        return CountByDayAsync(dates, from, toExclusive, cancellationToken);
    }

    private static async Task<IReadOnlyList<DailyCount>> CountByDayAsync(
        IQueryable<DateTime> dates,
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken)
        => await dates
            .Where(d => d >= from && d < toExclusive)
            .GroupBy(d => d.Date)
            .Select(g => new DailyCount(g.Key, g.Count()))
            .ToListAsync(cancellationToken);
}
