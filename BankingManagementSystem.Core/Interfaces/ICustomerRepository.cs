using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByCustomerCodeAsync(string customerCode, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Customer?> GetByNidAsync(string nid, CancellationToken cancellationToken = default);
    Task<Customer?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Customer?> GetCustomerWithDetailsAsync(int customerId, CancellationToken cancellationToken = default);
    Task<string> GenerateNextCustomerCodeAsync(CancellationToken cancellationToken = default);
}
