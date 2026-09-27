using System.ComponentModel.DataAnnotations;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BankingManagementSystem.Web.ViewModels;

public class AccountCreateViewModel
{
    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required]
    [Display(Name = "Account Type")]
    public int AccountTypeId { get; set; }

    [Required]
    [Display(Name = "Branch")]
    public int BranchId { get; set; }

    [Required]
    [Range(0, 10000000, ErrorMessage = "Initial deposit must be zero or positive")]
    [Display(Name = "Initial Deposit")]
    public decimal InitialDeposit { get; set; }

    // Selection lists
    public IEnumerable<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> AccountTypes { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Branches { get; set; } = new List<SelectListItem>();
}

public class AccountDetailsViewModel
{
    public Account Account { get; set; } = null!;
    public IEnumerable<Transaction> RecentTransactions { get; set; } = new List<Transaction>();
    public IEnumerable<Card> Cards { get; set; } = new List<Card>();
}

public class AccountListViewModel
{
    public IEnumerable<Account> Accounts { get; set; } = new List<Account>();
    public string? SearchTerm { get; set; }
    public int? AccountTypeId { get; set; }
    public AccountStatus? StatusFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

    public IEnumerable<SelectListItem> AccountTypes { get; set; } = new List<SelectListItem>();
}
