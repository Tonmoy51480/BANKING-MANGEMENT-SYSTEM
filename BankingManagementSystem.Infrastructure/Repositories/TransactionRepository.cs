using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Transaction?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Account)
                .ThenInclude(a => a.Customer)
            .Include(t => t.Transfer)
            .FirstOrDefaultAsync(t => t.TransactionReference == reference, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(int accountId, int count = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.TransactionDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetDailyWithdrawalTotalAsync(int accountId, DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _dbSet
            .Where(t => t.AccountId == accountId &&
                        t.TransactionType == TransactionType.WITHDRAWAL &&
                        t.Status == TransactionStatus.Completed &&
                        t.TransactionDate >= startOfDay &&
                        t.TransactionDate < endOfDay)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
    }

    public async Task<string> GenerateNextTransactionReferenceAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _dbSet.CountAsync(t => t.TransactionDate.Date == DateTime.UtcNow.Date, cancellationToken);
        return $"{BankingConstants.TransactionRefPrefix}{today}-{(count + 1):D6}";
    }
}
