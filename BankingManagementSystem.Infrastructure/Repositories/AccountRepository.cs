using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Repositories;

public class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Customer)
            .Include(a => a.AccountType)
            .Include(a => a.Branch)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<Account?> GetAccountWithDetailsAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Customer)
            .Include(a => a.AccountType)
            .Include(a => a.Branch)
            .Include(a => a.Transactions.OrderByDescending(t => t.TransactionDate).Take(50))
            .Include(a => a.Cards)
            .FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);
    }

    public async Task<Account?> GetAccountWithDetailsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.Customer)
            .Include(a => a.AccountType)
            .Include(a => a.Branch)
            .Include(a => a.Transactions.OrderByDescending(t => t.TransactionDate).Take(50))
            .Include(a => a.Cards)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetAccountsByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(a => a.AccountType)
            .Include(a => a.Branch)
            .Where(a => a.CustomerId == customerId)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateNextAccountNumberAsync(CancellationToken cancellationToken = default)
    {
        var count = await _dbSet.CountAsync(cancellationToken);
        return $"{BankingConstants.AccountNumberPrefix}{(10000000 + count + 1)}";
    }
}
