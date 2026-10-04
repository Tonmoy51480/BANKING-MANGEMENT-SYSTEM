using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface ICardRepository : IRepository<Card>
{
    Task<IEnumerable<Card>> GetCardsByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Card>> GetCardsByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
}
