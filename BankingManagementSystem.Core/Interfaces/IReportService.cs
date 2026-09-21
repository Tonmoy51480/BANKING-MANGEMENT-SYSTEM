using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface IReportService
{
    Task<DailyBankingSummaryDto> GetDailyBankingSummaryAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetTransactionReportAsync(TransactionReportFilterDto filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetCustomerReportAsync(string? status, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Account>> GetAccountReportAsync(int? accountTypeId, string? status, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Loan>> GetLoanReportAsync(LoanReportFilterDto filter, CancellationToken cancellationToken = default);
}
