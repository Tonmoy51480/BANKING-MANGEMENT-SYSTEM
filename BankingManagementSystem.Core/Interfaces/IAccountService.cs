using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;

namespace BankingManagementSystem.Core.Interfaces;

public interface IAccountService
{
    Task<ServiceResult<Account>> OpenAccountAsync(int customerId, int accountTypeId, int branchId, decimal initialDeposit, string? performedByUserId, CancellationToken cancellationToken = default);
    Task<Account?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default);
    Task<Account?> GetAccountByNumberAsync(string accountNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetAccountsByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<ServiceResult> ChangeAccountStatusAsync(int accountId, AccountStatus status, string? reason, string? performedByUserId, CancellationToken cancellationToken = default);
}
