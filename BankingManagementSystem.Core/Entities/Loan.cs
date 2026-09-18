using BankingManagementSystem.Core.Enums;

namespace BankingManagementSystem.Core.Entities;

public class Loan
{
    public int LoanId { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public int AccountId { get; set; }
    public LoanType LoanType { get; set; } = LoanType.Personal;
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; } // Annual interest rate percentage, e.g. 8.5%
    public int DurationMonths { get; set; }
    public decimal MonthlyPayment { get; set; } // Calculated EMI
    public decimal TotalRepaymentAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Pending;
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedByUserId { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual Account Account { get; set; } = null!;
    public virtual ICollection<LoanPayment> LoanPayments { get; set; } = new List<LoanPayment>();
}
