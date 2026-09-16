using BankingManagementSystem.Core.Enums;

namespace BankingManagementSystem.Core.Entities;

public class Customer
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string NID { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string? Occupation { get; set; }
    public string? ProfileImage { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    public virtual ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
