using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface ICustomerService
{
    Task<ServiceResult<Customer>> CreateCustomerAsync(Customer customer, string? password = null, CancellationToken cancellationToken = default);
    Task<ServiceResult<Customer>> UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<Customer?> GetCustomerByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Customer?> GetCustomerDetailsAsync(int customerId, CancellationToken cancellationToken = default);
    Task<ServiceResult> ChangeCustomerStatusAsync(int customerId, bool isActive, CancellationToken cancellationToken = default);
}
