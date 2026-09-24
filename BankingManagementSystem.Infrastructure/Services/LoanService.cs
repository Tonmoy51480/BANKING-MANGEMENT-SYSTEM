using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;

namespace BankingManagementSystem.Infrastructure.Services;

public class LoanService : ILoanService
{
    private readonly ILoanRepository _loanRepository;
    private readonly IRepository<LoanPayment> _loanPaymentRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;
    private readonly IUnitOfWork _unitOfWork;

    public LoanService(
        ILoanRepository loanRepository,
        IRepository<LoanPayment> loanPaymentRepository,
        IAccountRepository accountRepository,
        ITransactionRepository transactionRepository,
        ICustomerRepository customerRepository,
        IAuditLogService auditLogService,
        INotificationService notificationService,
        IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _loanPaymentRepository = loanPaymentRepository;
        _accountRepository = accountRepository;
        _transactionRepository = transactionRepository;
        _customerRepository = customerRepository;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
        _unitOfWork = unitOfWork;
    }

    public decimal CalculateMonthlyPayment(decimal principal, decimal annualRatePercentage, int durationMonths)
    {
        if (principal <= 0 || durationMonths <= 0)
        {
            return 0m;
        }

        if (annualRatePercentage <= 0)
        {
            return Math.Round(principal / durationMonths, 2);
        }

        // Standard EMI calculation: [P * r * (1+r)^n] / [(1+r)^n - 1]
        double p = (double)principal;
        double monthlyRate = (double)(annualRatePercentage / 12m / 100m);
        double factor = Math.Pow(1 + monthlyRate, durationMonths);
        double emi = (p * monthlyRate * factor) / (factor - 1);

        return Math.Round((decimal)emi, 2);
    }

