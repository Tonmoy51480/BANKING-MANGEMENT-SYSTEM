using System.ComponentModel.DataAnnotations;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BankingManagementSystem.Web.ViewModels;

public class LoanApplicationViewModel
{
    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required]
    [Display(Name = "Disbursement / Repayment Account")]
    public int AccountId { get; set; }

    [Required]
    [Display(Name = "Loan Type")]
    public LoanType LoanType { get; set; } = LoanType.Personal;

    [Required]
    [Range(100, 10000000, ErrorMessage = "Principal amount must be at least $100")]
    [Display(Name = "Principal Amount ($)")]
    public decimal PrincipalAmount { get; set; } = 5000;

    [Required]
    [Range(0.1, 50.0, ErrorMessage = "Interest rate must be between 0.1% and 50%")]
    [Display(Name = "Annual Interest Rate (%)")]
    public decimal InterestRate { get; set; } = 7.5m;

    [Required]
    [Range(1, 360, ErrorMessage = "Duration must be between 1 and 360 months")]
    [Display(Name = "Tenure / Duration (Months)")]
    public int DurationMonths { get; set; } = 12;

    [StringLength(500)]
    [Display(Name = "Application Purpose / Notes")]
    public string? Remarks { get; set; }

    public IEnumerable<SelectListItem> CustomerAccounts { get; set; } = new List<SelectListItem>();
    public IEnumerable<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
}

public class LoanPaymentViewModel
{
    public int LoanId { get; set; }
    public string LoanNumber { get; set; } = string.Empty;
    public decimal RemainingBalance { get; set; }
    public decimal MonthlyPayment { get; set; }
    public string LinkedAccountNumber { get; set; } = string.Empty;
    public decimal LinkedAccountBalance { get; set; }

    [Required]
    [Range(0.01, 10000000, ErrorMessage = "Payment amount must be greater than zero")]
    [Display(Name = "Repayment Amount ($)")]
    public decimal Amount { get; set; }
}

public class LoanListViewModel
{
    public IEnumerable<Loan> Loans { get; set; } = new List<Loan>();
    public LoanStatus? StatusFilter { get; set; }
    public LoanType? TypeFilter { get; set; }
    public string? SearchTerm { get; set; }
}
