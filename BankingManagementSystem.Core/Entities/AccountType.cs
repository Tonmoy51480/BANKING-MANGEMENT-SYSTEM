namespace BankingManagementSystem.Core.Entities;

public class AccountType
{
    public int AccountTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MinimumBalance { get; set; }
    public decimal InterestRate { get; set; }
    public decimal DailyWithdrawalLimit { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