    public async Task<ServiceResult<Loan>> ApplyForLoanAsync(
        int customerId,
        int accountId,
        LoanType loanType,
        decimal principalAmount,
        decimal interestRate,
        int durationMonths,
        string? remarks,
        CancellationToken cancellationToken = default)
    {
        if (principalAmount <= 0)
        {
            return ServiceResult<Loan>.Failed("Loan principal amount must be greater than zero.");
        }

        if (durationMonths < 1 || durationMonths > 360)
        {
            return ServiceResult<Loan>.Failed("Loan duration must be between 1 and 360 months.");
        }

        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null || customer.Status != CustomerStatus.Active)
        {
            return ServiceResult<Loan>.Failed("Customer not found or not in active status.");
        }

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null || account.CustomerId != customerId || account.Status != AccountStatus.Active)
        {
            return ServiceResult<Loan>.Failed("Invalid or inactive linked disbursement/repayment account.");
        }

        var emi = CalculateMonthlyPayment(principalAmount, interestRate, durationMonths);
        var totalRepayment = emi * durationMonths;
        var loanNumber = await _loanRepository.GenerateNextLoanNumberAsync(cancellationToken);

        var loan = new Loan
        {
            LoanNumber = loanNumber,
            CustomerId = customerId,
            AccountId = accountId,
            LoanType = loanType,
            PrincipalAmount = principalAmount,
            InterestRate = interestRate,
            DurationMonths = durationMonths,
            MonthlyPayment = emi,
            TotalRepaymentAmount = totalRepayment,
            RemainingBalance = totalRepayment,
            Status = LoanStatus.Pending,
            ApplicationDate = DateTime.UtcNow,
            Remarks = remarks,
            CreatedAt = DateTime.UtcNow
        };

        await _loanRepository.AddAsync(loan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            customer.UserId,
            "Loan Application Submitted",
            "Loan",
            loan.LoanId.ToString(),
            $"Submitted {loanType} loan application for {principalAmount:C2} ({loanNumber})",
            null,
            cancellationToken);

        if (!string.IsNullOrEmpty(customer.UserId))
        {
            await _notificationService.CreateNotificationAsync(
                customer.UserId,
                "Loan Application Received",
                $"Your {loanType} loan application ({loanNumber}) for {principalAmount:C2} has been submitted for administrative review.",
                "Info",
                cancellationToken);
        }

        return ServiceResult<Loan>.Ok(loan, "Loan application submitted successfully.");
    }

    public async Task<ServiceResult> ApproveLoanAsync(int loanId, string adminUserId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetLoanWithDetailsAsync(loanId, cancellationToken);
        if (loan == null)
        {
            return ServiceResult.Failed("Loan not found.");
        }

        if (loan.Status != LoanStatus.Pending)
        {
            return ServiceResult.Failed($"Only Pending loans can be approved. Current status: {loan.Status}.");
        }

        var account = await _accountRepository.GetByIdAsync(loan.AccountId, cancellationToken);
        if (account == null || account.Status != AccountStatus.Active)
        {
            return ServiceResult.Failed("Linked disbursement account is invalid or not active.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            loan.Status = LoanStatus.Active;
            loan.ApprovedDate = DateTime.UtcNow;
            loan.ApprovedByUserId = adminUserId;
            loan.UpdatedAt = DateTime.UtcNow;
            _loanRepository.Update(loan);

            // Disburse loan principal into customer's linked account
            account.Balance += loan.PrincipalAmount;
            account.UpdatedAt = DateTime.UtcNow;
            _accountRepository.Update(account);

            var txnRef = await _transactionRepository.GenerateNextTransactionReferenceAsync(cancellationToken);
            var disbursementTxn = new Transaction
            {
                TransactionReference = txnRef,
                AccountId = account.AccountId,
                TransactionType = TransactionType.DEPOSIT,
                Amount = loan.PrincipalAmount,
                BalanceAfterTransaction = account.Balance,
                Description = $"Loan Disbursement - {loan.LoanNumber} ({loan.LoanType})",
                Status = TransactionStatus.Completed,
                TransactionDate = DateTime.UtcNow,
                PerformedByUserId = adminUserId
            };

            await _transactionRepository.AddAsync(disbursementTxn, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                adminUserId,
                "Loan Approved",
                "Loan",
                loan.LoanId.ToString(),
                $"Loan {loan.LoanNumber} approved and {loan.PrincipalAmount:C2} disbursed to account {account.AccountNumber}",
                ipAddress,
                cancellationToken);

            if (!string.IsNullOrEmpty(loan.Customer?.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    loan.Customer.UserId,
                    "Loan Approved & Disbursed!",
                    $"Congratulations! Your loan {loan.LoanNumber} has been approved and {loan.PrincipalAmount:C2} has been credited to your account {account.AccountNumber}.",
                    "Success",
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ServiceResult.Ok("Loan approved and funds disbursed successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ServiceResult.Failed($"Failed to approve loan: {ex.Message}");
        }
    }

    public async Task<ServiceResult> RejectLoanAsync(int loanId, string adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var loan = await _loanRepository.GetLoanWithDetailsAsync(loanId, cancellationToken);
        if (loan == null)
        {
            return ServiceResult.Failed("Loan not found.");
        }

        if (loan.Status != LoanStatus.Pending)
        {
            return ServiceResult.Failed($"Only Pending loans can be rejected. Current status: {loan.Status}.");
        }

        loan.Status = LoanStatus.Rejected;
        loan.ApprovedDate = DateTime.UtcNow;
        loan.ApprovedByUserId = adminUserId;
        loan.Remarks = reason;
        loan.UpdatedAt = DateTime.UtcNow;

        _loanRepository.Update(loan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            adminUserId,
            "Loan Rejected",
            "Loan",
            loan.LoanId.ToString(),
            $"Loan {loan.LoanNumber} rejected. Reason: {reason}",
            ipAddress,
            cancellationToken);

        if (!string.IsNullOrEmpty(loan.Customer?.UserId))
        {
            await _notificationService.CreateNotificationAsync(
                loan.Customer.UserId,
                "Loan Application Decision",
                $"Your loan application {loan.LoanNumber} was not approved. Remarks: {reason}",
                "Warning",
                cancellationToken);
        }

        return ServiceResult.Ok("Loan application rejected.");
    }

    public async Task<ServiceResult<LoanPayment>> PayLoanInstallmentAsync(int loanId, decimal amount, string? performedByUserId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return ServiceResult<LoanPayment>.Failed("Payment amount must be greater than zero.");
        }

        var loan = await _loanRepository.GetLoanWithDetailsAsync(loanId, cancellationToken);
        if (loan == null)
        {
            return ServiceResult<LoanPayment>.Failed("Loan not found.");
        }

        if (loan.Status != LoanStatus.Active)
        {
            return ServiceResult<LoanPayment>.Failed($"Payments are only accepted for Active loans. Status: {loan.Status}.");
        }

        if (amount > loan.RemainingBalance)
        {
            return ServiceResult<LoanPayment>.Failed($"Payment amount of {amount:C2} exceeds remaining loan balance of {loan.RemainingBalance:C2}.");
        }

        var account = await _accountRepository.GetAccountWithDetailsAsync(loan.AccountId, cancellationToken);
        if (account == null || account.Status != AccountStatus.Active)
        {
            return ServiceResult<LoanPayment>.Failed("Linked repayment account is not active.");
        }

        if (account.Balance < amount)
        {
            return ServiceResult<LoanPayment>.Failed($"Insufficient balance in repayment account {account.AccountNumber}. Current balance: {account.Balance:C2}, Required: {amount:C2}.");
        }

        if (account.Balance - amount < account.MinimumBalance)
        {
            return ServiceResult<LoanPayment>.Failed($"Payment denied. Dropping below minimum balance of {account.MinimumBalance:C2}.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            // Deduct from account
            account.Balance -= amount;
            account.UpdatedAt = DateTime.UtcNow;
            _accountRepository.Update(account);

            // Update remaining loan balance
            loan.RemainingBalance -= amount;
            if (loan.RemainingBalance <= 0)
            {
                loan.RemainingBalance = 0;
                loan.Status = LoanStatus.Completed;
            }
            loan.UpdatedAt = DateTime.UtcNow;
            _loanRepository.Update(loan);

            var txnRef = await _transactionRepository.GenerateNextTransactionReferenceAsync(cancellationToken);
            var paymentTxn = new Transaction
            {
                TransactionReference = txnRef,
                AccountId = account.AccountId,
                TransactionType = TransactionType.LOAN_PAYMENT,
                Amount = amount,
                BalanceAfterTransaction = account.Balance,
                Description = $"Loan Repayment - {loan.LoanNumber}",
                Status = TransactionStatus.Completed,
                TransactionDate = DateTime.UtcNow,
                PerformedByUserId = performedByUserId
            };
            await _transactionRepository.AddAsync(paymentTxn, cancellationToken);

            // Approximate principal vs interest breakdown based on annual rate
            var monthlyRate = (loan.InterestRate / 12m / 100m);
            var interestComponent = Math.Min(amount, Math.Round(loan.RemainingBalance * monthlyRate, 2));
            var principalComponent = amount - interestComponent;

            var loanPayment = new LoanPayment
            {
                LoanId = loan.LoanId,
                Amount = amount,
                PrincipalComponent = principalComponent,
                InterestComponent = interestComponent,
                PaymentDate = DateTime.UtcNow,
                RemainingBalance = loan.RemainingBalance,
                TransactionReference = txnRef,
                Notes = $"Repayment via account {account.AccountNumber}"
            };
            await _loanPaymentRepository.AddAsync(loanPayment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _auditLogService.LogAsync(
                performedByUserId,
                "Loan Payment",
                "LoanPayment",
                loanPayment.PaymentId.ToString(),
                $"Loan payment of {amount:C2} made for {loan.LoanNumber}. Remaining balance: {loan.RemainingBalance:C2}",
                ipAddress,
                cancellationToken);

            if (!string.IsNullOrEmpty(loan.Customer?.UserId))
            {
                var completionText = loan.Status == LoanStatus.Completed ? " Your loan is now fully PAID OFF!" : "";
                await _notificationService.CreateNotificationAsync(
                    loan.Customer.UserId,
                    "Loan Payment Received",
                    $"Payment of {amount:C2} was processed for loan {loan.LoanNumber}. Remaining balance: {loan.RemainingBalance:C2}.{completionText}",
                    loan.Status == LoanStatus.Completed ? "Success" : "Info",
                    cancellationToken);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ServiceResult<LoanPayment>.Ok(loanPayment, "Loan payment processed successfully.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ServiceResult<LoanPayment>.Failed($"Loan payment failed: {ex.Message}");
        }
    }

    public async Task<Loan?> GetLoanDetailsAsync(int loanId, CancellationToken cancellationToken = default)
    {
        return await _loanRepository.GetLoanWithDetailsAsync(loanId, cancellationToken);
    }

    public async Task<IEnumerable<Loan>> GetLoansByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _loanRepository.GetLoansByCustomerIdAsync(customerId, cancellationToken);
    }
}
