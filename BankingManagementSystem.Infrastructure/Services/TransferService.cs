using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;

namespace BankingManagementSystem.Infrastructure.Services;

public class TransferService : ITransferService
{
    private readonly ITransferRepository _transferRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public TransferService(
        ITransferRepository transferRepository,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        IAuditLogService auditLogService,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _transferRepository = transferRepository;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<Transfer>> TransferFundsAsync(
        string senderAccountNumber,
        string receiverAccountNumber,
        decimal amount,
        string? description,
        string? performedByUserId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        // 1. Amount validation
        if (amount <= 0)
        {
            return ServiceResult<Transfer>.Failed("Transfer amount must be strictly greater than zero.");
        }

        // 4. Ensure sender and receiver are different
        if (string.Equals(senderAccountNumber?.Trim(), receiverAccountNumber?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<Transfer>.Failed("Sender and receiver accounts cannot be the same.");
        }

        // 1 & 2. Validate sender & receiver
        var senderAccount = await _accountRepository.GetAccountWithDetailsByNumberAsync(senderAccountNumber?.Trim() ?? string.Empty, cancellationToken);
        if (senderAccount == null)
        {
            return ServiceResult<Transfer>.Failed($"Sender account '{senderAccountNumber}' does not exist.");
        }

        var receiverAccount = await _accountRepository.GetAccountWithDetailsByNumberAsync(receiverAccountNumber?.Trim() ?? string.Empty, cancellationToken);
        if (receiverAccount == null)
        {
            return ServiceResult<Transfer>.Failed($"Receiver account '{receiverAccountNumber}' does not exist.");
        }

        // 3. Ensure both accounts are active
        if (senderAccount.Status != AccountStatus.Active)
        {
            return ServiceResult<Transfer>.Failed($"Sender account {senderAccount.AccountNumber} is {senderAccount.Status} and cannot transfer funds.");
        }

        if (receiverAccount.Status != AccountStatus.Active)
        {
            return ServiceResult<Transfer>.Failed($"Receiver account {receiverAccount.AccountNumber} is {receiverAccount.Status} and cannot receive funds.");
        }

        // 5. Validate sufficient balance
        if (amount > senderAccount.Balance)
        {
            return ServiceResult<Transfer>.Failed($"Insufficient funds. Available: {senderAccount.Balance:C2}, Requested: {amount:C2}.");
        }

        if (senderAccount.Balance - amount < senderAccount.MinimumBalance)
        {
            return ServiceResult<Transfer>.Failed($"Transfer denied. Sending this amount would drop balance below required minimum balance of {senderAccount.MinimumBalance:C2}. Available to transfer: {(senderAccount.Balance - senderAccount.MinimumBalance):C2}.");
        }

        // 6. Begin database transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // 7. Deduct sender balance
            senderAccount.Balance -= amount;
            senderAccount.UpdatedAt = DateTime.UtcNow;
            _accountRepository.Update(senderAccount);

            // 8. Add receiver balance
            receiverAccount.Balance += amount;
            receiverAccount.UpdatedAt = DateTime.UtcNow;
            _accountRepository.Update(receiverAccount);

            var transferRef = await _transferRepository.GenerateNextTransferReferenceAsync(cancellationToken);
            var transfer = new Transfer
            {
                TransferReference = transferRef,
                SenderAccountId = senderAccount.AccountId,
                ReceiverAccountId = receiverAccount.AccountId,
                Amount = amount,
                Description = string.IsNullOrWhiteSpace(description) ? $"Transfer from {senderAccount.AccountNumber} to {receiverAccount.AccountNumber}" : description.Trim(),
                TransferDate = DateTime.UtcNow,
                Status = TransferStatus.Completed,
                InitiatedByUserId = performedByUserId
            };

            await _transferRepository.AddAsync(transfer, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 9. Create debit transaction (TRANSFER_OUT)
            var senderTxnRef = await _transactionRepository.GenerateNextTransactionReferenceAsync(cancellationToken);
            var debitTxn = new Transaction
            {
                TransactionReference = senderTxnRef,
                AccountId = senderAccount.AccountId,
                TransactionType = TransactionType.TRANSFER_OUT,
                Amount = amount,
                BalanceAfterTransaction = senderAccount.Balance,
                Description = $"Transfer to {receiverAccount.AccountNumber} ({receiverAccount.Customer?.FullName})",
                Status = TransactionStatus.Completed,
                TransactionDate = DateTime.UtcNow,
                PerformedByUserId = performedByUserId,
                TransferId = transfer.TransferId
            };
            await _transactionRepository.AddAsync(debitTxn, cancellationToken);

            // 10. Create credit transaction (TRANSFER_IN)
            var receiverTxnRef = $"{senderTxnRef}-CR";
            var creditTxn = new Transaction
            {
                TransactionReference = receiverTxnRef,
                AccountId = receiverAccount.AccountId,
                TransactionType = TransactionType.TRANSFER_IN,
                Amount = amount,
                BalanceAfterTransaction = receiverAccount.Balance,
                Description = $"Transfer from {senderAccount.AccountNumber} ({senderAccount.Customer?.FullName})",
                Status = TransactionStatus.Completed,
                TransactionDate = DateTime.UtcNow,
                PerformedByUserId = performedByUserId,
                TransferId = transfer.TransferId
            };
            await _transactionRepository.AddAsync(creditTxn, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Audit log
            await _auditLogService.LogAsync(
                performedByUserId,
                "Fund Transfer",
                "Transfer",
                transfer.TransferId.ToString(),
                $"Transferred {amount:C2} from {senderAccount.AccountNumber} to {receiverAccount.AccountNumber}. Ref: {transferRef}",
                ipAddress,
                cancellationToken);

            // Notifications to sender and receiver
            if (!string.IsNullOrEmpty(senderAccount.Customer?.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    senderAccount.Customer.UserId,
                    "Debit Alert - Fund Transfer",
                    $"Your transfer of {amount:C2} to {receiverAccount.AccountNumber} was successful. Remaining balance: {senderAccount.Balance:C2}.",
                    "Info",
                    cancellationToken);
            }

            if (!string.IsNullOrEmpty(receiverAccount.Customer?.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    receiverAccount.Customer.UserId,
                    "Credit Alert - Fund Received",
                    $"You have received {amount:C2} from {senderAccount.AccountNumber}. Updated balance: {receiverAccount.Balance:C2}.",
                    "Success",
                    cancellationToken);
            }

            // 12. Commit transaction
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 13. Return success
            return ServiceResult<Transfer>.Ok(transfer, "Fund transfer completed successfully.");
        }
        catch (Exception ex)
        {
            // ROLLBACK if ANY error occurs
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ServiceResult<Transfer>.Failed($"Transfer operation failed and was rolled back: {ex.Message}");
        }
    }

    public async Task<Transfer?> GetTransferByIdAsync(long transferId, CancellationToken cancellationToken = default)
    {
        return await _transferRepository.GetByIdAsync(transferId, cancellationToken);
    }

    public async Task<Transfer?> GetTransferByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _transferRepository.GetByReferenceAsync(reference, cancellationToken);
    }

    public async Task<IEnumerable<Transfer>> GetTransfersByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _transferRepository.GetTransfersByAccountIdAsync(accountId, cancellationToken);
    }
}
