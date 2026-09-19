using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<Account?> GetAccountWithDetailsAsync(int accountId, CancellationToken cancellationToken = default);
    Task<Account?> GetAccountWithDetailsByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetAccountsByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<string> GenerateNextAccountNumberAsync(CancellationToken cancellationToken = default);
}
