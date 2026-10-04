namespace BankingManagementSystem.Core.DTOs;

public class DailyBankingSummaryDto
{
    public DateTime Date { get; set; }
    public decimal TotalDeposits { get; set; }
    public int DepositCount { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public int WithdrawalCount { get; set; }
    public decimal TotalTransfers { get; set; }
    public int TransferCount { get; set; }
    public decimal TotalLoanDisbursements { get; set; }
    public int NewAccountsOpened { get; set; }
    public int NewCustomersRegistered { get; set; }
    public decimal NetCashFlow => TotalDeposits - TotalWithdrawals;
}

public class TransactionReportFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? AccountNumber { get; set; }
    public string? TransactionType { get; set; }
    public string? Status { get; set; }
}

public class LoanReportFilterDto
{
    public string? LoanType { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
