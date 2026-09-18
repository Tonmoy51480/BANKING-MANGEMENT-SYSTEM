namespace BankingManagementSystem.Core.Entities;

public class LoanPayment
{
    public long PaymentId { get; set; }
    public int LoanId { get; set; }
    public decimal Amount { get; set; }
    public decimal PrincipalComponent { get; set; }
    public decimal InterestComponent { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal RemainingBalance { get; set; }
    public string TransactionReference { get; set; } = string.Empty;
    public string? Notes { get; set; }

    // Navigation properties
    public virtual Loan Loan { get; set; } = null!;
}
