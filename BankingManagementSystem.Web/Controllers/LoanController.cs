using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Web.Controllers;

[Authorize]
public class LoanController : Controller
{
    private readonly ILoanService _loanService;
    private readonly ILoanRepository _loanRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoanController(
        ILoanService loanService,
        ILoanRepository loanRepository,
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        UserManager<ApplicationUser> userManager)
    {
        _loanService = loanService;
        _loanRepository = loanRepository;
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(LoanStatus? statusFilter, LoanType? typeFilter, string? searchTerm)
    {
        var query = _loanRepository.Query()
            .Include(l => l.Customer)
            .Include(l => l.Account)
            .AsNoTracking();

        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer != null)
            {
                query = query.Where(l => l.CustomerId == customer.CustomerId);
            }
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(l => l.Status == statusFilter.Value);
        }

        if (typeFilter.HasValue)
        {
            query = query.Where(l => l.LoanType == typeFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(l => l.LoanNumber.ToLower().Contains(term) ||
                                     l.Customer.FullName.ToLower().Contains(term));
        }

        var loans = await query.OrderByDescending(l => l.ApplicationDate).ToListAsync();
        return View(new LoanListViewModel
        {
            Loans = loans,
            StatusFilter = statusFilter,
            TypeFilter = typeFilter,
            SearchTerm = searchTerm
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var loan = await _loanService.GetLoanDetailsAsync(id);
        if (loan == null)
        {
            return NotFound();
        }

        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer == null || customer.CustomerId != loan.CustomerId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        return View(loan);
    }

    [HttpGet]
    public async Task<IActionResult> Apply(int? customerId = null)
    {
        var model = new LoanApplicationViewModel();
        await PopulateLoanFormDropdowns(model, customerId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(LoanApplicationViewModel model)
    {
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer != null)
            {
                model.CustomerId = customer.CustomerId;
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateLoanFormDropdowns(model, model.CustomerId);
            return View(model);
        }

        var result = await _loanService.ApplyForLoanAsync(
            model.CustomerId,
            model.AccountId,
            model.LoanType,
            model.PrincipalAmount,
            model.InterestRate,
            model.DurationMonths,
            model.Remarks);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateLoanFormDropdowns(model, model.CustomerId);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Loan application submitted successfully! Reference: {result.Data!.LoanNumber}.";
        return RedirectToAction(nameof(Details), new { id = result.Data.LoanId });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var adminUserId = _userManager.GetUserId(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _loanService.ApproveLoanAsync(id, adminUserId!, ip);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Rejection reason is required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var adminUserId = _userManager.GetUserId(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _loanService.RejectLoanAsync(id, adminUserId!, reason, ip);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = result.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Pay(int id)
    {
        var loan = await _loanService.GetLoanDetailsAsync(id);
        if (loan == null || loan.Status != LoanStatus.Active)
        {
            TempData["ErrorMessage"] = "Only active loans can accept payments.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer == null || customer.CustomerId != loan.CustomerId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        var model = new LoanPaymentViewModel
        {
            LoanId = loan.LoanId,
            LoanNumber = loan.LoanNumber,
            RemainingBalance = loan.RemainingBalance,
            MonthlyPayment = loan.MonthlyPayment,
            LinkedAccountNumber = loan.Account?.AccountNumber ?? "N/A",
            LinkedAccountBalance = loan.Account?.Balance ?? 0m,
            Amount = Math.Min(loan.MonthlyPayment, loan.RemainingBalance)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(LoanPaymentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _loanService.PayLoanInstallmentAsync(model.LoanId, model.Amount, userId, ip);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Loan payment of {model.Amount:C2} processed successfully! Reference: {result.Data!.TransactionReference}";
        return RedirectToAction(nameof(Details), new { id = model.LoanId });
    }

    // EMI Calculator AJAX API
    [HttpGet]
    [AllowAnonymous]
    public IActionResult CalculateEmi(decimal principal, decimal rate, int months)
    {
        var emi = _loanService.CalculateMonthlyPayment(principal, rate, months);
        var totalAmount = emi * months;
        var totalInterest = Math.Max(0, totalAmount - principal);

        return Json(new
        {
            monthlyPayment = emi,
            totalPayment = totalAmount,
            totalInterest = totalInterest
        });
    }

    private async Task PopulateLoanFormDropdowns(LoanApplicationViewModel model, int? customerId)
    {
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer != null)
            {
                model.CustomerId = customer.CustomerId;
                var accounts = await _accountRepository.GetAccountsByCustomerIdAsync(customer.CustomerId);
                model.CustomerAccounts = accounts.Where(a => a.Status == AccountStatus.Active).Select(a => new SelectListItem
                {
                    Value = a.AccountId.ToString(),
                    Text = $"{a.AccountNumber} ({a.AccountType.Name}) - Balance: {a.Balance:C2}"
                });
            }
        }
        else
        {
            var customers = await _customerRepository.Query()
                .Where(c => c.Status == CustomerStatus.Active)
                .OrderBy(c => c.FullName)
                .Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.FullName} ({c.CustomerCode})" })
                .ToListAsync();

            model.Customers = customers;

            var targetCustId = customerId ?? (customers.Count > 0 ? int.Parse(customers[0].Value) : 0);
            if (targetCustId > 0)
            {
                var accounts = await _accountRepository.GetAccountsByCustomerIdAsync(targetCustId);
                model.CustomerAccounts = accounts.Where(a => a.Status == AccountStatus.Active).Select(a => new SelectListItem
                {
                    Value = a.AccountId.ToString(),
                    Text = $"{a.AccountNumber} ({a.AccountType.Name}) - Balance: {a.Balance:C2}"
                });
            }
        }
    }
}
