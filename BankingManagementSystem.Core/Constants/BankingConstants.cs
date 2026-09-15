namespace BankingManagementSystem.Core.Constants;

public static class BankingConstants
{
    public const string CurrencyCode = "USD";
    public const string CurrencySymbol = "$";
    
    public const decimal DefaultSavingsMinBalance = 100.00m;
    public const decimal DefaultCurrentMinBalance = 500.00m;
    public const decimal DefaultSalaryMinBalance = 0.00m;

    public const decimal DefaultDailyWithdrawalLimit = 5000.00m;

    public const string AccountNumberPrefix = "BD-BNK-";
    public const string CustomerCodePrefix = "CUST-";
    public const string TransactionRefPrefix = "TXN-";
    public const string TransferRefPrefix = "TRF-";
    public const string LoanNumberPrefix = "LN-";
}
