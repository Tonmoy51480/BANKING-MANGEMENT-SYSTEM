using System.ComponentModel.DataAnnotations;
using BankingManagementSystem.Core.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BankingManagementSystem.Web.ViewModels;

public class TransferViewModel
{
    [Required(ErrorMessage = "Please select or provide the sender account number")]
    [Display(Name = "From Account")]
    public string SenderAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the destination account number")]
    [Display(Name = "To Account Number")]
    public string ReceiverAccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Transfer amount is required")]
    [Range(0.01, 5000000.00, ErrorMessage = "Transfer amount must be greater than zero")]
    [Display(Name = "Amount ($)")]
    public decimal Amount { get; set; }

    [StringLength(250)]
    [Display(Name = "Purpose / Description")]
    public string? Description { get; set; }

    public IEnumerable<SelectListItem> SenderAccounts { get; set; } = new List<SelectListItem>();
    public IEnumerable<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
}

public class TransferReceiptViewModel
{
    public string TransferReference { get; set; } = string.Empty;
    public string SenderAccountNumber { get; set; } = string.Empty;
    public string SenderCustomerName { get; set; } = string.Empty;
    public string ReceiverAccountNumber { get; set; } = string.Empty;
    public string ReceiverCustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
}
