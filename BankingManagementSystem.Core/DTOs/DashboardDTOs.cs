namespace BankingManagementSystem.Core.DTOs;

public class AdminDashboardDto
{
    public int TotalCustomers { get; set; }
    public int TotalAccounts { get; set; }
    public int ActiveAccounts { get; set; }
    public decimal TotalDepositVolume { get; set; }
    public decimal TotalWithdrawalVolume { get; set; }
    public decimal TotalTransferVolume { get; set; }
    public int TotalLoans { get; set; }
    public int PendingLoans { get; set; }
    public decimal TotalLoanDisbursed { get; set; }

    // Chart Data
    public List<string> MonthlyLabels { get; set; } = new();
    public List<decimal> MonthlyDeposits { get; set; } = new();
    public List<decimal> MonthlyWithdrawals { get; set; } = new();
    public Dictionary<string, int> AccountTypeDistribution { get; set; } = new();
    public Dictionary<string, int> TransactionTypeDistribution { get; set; } = new();

    public List<TransactionDto> RecentTransactions { get; set; } = new();
}

public class CustomerDashboardDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public decimal TotalBalance { get; set; }
    public int ActiveAccountsCount { get; set; }
    public int ActiveCardsCount { get; set; }
    public int ActiveLoansCount { get; set; }

    public List<AccountSummaryDto> Accounts { get; set; } = new();
    public List<TransactionDto> RecentTransactions { get; set; } = new();
}

public class AccountSummaryDto
{
    public int AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountTypeName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
}

public class TransactionDto
{
    public long TransactionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
