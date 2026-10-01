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
public class TransactionController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public TransactionController(
        ITransactionService transactionService,
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        UserManager<ApplicationUser> userManager)
    {
        _transactionService = transactionService;
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? accountNumber, TransactionType? typeFilter, TransactionStatus? statusFilter, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        var pageSize = 15;
        var query = _transactionRepository.Query()
            .Include(t => t.Account)
                .ThenInclude(a => a.Customer)
            .AsNoTracking();

        // Customer security scope
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer == null)
            {
                return View(new TransactionListViewModel());
            }

            var customerAccountIds = await _accountRepository.Query()
                .Where(a => a.CustomerId == customer.CustomerId)
                .Select(a => a.AccountId)
                .ToListAsync();

            query = query.Where(t => customerAccountIds.Contains(t.AccountId));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(t => t.TransactionReference.ToLower().Contains(term) ||
                                     t.Account.AccountNumber.ToLower().Contains(term) ||
                                     (t.Description != null && t.Description.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(accountNumber))
        {
            query = query.Where(t => t.Account.AccountNumber == accountNumber.Trim());
        }

        if (typeFilter.HasValue)
        {
            query = query.Where(t => t.TransactionType == typeFilter.Value);
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(t => t.Status == statusFilter.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1);
            query = query.Where(t => t.TransactionDate < endOfDay);
        }

        var totalItems = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new TransactionListViewModel
        {
            Transactions = transactions,
            SearchTerm = searchTerm,
            AccountNumber = accountNumber,
            TypeFilter = typeFilter,
            StatusFilter = statusFilter,
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return View(model);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpGet]
    public async Task<IActionResult> Deposit(int? accountId = null)
    {
        var model = new DepositViewModel();
        if (accountId.HasValue)
        {
            model.AccountId = accountId.Value;
        }

        await PopulateAccountsDropdown(model.AccountId);
        return View(model);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposit(DepositViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAccountsDropdown(model.AccountId);
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _transactionService.DepositAsync(model.AccountId, model.Amount, model.Description, userId, ip);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateAccountsDropdown(model.AccountId);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Deposit of {model.Amount:C2} successful! Reference: {result.Data!.TransactionReference}";
        return RedirectToAction(nameof(Receipt), new { reference = result.Data.TransactionReference });
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpGet]
    public async Task<IActionResult> Withdraw(int? accountId = null)
    {
        var model = new WithdrawalViewModel();
        if (accountId.HasValue)
        {
            model.AccountId = accountId.Value;
        }

        await PopulateAccountsDropdown(model.AccountId);
        return View(model);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(WithdrawalViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAccountsDropdown(model.AccountId);
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _transactionService.WithdrawAsync(model.AccountId, model.Amount, model.Description, userId, ip);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateAccountsDropdown(model.AccountId);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Withdrawal of {model.Amount:C2} completed. Reference: {result.Data!.TransactionReference}";
        return RedirectToAction(nameof(Receipt), new { reference = result.Data.TransactionReference });
    }

    public async Task<IActionResult> Receipt(string reference)
    {
        var txn = await _transactionRepository.GetByReferenceAsync(reference);
        if (txn == null)
        {
            return NotFound();
        }

        // Security check for Customer role
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer == null || customer.CustomerId != txn.Account.CustomerId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        var model = new TransactionReceiptViewModel
        {
            Reference = txn.TransactionReference,
            TransactionType = txn.TransactionType.ToString(),
            Date = txn.TransactionDate,
            AccountNumber = txn.Account.AccountNumber,
            CustomerName = txn.Account.Customer?.FullName ?? "Valued Customer",
            Amount = txn.Amount,
            BalanceAfter = txn.BalanceAfterTransaction,
            Description = txn.Description,
            Status = txn.Status.ToString(),
            PerformedBy = txn.PerformedByUserId ?? "System Terminal"
        };

        return View(model);
    }

    private async Task PopulateAccountsDropdown(int selectedAccountId)
    {
        var accounts = await _accountRepository.Query()
            .Include(a => a.Customer)
            .Where(a => a.Status == AccountStatus.Active)
            .OrderBy(a => a.AccountNumber)
            .Select(a => new SelectListItem
            {
                Value = a.AccountId.ToString(),
                Text = $"{a.AccountNumber} - {a.Customer.FullName} (Balance: {a.Balance:C2})",
                Selected = a.AccountId == selectedAccountId
            })
            .ToListAsync();

        ViewBag.AccountList = accounts;
    }
}
