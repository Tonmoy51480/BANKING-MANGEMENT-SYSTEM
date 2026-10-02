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
public class CardController : Controller
{
    private readonly ICardService _cardService;
    private readonly ICardRepository _cardRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public CardController(
        ICardService cardService,
        ICardRepository cardRepository,
        ICustomerRepository customerRepository,
        IAccountRepository accountRepository,
        UserManager<ApplicationUser> userManager)
    {
        _cardService = cardService;
        _cardRepository = cardRepository;
        _customerRepository = customerRepository;
        _accountRepository = accountRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var query = _cardRepository.Query()
            .Include(c => c.Customer)
            .Include(c => c.Account)
            .AsNoTracking();

        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer != null)
            {
                query = query.Where(c => c.CustomerId == customer.CustomerId);
            }
        }

        var cards = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        return View(new CardListViewModel { Cards = cards });
    }

    [HttpGet]
    public async Task<IActionResult> Issue(int? customerId = null)
    {
        var model = new CardIssueViewModel();
        await PopulateCardFormDropdowns(model, customerId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(CardIssueViewModel model)
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
            await PopulateCardFormDropdowns(model, model.CustomerId);
            return View(model);
        }

        var result = await _cardService.IssueCardAsync(model.CustomerId, model.AccountId, model.CardType, model.CardHolderName);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            await PopulateCardFormDropdowns(model, model.CustomerId);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Debit Card issued successfully! Card Number: {result.Data!.CardNumberMasked}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int cardId, CardStatus status)
    {
        // Customer security check
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            var card = await _cardRepository.GetByIdAsync(cardId);
            if (customer == null || card == null || card.CustomerId != customer.CustomerId)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        var result = await _cardService.ChangeCardStatusAsync(cardId, status);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"Card status updated to {status}.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCardFormDropdowns(CardIssueViewModel model, int? customerId)
    {
        if (User.IsInRole(Roles.Customer))
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _customerRepository.GetByUserIdAsync(userId!);
            if (customer != null)
            {
                model.CustomerId = customer.CustomerId;
                model.CardHolderName = customer.FullName.ToUpper();
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
