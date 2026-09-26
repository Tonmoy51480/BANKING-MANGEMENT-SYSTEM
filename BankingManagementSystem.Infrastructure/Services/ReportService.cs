using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DailyBankingSummaryDto> GetDailyBankingSummaryAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var deposits = await _context.Transactions
            .Where(t => t.TransactionType == TransactionType.DEPOSIT &&
                        t.Status == TransactionStatus.Completed &&
                        t.TransactionDate >= startOfDay &&
                        t.TransactionDate < endOfDay)
            .ToListAsync(cancellationToken);

        var withdrawals = await _context.Transactions
            .Where(t => t.TransactionType == TransactionType.WITHDRAWAL &&
                        t.Status == TransactionStatus.Completed &&
                        t.TransactionDate >= startOfDay &&
                        t.TransactionDate < endOfDay)
            .ToListAsync(cancellationToken);

        var transfers = await _context.Transfers
            .Where(t => t.Status == TransferStatus.Completed &&
                        t.TransferDate >= startOfDay &&
                        t.TransferDate < endOfDay)
            .ToListAsync(cancellationToken);

        var loanDisbursements = await _context.Loans
            .Where(l => l.Status == LoanStatus.Active &&
                        l.ApprovedDate >= startOfDay &&
                        l.ApprovedDate < endOfDay)
            .SumAsync(l => (decimal?)l.PrincipalAmount, cancellationToken) ?? 0m;

        var newAccounts = await _context.Accounts
            .CountAsync(a => a.CreatedAt >= startOfDay && a.CreatedAt < endOfDay, cancellationToken);

        var newCustomers = await _context.Customers
            .CountAsync(c => c.CreatedAt >= startOfDay && c.CreatedAt < endOfDay, cancellationToken);

        return new DailyBankingSummaryDto
        {
            Date = date.Date,
            TotalDeposits = deposits.Sum(d => d.Amount),
            DepositCount = deposits.Count,
            TotalWithdrawals = withdrawals.Sum(w => w.Amount),
            WithdrawalCount = withdrawals.Count,
            TotalTransfers = transfers.Sum(t => t.Amount),
            TransferCount = transfers.Count,
            TotalLoanDisbursements = loanDisbursements,
            NewAccountsOpened = newAccounts,
            NewCustomersRegistered = newCustomers
        };
    }

    public async Task<IEnumerable<Transaction>> GetTransactionReportAsync(TransactionReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Include(t => t.Account)
                .ThenInclude(a => a.Customer)
            .AsNoTracking()
            .AsQueryable();

        if (filter.FromDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            var endOfDay = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(t => t.TransactionDate < endOfDay);
        }

        if (!string.IsNullOrWhiteSpace(filter.AccountNumber))
        {
            query = query.Where(t => t.Account.AccountNumber.Contains(filter.AccountNumber.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.TransactionType) && Enum.TryParse<TransactionType>(filter.TransactionType, true, out var tType))
        {
            query = query.Where(t => t.TransactionType == tType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<TransactionStatus>(filter.Status, true, out var tStatus))
        {
            query = query.Where(t => t.Status == tStatus);
        }

        return await query.OrderByDescending(t => t.TransactionDate).Take(500).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetCustomerReportAsync(string? status, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers
            .Include(c => c.Accounts)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CustomerStatus>(status, true, out var cStatus))
        {
            query = query.Where(c => c.Status == cStatus);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(c => c.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1);
            query = query.Where(c => c.CreatedAt < endOfDay);
        }

        return await query.OrderByDescending(c => c.CreatedAt).Take(500).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetAccountReportAsync(int? accountTypeId, string? status, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _context.Accounts
            .Include(a => a.Customer)
            .Include(a => a.AccountType)
            .Include(a => a.Branch)
            .AsNoTracking()
            .AsQueryable();

        if (accountTypeId.HasValue && accountTypeId > 0)
        {
            query = query.Where(a => a.AccountTypeId == accountTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AccountStatus>(status, true, out var aStatus))
        {
            query = query.Where(a => a.Status == aStatus);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedAt < endOfDay);
        }

        return await query.OrderByDescending(a => a.CreatedAt).Take(500).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Loan>> GetLoanReportAsync(LoanReportFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Loans
            .Include(l => l.Customer)
            .Include(l => l.Account)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.LoanType) && Enum.TryParse<LoanType>(filter.LoanType, true, out var lType))
        {
            query = query.Where(l => l.LoanType == lType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<LoanStatus>(filter.Status, true, out var lStatus))
        {
            query = query.Where(l => l.Status == lStatus);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(l => l.ApplicationDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            var endOfDay = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(l => l.ApplicationDate < endOfDay);
        }

        return await query.OrderByDescending(l => l.ApplicationDate).Take(500).ToListAsync(cancellationToken);
    }
}
