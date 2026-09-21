using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface ITransferService
{
    Task<ServiceResult<Transfer>> TransferFundsAsync(string senderAccountNumber, string receiverAccountNumber, decimal amount, string? description, string? performedByUserId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<Transfer?> GetTransferByIdAsync(long transferId, CancellationToken cancellationToken = default);
    Task<Transfer?> GetTransferByReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transfer>> GetTransfersByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
}
