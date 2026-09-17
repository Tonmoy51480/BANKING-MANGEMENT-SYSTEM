using BankingManagementSystem.Core.Enums;

namespace BankingManagementSystem.Core.Entities;

public class Transaction
{
    public long TransactionId { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public int AccountId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfterTransaction { get; set; }
    public string? Description { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? PerformedByUserId { get; set; }
    public long? TransferId { get; set; }

    // Navigation properties
    public virtual Account Account { get; set; } = null!;
    public virtual Transfer? Transfer { get; set; }
}
