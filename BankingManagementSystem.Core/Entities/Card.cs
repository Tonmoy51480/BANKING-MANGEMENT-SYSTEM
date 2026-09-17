using BankingManagementSystem.Core.Enums;

namespace BankingManagementSystem.Core.Entities;

public class Card
{
    public int CardId { get; set; }
    public int CustomerId { get; set; }
    public int AccountId { get; set; }
    public string CardNumberMasked { get; set; } = string.Empty; // e.g. 4532 **** **** 8821
    public string CardHolderName { get; set; } = string.Empty;
    public CardType CardType { get; set; } = CardType.Debit;
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }
    public CardStatus Status { get; set; } = CardStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Account Account { get; set; } = null!;
}
