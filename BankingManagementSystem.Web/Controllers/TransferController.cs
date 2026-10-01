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
public class TransferController : Controller
{
    private readonly ITransferService _transferService;
    private readonly ITransferRepository _transferRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IBeneficiaryRepository _beneficiaryRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public TransferController(
        ITransferService transferService,
        ITransferRepository transferRepository,
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        IBeneficiaryRepository beneficiaryRepository,
        UserManager<ApplicationUser> userManager)
    {
        _transferService = transferService;
        _transferRepository = transferRepository;
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _beneficiaryRepository = beneficiaryRepository;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new TransferViewModel();
        await PopulateTransferFormAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(TransferViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateTransferFormAsync(model);
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        // If Customer role, ensure they own the sender account
        if (User.IsInRole(Roles.Customer))
        {
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            var senderAccount = await _accountRepository.GetByAccountNumberAsync(model.SenderAccountNumber.Trim());
            if (customer == null || senderAccount == null || senderAccount.CustomerId != customer.CustomerId)
            {
                ModelState.AddModelError(string.Empty, "You are not authorized to transfer funds from this account.");
                await PopulateTransferFormAsync(model);
                return View(model);
            }
        }

        var result = await _transferService.TransferFundsAsync(
            model.SenderAccountNumber,
            model.ReceiverAccountNumber,
            model.Amount,
            model.Description,
            userId,
            ip);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateTransferFormAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Transfer of {model.Amount:C2} completed successfully! Reference: {result.Data!.TransferReference}";
        return RedirectToAction(nameof(Receipt), new { reference = result.Data.TransferReference });
    }

    public async Task<IActionResult> History()
    {
        var query = _transferRepository.Query()
            .Include(t => t.SenderAccount).ThenInclude(sa => sa.Customer)
            .Include(t => t.ReceiverAccount).ThenInclude(ra => ra.Customer)
            .AsNoTracking();

        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer != null)
            {
                var custAccountIds = await _accountRepository.Query()
                    .Where(a => a.CustomerId == customer.CustomerId)
                    .Select(a => a.AccountId)
                    .ToListAsync();

                query = query.Where(t => custAccountIds.Contains(t.SenderAccountId) || custAccountIds.Contains(t.ReceiverAccountId));
            }
        }

        var transfers = await query.OrderByDescending(t => t.TransferDate).Take(100).ToListAsync();
        return View(transfers);
    }

    public async Task<IActionResult> Receipt(string reference)
    {
        var transfer = await _transferRepository.GetByReferenceAsync(reference);
        if (transfer == null)
        {
            return NotFound();
        }

        // Customer security check
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer != null &&
                transfer.SenderAccount.CustomerId != customer.CustomerId &&
                transfer.ReceiverAccount.CustomerId != customer.CustomerId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        var model = new TransferReceiptViewModel
        {
            TransferReference = transfer.TransferReference,
            SenderAccountNumber = transfer.SenderAccount.AccountNumber,
            SenderCustomerName = transfer.SenderAccount.Customer?.FullName ?? "N/A",
            ReceiverAccountNumber = transfer.ReceiverAccount.AccountNumber,
            ReceiverCustomerName = transfer.ReceiverAccount.Customer?.FullName ?? "N/A",
            Amount = transfer.Amount,
            Description = transfer.Description,
            Date = transfer.TransferDate,
            Status = transfer.Status.ToString()
        };

        return View(model);
    }

    // AJAX endpoint for dynamic account validation
    [HttpGet]
    public async Task<IActionResult> LookupAccount(string accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return Json(new { found = false, message = "Account number is required." });
        }

        var account = await _accountRepository.GetAccountWithDetailsByNumberAsync(accountNumber.Trim());
        if (account == null)
        {
            return Json(new { found = false, message = "Account not found." });
        }

        if (account.Status != AccountStatus.Active)
        {
            return Json(new { found = false, message = $"Account is {account.Status} and cannot receive funds." });
        }

        return Json(new
        {
            found = true,
            accountNumber = account.AccountNumber,
            customerName = account.Customer?.FullName ?? "Account Holder",
            accountType = account.AccountType?.Name ?? "Standard",
            branch = account.Branch?.BranchName ?? "Main Branch"
        });
    }

    private async Task PopulateTransferFormAsync(TransferViewModel model)
    {
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer != null)
            {
                var accounts = await _accountRepository.GetAccountsByCustomerIdAsync(customer.CustomerId);
                model.SenderAccounts = accounts
                    .Where(a => a.Status == AccountStatus.Active)
                    .Select(a => new SelectListItem
                    {
                        Value = a.AccountNumber,
                        Text = $"{a.AccountNumber} ({a.AccountType.Name}) - Balance: {a.Balance:C2}"
                    });

                model.Beneficiaries = await _beneficiaryRepository.GetBeneficiariesByCustomerIdAsync(customer.CustomerId);
            }
        }
        else
        {
            var allActiveAccounts = await _accountRepository.Query()
                .Include(a => a.Customer)
                .Include(a => a.AccountType)
                .Where(a => a.Status == AccountStatus.Active)
                .OrderBy(a => a.AccountNumber)
                .Select(a => new SelectListItem
                {
                    Value = a.AccountNumber,
                    Text = $"{a.AccountNumber} - {a.Customer.FullName} ({a.Balance:C2})"
                })
                .ToListAsync();

            model.SenderAccounts = allActiveAccounts;
        }
    }
}
