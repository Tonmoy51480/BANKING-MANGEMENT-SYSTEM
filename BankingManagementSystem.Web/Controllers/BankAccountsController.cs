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
public class BankAccountsController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IRepository<AccountType> _accountTypeRepository;
    private readonly IRepository<Branch> _branchRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public BankAccountsController(
        IAccountService accountService,
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        IRepository<AccountType> accountTypeRepository,
        IRepository<Branch> branchRepository,
        ITransactionRepository transactionRepository,
        UserManager<ApplicationUser> userManager)
    {
        _accountService = accountService;
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _accountTypeRepository = accountTypeRepository;
        _branchRepository = branchRepository;
        _transactionRepository = transactionRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? searchTerm, int? accountTypeId, AccountStatus? statusFilter, int page = 1)
    {
        var pageSize = 10;
        var query = _accountRepository.Query()
            .Include(a => a.Customer)
            .Include(a => a.AccountType)
            .Include(a => a.Branch)
            .AsNoTracking();

        // If customer, show only customer's own accounts
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer == null)
            {
                return View(new AccountListViewModel());
            }
            query = query.Where(a => a.CustomerId == customer.CustomerId);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(a => a.AccountNumber.ToLower().Contains(term) ||
                                     a.Customer.FullName.ToLower().Contains(term) ||
                                     a.Customer.CustomerCode.ToLower().Contains(term));
        }

        if (accountTypeId.HasValue && accountTypeId > 0)
        {
            query = query.Where(a => a.AccountTypeId == accountTypeId.Value);
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(a => a.Status == statusFilter.Value);
        }

        var totalItems = await query.CountAsync();
        var accounts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var accountTypes = await _accountTypeRepository.GetAllAsync();

        var model = new AccountListViewModel
        {
            Accounts = accounts,
            SearchTerm = searchTerm,
            AccountTypeId = accountTypeId,
            StatusFilter = statusFilter,
            PageNumber = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            AccountTypes = accountTypes.Select(t => new SelectListItem { Value = t.AccountTypeId.ToString(), Text = t.Name })
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var account = await _accountService.GetAccountByIdAsync(id);
        if (account == null)
        {
            return NotFound();
        }

        // Customer security validation
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer == null || customer.CustomerId != account.CustomerId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        var recentTxns = await _transactionRepository.GetByAccountIdAsync(id, 20);

        var model = new AccountDetailsViewModel
        {
            Account = account,
            RecentTransactions = recentTxns,
            Cards = account.Cards
        };

        return View(model);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null)
    {
        var model = new AccountCreateViewModel();
        if (customerId.HasValue)
        {
            model.CustomerId = customerId.Value;
        }

        await PopulateDropdowns(model);
        return View(model);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccountCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(model);
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        var result = await _accountService.OpenAccountAsync(model.CustomerId, model.AccountTypeId, model.BranchId, model.InitialDeposit, userId);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateDropdowns(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Account '{result.Data!.AccountNumber}' opened successfully with initial deposit of {result.Data.Balance:C2}.";
        return RedirectToAction(nameof(Details), new { id = result.Data.AccountId });
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, AccountStatus status, string? reason)
    {
        var userId = _userManager.GetUserId(User);
        var result = await _accountService.ChangeAccountStatusAsync(id, status, reason, userId);

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

    public async Task<IActionResult> Statement(int id, DateTime? fromDate, DateTime? toDate)
    {
        var account = await _accountService.GetAccountByIdAsync(id);
        if (account == null)
        {
            return NotFound();
        }

        // Customer security check
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer == null || customer.CustomerId != account.CustomerId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        var start = fromDate ?? DateTime.UtcNow.AddMonths(-1);
        var end = (toDate ?? DateTime.UtcNow).Date.AddDays(1);

        var transactions = await _transactionRepository.Query()
            .Where(t => t.AccountId == id && t.TransactionDate >= start && t.TransactionDate < end)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        ViewBag.FromDate = start.ToString("yyyy-MM-dd");
        ViewBag.ToDate = (toDate ?? DateTime.UtcNow).ToString("yyyy-MM-dd");

        return View(new AccountDetailsViewModel
        {
            Account = account,
            RecentTransactions = transactions
        });
    }

    private async Task PopulateDropdowns(AccountCreateViewModel model)
    {
        var customers = await _customerRepository.Query()
            .Where(c => c.Status == CustomerStatus.Active)
            .OrderBy(c => c.FullName)
            .Select(c => new SelectListItem { Value = c.CustomerId.ToString(), Text = $"{c.FullName} ({c.CustomerCode}) - {c.Email}" })
            .ToListAsync();

        var accountTypes = await _accountTypeRepository.Query()
            .Where(t => t.IsActive)
            .Select(t => new SelectListItem { Value = t.AccountTypeId.ToString(), Text = $"{t.Name} (Min: {t.MinimumBalance:C0}, Rate: {t.InterestRate}%)" })
            .ToListAsync();

        var branches = await _branchRepository.Query()
            .Where(b => b.IsActive)
            .Select(b => new SelectListItem { Value = b.BranchId.ToString(), Text = $"{b.BranchName} ({b.BranchCode})" })
            .ToListAsync();

        model.Customers = customers;
        model.AccountTypes = accountTypes;
        model.Branches = branches;
    }
}
