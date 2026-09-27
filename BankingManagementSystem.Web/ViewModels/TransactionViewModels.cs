using System.ComponentModel.DataAnnotations;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BankingManagementSystem.Web.ViewModels;

public class DepositViewModel
{
    [Required(ErrorMessage = "Please select an account")]
    [Display(Name = "Target Account")]
    public int AccountId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 10000000.00, ErrorMessage = "Deposit amount must be greater than zero")]
    [Display(Name = "Deposit Amount ($)")]
    public decimal Amount { get; set; }

    [StringLength(250)]
    [Display(Name = "Description / Narrative")]
    public string? Description { get; set; }

    public IEnumerable<SelectListItem> Accounts { get; set; } = new List<SelectListItem>();
}

public class WithdrawalViewModel
{
    [Required(ErrorMessage = "Please select an account")]
    [Display(Name = "Source Account")]
    public int AccountId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 10000000.00, ErrorMessage = "Withdrawal amount must be greater than zero")]
    [Display(Name = "Withdrawal Amount ($)")]
    public decimal Amount { get; set; }

    [StringLength(250)]
    [Display(Name = "Description / Reason")]
    public string? Description { get; set; }

    public IEnumerable<SelectListItem> Accounts { get; set; } = new List<SelectListItem>();
}

public class TransactionListViewModel
{
    public IEnumerable<Transaction> Transactions { get; set; } = new List<Transaction>();
    public string? SearchTerm { get; set; }
    public string? AccountNumber { get; set; }
    public TransactionType? TypeFilter { get; set; }
    public TransactionStatus? StatusFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}

public class TransactionReceiptViewModel
{
    public string Reference { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
}
