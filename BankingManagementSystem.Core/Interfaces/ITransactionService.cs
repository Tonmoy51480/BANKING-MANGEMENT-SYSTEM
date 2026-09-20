using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface ITransactionService
{
    Task<ServiceResult<Transaction>> DepositAsync(int accountId, decimal amount, string? description, string? performedByUserId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ServiceResult<Transaction>> WithdrawAsync(int accountId, decimal amount, string? description, string? performedByUserId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<Transaction?> GetTransactionByIdAsync(long transactionId, CancellationToken cancellationToken = default);
    Task<Transaction?> GetTransactionByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetAccountTransactionsAsync(int accountId, int count = 50, CancellationToken cancellationToken = default);
}
