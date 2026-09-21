using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;

namespace BankingManagementSystem.Core.Interfaces;

public interface ILoanService
{
    Task<ServiceResult<Loan>> ApplyForLoanAsync(int customerId, int accountId, LoanType loanType, decimal principalAmount, decimal interestRate, int durationMonths, string? remarks, CancellationToken cancellationToken = default);
    Task<ServiceResult> ApproveLoanAsync(int loanId, string adminUserId, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ServiceResult> RejectLoanAsync(int loanId, string adminUserId, string reason, string? ipAddress, CancellationToken cancellationToken = default);
    Task<ServiceResult<LoanPayment>> PayLoanInstallmentAsync(int loanId, decimal amount, string? performedByUserId, string? ipAddress, CancellationToken cancellationToken = default);
    decimal CalculateMonthlyPayment(decimal principal, decimal annualRatePercentage, int durationMonths);
    Task<Loan?> GetLoanDetailsAsync(int loanId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Loan>> GetLoansByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
}
