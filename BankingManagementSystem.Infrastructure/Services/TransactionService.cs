using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;

namespace BankingManagementSystem.Infrastructure.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IAuditLogService auditLogService,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<Transaction>> DepositAsync(int accountId, decimal amount, string? description, string? performedByUserId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return ServiceResult<Transaction>.Failed("Deposit amount must be strictly greater than zero.");
        }

        var account = await _accountRepository.GetAccountWithDetailsAsync(accountId, cancellationToken);
        if (account == null)
        {
            return ServiceResult<Transaction>.Failed("Account not found.");
        }

        if (account.Status != AccountStatus.Active)
        {
            return ServiceResult<Transaction>.Failed($"Account {account.AccountNumber} is {account.Status} and cannot accept deposits.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            account.Balance += amount;
            account.UpdatedAt = DateTime.UtcNow;
            _accountRepository.Update(account);

            var txnRef = await _transactionRepository.GenerateNextTransactionReferenceAsync(cancellationToken);
            var transaction = new Transaction
            {
                TransactionReference = txnRef,
                AccountId = account.AccountId,
                TransactionType = TransactionType.DEPOSIT,
                Amount = amount,
                BalanceAfterTransaction = account.Balance,
                Description = string.IsNullOrWhiteSpace(description) ? "Cash / Cheque Deposit" : description.Trim(),
                Status = TransactionStatus.Completed,
                TransactionDate = DateTime.UtcNow,
                PerformedByUserId = performedByUserId
            };

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                performedByUserId,
                "Deposit",
                "Transaction",
                transaction.TransactionId.ToString(),
                $"Deposit of {amount:C2} into account {account.AccountNumber}. Reference: {txnRef}",
                ipAddress,
                cancellationToken);

            if (!string.IsNullOrEmpty(account.Customer?.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    account.Customer.UserId,
                    "Deposit Received",
                    $"An amount of {amount:C2} has been credited to your account {account.AccountNumber}. Updated balance: {account.Balance:C2}.",
                    "Success",
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ServiceResult<Transaction>.Ok(transaction, "Deposit completed successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ServiceResult<Transaction>.Failed($"Failed to process deposit: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Transaction>> WithdrawAsync(int accountId, decimal amount, string? description, string? performedByUserId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return ServiceResult<Transaction>.Failed("Withdrawal amount must be strictly greater than zero.");
        }

        var account = await _accountRepository.GetAccountWithDetailsAsync(accountId, cancellationToken);
        if (account == null)
        {
            return ServiceResult<Transaction>.Failed("Account not found.");
        }

        if (account.Status != AccountStatus.Active)
        {
            return ServiceResult<Transaction>.Failed($"Account {account.AccountNumber} is {account.Status} and cannot process withdrawals.");
        }

        // Rule 1: Withdrawal > Available Balance check
        if (amount > account.Balance)
        {
            return ServiceResult<Transaction>.Failed($"Insufficient funds. Requested: {amount:C2}, Current Balance: {account.Balance:C2}.");
        }

        // Rule 2: Minimum balance check
        if (account.Balance - amount < account.MinimumBalance)
        {
            return ServiceResult<Transaction>.Failed($"Withdrawal denied. Account requires a minimum balance of {account.MinimumBalance:C2}. Available to withdraw: {(account.Balance - account.MinimumBalance):C2}.");
        }

        // Rule 3: Daily withdrawal limit check
        if (account.AccountType != null && account.AccountType.DailyWithdrawalLimit > 0)
        {
            var todayWithdrawn = await _transactionRepository.GetDailyWithdrawalTotalAsync(account.AccountId, DateTime.UtcNow, cancellationToken);
            if (todayWithdrawn + amount > account.AccountType.DailyWithdrawalLimit)
            {
                var remainingLimit = Math.Max(0, account.AccountType.DailyWithdrawalLimit - todayWithdrawn);
                return ServiceResult<Transaction>.Failed($"Withdrawal exceeds daily limit of {account.AccountType.DailyWithdrawalLimit:C2}. Remaining daily allowance: {remainingLimit:C2}.");
            }
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            account.Balance -= amount;
            account.UpdatedAt = DateTime.UtcNow;
            _accountRepository.Update(account);

            var txnRef = await _transactionRepository.GenerateNextTransactionReferenceAsync(cancellationToken);
            var transaction = new Transaction
            {
                TransactionReference = txnRef,
                AccountId = account.AccountId,
                TransactionType = TransactionType.WITHDRAWAL,
                Amount = amount,
                BalanceAfterTransaction = account.Balance,
                Description = string.IsNullOrWhiteSpace(description) ? "Cash Withdrawal" : description.Trim(),
                Status = TransactionStatus.Completed,
                TransactionDate = DateTime.UtcNow,
                PerformedByUserId = performedByUserId
            };

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                performedByUserId,
                "Withdrawal",
                "Transaction",
                transaction.TransactionId.ToString(),
                $"Withdrawal of {amount:C2} from account {account.AccountNumber}. Reference: {txnRef}",
                ipAddress,
                cancellationToken);

            if (!string.IsNullOrEmpty(account.Customer?.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    account.Customer.UserId,
                    "Withdrawal Alert",
                    $"An amount of {amount:C2} was withdrawn from your account {account.AccountNumber}. Remaining balance: {account.Balance:C2}.",
                    "Warning",
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ServiceResult<Transaction>.Ok(transaction, "Withdrawal completed successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ServiceResult<Transaction>.Failed($"Failed to process withdrawal: {ex.Message}");
        }
    }

    public async Task<Transaction?> GetTransactionByIdAsync(long transactionId, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetByIdAsync(transactionId, cancellationToken);
    }

    public async Task<Transaction?> GetTransactionByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetByReferenceAsync(reference, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetAccountTransactionsAsync(int accountId, int count = 50, CancellationToken cancellationToken = default)
    {
        return await _transactionRepository.GetByAccountIdAsync(accountId, count, cancellationToken);
    }
}
