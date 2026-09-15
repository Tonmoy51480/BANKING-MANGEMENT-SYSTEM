using Microsoft.AspNetCore.Identity;

namespace BankingManagementSystem.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual Customer? Customer { get; set; }
    public virtual Employee? Employee { get; set; }
}
