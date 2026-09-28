using System.ComponentModel.DataAnnotations;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BankingManagementSystem.Web.ViewModels;

public class CardIssueViewModel
{
    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required]
    [Display(Name = "Linked Account")]
    public int AccountId { get; set; }

    [Required]
    [Display(Name = "Card Type")]
    public CardType CardType { get; set; } = CardType.Debit;

    [Required]
    [StringLength(100)]
    [Display(Name = "Name on Card")]
    public string CardHolderName { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> CustomerAccounts { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
}

public class CardListViewModel
{
    public IEnumerable<Card> Cards { get; set; } = new List<Card>();
}

public class BeneficiaryViewModel
{
    public int? BeneficiaryId { get; set; }

    [Required(ErrorMessage = "Account number is required")]
    [StringLength(30)]
    [Display(Name = "Beneficiary Account Number")]
    public string AccountNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Beneficiary full name is required")]
    [StringLength(100)]
    [Display(Name = "Beneficiary Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bank name is required")]
    [StringLength(100)]
    [Display(Name = "Bank Name")]
    public string BankName { get; set; } = "SecureBank Ltd";

    [StringLength(50)]
    [Display(Name = "Nickname / Short Name")]
    public string? Nickname { get; set; }
}
