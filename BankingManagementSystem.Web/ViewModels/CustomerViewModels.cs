using System.ComponentModel.DataAnnotations;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;

namespace BankingManagementSystem.Web.ViewModels;

public class CustomerCreateViewModel
{
    [Required(ErrorMessage = "Full Name is required")]
    [StringLength(100, MinimumLength = 3)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "NID is required")]
    [StringLength(30)]
    [Display(Name = "National ID / Passport")]
    public string NID { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; } = DateTime.UtcNow.AddYears(-25);

    [Required]
    [StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required]
    public Gender Gender { get; set; } = Gender.Male;

    [StringLength(100)]
    public string? Occupation { get; set; }

    [Display(Name = "Profile Image URL")]
    public string? ProfileImage { get; set; }

    [Display(Name = "Portal Access Password (Optional)")]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string? InitialPassword { get; set; }
}

public class CustomerEditViewModel
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 3)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string NID { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [StringLength(250)]
    public string Address { get; set; } = string.Empty;

    [Required]
    public Gender Gender { get; set; }

    [StringLength(100)]
    public string? Occupation { get; set; }

    public string? ProfileImage { get; set; }
}

public class CustomerListViewModel
{
    public IEnumerable<Customer> Customers { get; set; } = new List<Customer>();
    public string? SearchTerm { get; set; }
    public CustomerStatus? StatusFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}

public class CustomerDetailsViewModel
{
    public Customer Customer { get; set; } = null!;
    public IEnumerable<Account> Accounts { get; set; } = new List<Account>();
    public IEnumerable<Loan> Loans { get; set; } = new List<Loan>();
    public IEnumerable<Card> Cards { get; set; } = new List<Card>();
    public IEnumerable<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
}
