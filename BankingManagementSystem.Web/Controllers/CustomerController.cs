using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Web.Controllers;

[Authorize]
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ICustomerRepository _customerRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerController(
        ICustomerService customerService,
        ICustomerRepository customerRepository,
        UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
        _customerRepository = customerRepository;
        _userManager = userManager;
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    public async Task<IActionResult> Index(string? searchTerm, CustomerStatus? statusFilter, int page = 1)
    {
        var pageSize = 10;
        var query = _customerRepository.Query()
            .Include(c => c.Accounts)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c => c.FullName.ToLower().Contains(term) ||
                                     c.CustomerCode.ToLower().Contains(term) ||
                                     c.Email.ToLower().Contains(term) ||
                                     c.Phone.Contains(term) ||
                                     c.NID.Contains(term));
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(c => c.Status == statusFilter.Value);
        }

        var totalItems = await query.CountAsync();
        var customers = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new CustomerListViewModel
        {
            Customers = customers,
            SearchTerm = searchTerm,
            StatusFilter = statusFilter,
            PageNumber = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        // If customer role, only allow viewing their own record
        if (User.IsInRole(Roles.Customer))
        {
            var currentUserId = _userManager.GetUserId(User);
            var ownCustomer = await _customerService.GetCustomerByUserIdAsync(currentUserId!);
            if (ownCustomer == null || ownCustomer.CustomerId != id)
            {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        var customer = await _customerService.GetCustomerDetailsAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        var model = new CustomerDetailsViewModel
        {
            Customer = customer,
            Accounts = customer.Accounts,
            Loans = customer.Loans,
            Cards = customer.Cards,
            Beneficiaries = customer.Beneficiaries
        };

        return View(model);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CustomerCreateViewModel());
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = new Customer
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            NID = model.NID.Trim(),
            DateOfBirth = model.DateOfBirth,
            Address = model.Address.Trim(),
            Gender = model.Gender,
            Occupation = model.Occupation?.Trim(),
            ProfileImage = model.ProfileImage?.Trim(),
            Status = CustomerStatus.Active
        };

        var result = await _customerService.CreateCustomerAsync(customer, model.InitialPassword);
        if (!result.Success)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(model);
        }

        TempData["SuccessMessage"] = $"Customer '{customer.FullName}' registered successfully with code {customer.CustomerCode}.";
        return RedirectToAction(nameof(Details), new { id = customer.CustomerId });
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        var model = new CustomerEditViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            NID = customer.NID,
            DateOfBirth = customer.DateOfBirth,
            Address = customer.Address,
            Gender = customer.Gender,
            Occupation = customer.Occupation,
            ProfileImage = customer.ProfileImage
        };

        return View(model);
    }

    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customer = new Customer
        {
            CustomerId = model.CustomerId,
            CustomerCode = model.CustomerCode,
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            NID = model.NID.Trim(),
            DateOfBirth = model.DateOfBirth,
            Address = model.Address.Trim(),
            Gender = model.Gender,
            Occupation = model.Occupation?.Trim(),
            ProfileImage = model.ProfileImage?.Trim()
        };

        var result = await _customerService.UpdateCustomerAsync(customer);
        if (!result.Success)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(model);
        }

        TempData["SuccessMessage"] = "Customer details updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.CustomerId });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, bool activate)
    {
        var result = await _customerService.ChangeCustomerStatusAsync(id, activate);
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
}
