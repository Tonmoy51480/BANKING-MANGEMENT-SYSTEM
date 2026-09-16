using BankingManagementSystem.Core.Enums;

namespace BankingManagementSystem.Core.Entities;

public class Account
{
    public int AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int AccountTypeId { get; set; }
    public int BranchId { get; set; }
    public decimal Balance { get; set; }
    public decimal MinimumBalance { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTime OpenedDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual AccountType AccountType { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public virtual ICollection<Transfer> OutgoingTransfers { get; set; } = new List<Transfer>();
    public virtual ICollection<Transfer> IncomingTransfers { get; set; } = new List<Transfer>();
    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
