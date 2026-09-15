using Microsoft.AspNetCore.Identity;

namespace BankingManagementSystem.Core.Entities;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName, string? description = null) : base(roleName)
    {
        Description = description;
    }
}
