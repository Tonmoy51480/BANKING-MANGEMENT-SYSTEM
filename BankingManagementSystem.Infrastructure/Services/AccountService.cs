using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRepository<AccountType> _accountTypeRepository;
    private readonly IRepository<Branch> _branchRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        IRepository<AccountType> accountTypeRepository,
        IRepository<Branch> branchRepository,
        ITransactionRepository transactionRepository,
        IAuditLogService auditLogService,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _accountTypeRepository = accountTypeRepository;
        _branchRepository = branchRepository;
        _transactionRepository = transactionRepository;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<Account>> OpenAccountAsync(int customerId, int accountTypeId, int branchId, decimal initialDeposit, string? performedByUserId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
        {
            return ServiceResult<Account>.Failed("Customer not found.");
        }

        if (customer.Status != CustomerStatus.Active)
        {
            return ServiceResult<Account>.Failed("Cannot open account for an inactive or suspended customer.");
        }

        var accountType = await _accountTypeRepository.GetByIdAsync(accountTypeId, cancellationToken);
        if (accountType == null || !accountType.IsActive)
        {
            return ServiceResult<Account>.Failed("Invalid or inactive account type selected.");
        }

        var branch = await _branchRepository.GetByIdAsync(branchId, cancellationToken);
        if (branch == null || !branch.IsActive)
        {
            return ServiceResult<Account>.Failed("Invalid or inactive branch selected.");
        }

        if (initialDeposit < accountType.MinimumBalance)
        {
            return ServiceResult<Account>.Failed($"Initial deposit of {initialDeposit:C2} is below the required minimum balance of {accountType.MinimumBalance:C2} for {accountType.Name}.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var accountNumber = await _accountRepository.GenerateNextAccountNumberAsync(cancellationToken);

            var account = new Account
            {
                AccountNumber = accountNumber,
                CustomerId = customerId,
                AccountTypeId = accountTypeId,
                BranchId = branchId,
                Balance = initialDeposit,
                MinimumBalance = accountType.MinimumBalance,
                Status = AccountStatus.Active,
                OpenedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _accountRepository.AddAsync(account, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Record initial deposit transaction if amount > 0
            if (initialDeposit > 0)
            {
                var txnRef = await _transactionRepository.GenerateNextTransactionReferenceAsync(cancellationToken);
                var initialTxn = new Transaction
                {
                    TransactionReference = txnRef,
                    AccountId = account.AccountId,
                    TransactionType = TransactionType.DEPOSIT,
                    Amount = initialDeposit,
                    BalanceAfterTransaction = initialDeposit,
                    Description = "Initial opening deposit",
                    Status = TransactionStatus.Completed,
                    TransactionDate = DateTime.UtcNow,
                    PerformedByUserId = performedByUserId
                };

                await _transactionRepository.AddAsync(initialTxn, cancellationToken);
            }

            await _auditLogService.LogAsync(
                performedByUserId,
                "Account Created",
                "Account",
                account.AccountId.ToString(),
                $"Opened {accountType.Name} account {account.AccountNumber} for customer {customer.FullName} with initial deposit of {initialDeposit:C2}",
                null,
                cancellationToken);

            if (!string.IsNullOrEmpty(customer.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    customer.UserId,
                    "New Bank Account Opened",
                    $"Your new {accountType.Name} account {account.AccountNumber} has been successfully opened with a starting balance of {initialDeposit:C2}.",
                    "Success",
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ServiceResult<Account>.Ok(account, "Account opened successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ServiceResult<Account>.Failed($"Error opening account: {ex.Message}");
        }
    }

    public async Task<Account?> GetAccountByIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _accountRepository.GetAccountWithDetailsAsync(accountId, cancellationToken);
    }

    public async Task<Account?> GetAccountByNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        return await _accountRepository.GetAccountWithDetailsByNumberAsync(accountNumber, cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetAccountsByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _accountRepository.GetAccountsByCustomerIdAsync(customerId, cancellationToken);
    }

    public async Task<ServiceResult> ChangeAccountStatusAsync(int accountId, AccountStatus status, string? reason, string? performedByUserId, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null)
        {
            return ServiceResult.Failed("Account not found.");
        }

        var oldStatus = account.Status;
        account.Status = status;
        account.UpdatedAt = DateTime.UtcNow;

        _accountRepository.Update(account);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            performedByUserId,
            $"Account Status Changed to {status}",
            "Account",
            account.AccountId.ToString(),
            $"Account {account.AccountNumber} status updated from {oldStatus} to {status}. Reason: {reason ?? "N/A"}",
            null,
            cancellationToken);

        return ServiceResult.Ok($"Account status updated to {status}.");
    }
}
