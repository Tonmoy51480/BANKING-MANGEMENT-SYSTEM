namespace BankingManagementSystem.Core.Entities;

public class Beneficiary
{
    public int BeneficiaryId { get; set; }
    public int CustomerId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public bool Status { get; set; } = true; // IsActive
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Customer Customer { get; set; } = null!;
}
