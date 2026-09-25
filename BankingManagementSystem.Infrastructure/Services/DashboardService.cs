using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardDto> GetAdminDashboardDataAsync(CancellationToken cancellationToken = default)
    {
        var totalCustomers = await _context.Customers.CountAsync(cancellationToken);
        var totalAccounts = await _context.Accounts.CountAsync(cancellationToken);
        var activeAccounts = await _context.Accounts.CountAsync(a => a.Status == AccountStatus.Active, cancellationToken);

        var totalDeposits = await _context.Transactions
            .Where(t => t.TransactionType == TransactionType.DEPOSIT && t.Status == TransactionStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var totalWithdrawals = await _context.Transactions
            .Where(t => t.TransactionType == TransactionType.WITHDRAWAL && t.Status == TransactionStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var totalTransfers = await _context.Transfers
            .Where(t => t.Status == TransferStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        var totalLoans = await _context.Loans.CountAsync(cancellationToken);
        var pendingLoans = await _context.Loans.CountAsync(l => l.Status == LoanStatus.Pending, cancellationToken);
        var totalLoanDisbursed = await _context.Loans
            .Where(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Completed)
            .SumAsync(l => (decimal?)l.PrincipalAmount, cancellationToken) ?? 0m;

        // Generate past 6 months data for charts
        var monthlyLabels = new List<string>();
        var monthlyDeposits = new List<decimal>();
        var monthlyWithdrawals = new List<decimal>();

        var now = DateTime.UtcNow;
        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var startOfMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            monthlyLabels.Add(startOfMonth.ToString("MMM yyyy"));

            var depSum = await _context.Transactions
                .Where(t => t.TransactionType == TransactionType.DEPOSIT &&
                            t.Status == TransactionStatus.Completed &&
                            t.TransactionDate >= startOfMonth &&
                            t.TransactionDate < endOfMonth)
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
            monthlyDeposits.Add(depSum);

            var withSum = await _context.Transactions
                .Where(t => t.TransactionType == TransactionType.WITHDRAWAL &&
                            t.Status == TransactionStatus.Completed &&
                            t.TransactionDate >= startOfMonth &&
                            t.TransactionDate < endOfMonth)
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
            monthlyWithdrawals.Add(withSum);
        }

        // Account type distribution
        var accountTypeGroups = await _context.Accounts
            .Include(a => a.AccountType)
            .GroupBy(a => a.AccountType.Name)
            .Select(g => new { TypeName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var accountTypeDist = accountTypeGroups.ToDictionary(k => k.TypeName, v => v.Count);

        // Transaction type distribution
        var txnTypeGroups = await _context.Transactions
            .GroupBy(t => t.TransactionType)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var txnTypeDist = txnTypeGroups.ToDictionary(k => k.Type, v => v.Count);

        // Recent 10 transactions
        var recentTxns = await _context.Transactions
            .Include(t => t.Account)
            .OrderByDescending(t => t.TransactionDate)
            .Take(10)
            .Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                Reference = t.TransactionReference,
                AccountNumber = t.Account.AccountNumber,
                TransactionType = t.TransactionType.ToString(),
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfterTransaction,
                Description = t.Description,
                Status = t.Status.ToString(),
                Date = t.TransactionDate
            })
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto
        {
            TotalCustomers = totalCustomers,
            TotalAccounts = totalAccounts,
            ActiveAccounts = activeAccounts,
            TotalDepositVolume = totalDeposits,
            TotalWithdrawalVolume = totalWithdrawals,
            TotalTransferVolume = totalTransfers,
            TotalLoans = totalLoans,
            PendingLoans = pendingLoans,
            TotalLoanDisbursed = totalLoanDisbursed,
            MonthlyLabels = monthlyLabels,
            MonthlyDeposits = monthlyDeposits,
            MonthlyWithdrawals = monthlyWithdrawals,
            AccountTypeDistribution = accountTypeDist,
            TransactionTypeDistribution = txnTypeDist,
            RecentTransactions = recentTxns
        };
    }

    public async Task<CustomerDashboardDto> GetCustomerDashboardDataAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .Include(c => c.Accounts)
                .ThenInclude(a => a.AccountType)
            .Include(c => c.Accounts)
                .ThenInclude(a => a.Branch)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (customer == null)
        {
            return new CustomerDashboardDto();
        }

        var totalBalance = customer.Accounts.Sum(a => a.Balance);
        var activeAccountsCount = customer.Accounts.Count(a => a.Status == AccountStatus.Active);
        var activeCardsCount = await _context.Cards.CountAsync(c => c.CustomerId == customerId && c.Status == CardStatus.Active, cancellationToken);
        var activeLoansCount = await _context.Loans.CountAsync(l => l.CustomerId == customerId && l.Status == LoanStatus.Active, cancellationToken);

        var accountIds = customer.Accounts.Select(a => a.AccountId).ToList();
        var recentTxns = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => accountIds.Contains(t.AccountId))
            .OrderByDescending(t => t.TransactionDate)
            .Take(10)
            .Select(t => new TransactionDto
            {
                TransactionId = t.TransactionId,
                Reference = t.TransactionReference,
                AccountNumber = t.Account.AccountNumber,
                TransactionType = t.TransactionType.ToString(),
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfterTransaction,
                Description = t.Description,
                Status = t.Status.ToString(),
                Date = t.TransactionDate
            })
            .ToListAsync(cancellationToken);

        return new CustomerDashboardDto
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.FullName,
            CustomerCode = customer.CustomerCode,
            TotalBalance = totalBalance,
            ActiveAccountsCount = activeAccountsCount,
            ActiveCardsCount = activeCardsCount,
            ActiveLoansCount = activeLoansCount,
            Accounts = customer.Accounts.Select(a => new AccountSummaryDto
            {
                AccountId = a.AccountId,
                AccountNumber = a.AccountNumber,
                AccountTypeName = a.AccountType?.Name ?? "Standard",
                Balance = a.Balance,
                Status = a.Status.ToString(),
                BranchName = a.Branch?.BranchName ?? "Main Branch"
            }).ToList(),
            RecentTransactions = recentTxns
        };
    }
}
