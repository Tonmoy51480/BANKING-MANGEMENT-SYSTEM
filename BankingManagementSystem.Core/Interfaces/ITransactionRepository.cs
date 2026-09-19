using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<Transaction?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByAccountIdAsync(int accountId, int count = 50, CancellationToken cancellationToken = default);
    Task<decimal> GetDailyWithdrawalTotalAsync(int accountId, DateTime date, CancellationToken cancellationToken = default);
    Task<string> GenerateNextTransactionReferenceAsync(CancellationToken cancellationToken = default);
}
