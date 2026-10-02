using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankingManagementSystem.Web.Controllers;

[Authorize]
public class BeneficiaryController : Controller
{
    private readonly IBeneficiaryService _beneficiaryService;
    private readonly ICustomerRepository _customerRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public BeneficiaryController(
        IBeneficiaryService beneficiaryService,
        ICustomerRepository customerRepository,
        UserManager<ApplicationUser> userManager)
    {
        _beneficiaryService = beneficiaryService;
        _customerRepository = customerRepository;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var customer = await _customerRepository.GetByUserIdAsync(userId!);
        if (customer == null)
        {
            return View(new List<Beneficiary>());
        }

        var beneficiaries = await _beneficiaryService.GetBeneficiariesByCustomerIdAsync(customer.CustomerId);
        return View(beneficiaries);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new BeneficiaryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BeneficiaryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        var customer = await _customerRepository.GetByUserIdAsync(userId!);
        if (customer == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var result = await _beneficiaryService.AddBeneficiaryAsync(
            customer.CustomerId,
            model.AccountNumber,
            model.Name,
            model.BankName,
            model.Nickname);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Beneficiary '{model.Name}' added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _userManager.GetUserId(User);
        var customer = await _customerRepository.GetByUserIdAsync(userId!);
        if (customer == null)
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var result = await _beneficiaryService.DeleteBeneficiaryAsync(id, customer.CustomerId);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Beneficiary removed.";
        }

        return RedirectToAction(nameof(Index));
    }
}
