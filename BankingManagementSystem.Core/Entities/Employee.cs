namespace BankingManagementSystem.Core.Entities;

public class Employee
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int BranchId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    public virtual Branch Branch { get; set; } = null!;
}
