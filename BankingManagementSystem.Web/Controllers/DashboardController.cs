using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankingManagementSystem.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        IDashboardService dashboardService,
        ICustomerService customerService,
        UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _customerService = customerService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (User.IsInRole(Roles.Customer))
        {
            var customer = await _customerService.GetCustomerByUserIdAsync(user.Id);
            if (customer == null)
            {
                ViewBag.ErrorMessage = "No customer banking profile found for your account.";
                return View("CustomerDashboard", new Core.DTOs.CustomerDashboardDto());
            }

            var customerData = await _dashboardService.GetCustomerDashboardDataAsync(customer.CustomerId);
            return View("CustomerDashboard", customerData);
        }

        // Admin or Employee Dashboard
        var adminData = await _dashboardService.GetAdminDashboardDataAsync();
        return View("AdminDashboard", adminData);
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Employee}")]
    public async Task<IActionResult> GetChartData()
    {
        var data = await _dashboardService.GetAdminDashboardDataAsync();
        return Json(new
        {
            monthlyLabels = data.MonthlyLabels,
            monthlyDeposits = data.MonthlyDeposits,
            monthlyWithdrawals = data.MonthlyWithdrawals,
            accountTypeDistribution = data.AccountTypeDistribution,
            transactionTypeDistribution = data.TransactionTypeDistribution
        });
    }
}
