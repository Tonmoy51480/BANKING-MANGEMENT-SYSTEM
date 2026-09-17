using BankingManagementSystem.Core.Enums;

namespace BankingManagementSystem.Core.Entities;

public class Transfer
{
    public long TransferId { get; set; }
    public string TransferReference { get; set; } = string.Empty;
    public int SenderAccountId { get; set; }
    public int ReceiverAccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public TransferStatus Status { get; set; } = TransferStatus.Completed;
    public string? InitiatedByUserId { get; set; }

    // Navigation properties
    public virtual Account SenderAccount { get; set; } = null!;
    public virtual Account ReceiverAccount { get; set; } = null!;
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
