using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface ITransferRepository : IRepository<Transfer>
{
    Task<Transfer?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transfer>> GetTransfersByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<string> GenerateNextTransferReferenceAsync(CancellationToken cancellationToken = default);
}
