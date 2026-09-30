using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BankingManagementSystem.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICustomerService _customerService;
    private readonly IAuditLogService _auditLogService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ICustomerService customerService,
        IAuditLogService auditLogService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _customerService = customerService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login credentials.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact support.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _auditLogService.LogAsync(user.Id, "User Login", "ApplicationUser", user.Id, $"User logged in: {user.Email}", ip);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _auditLogService.LogAsync(user.Id, "Account Locked", "ApplicationUser", user.Id, $"Account locked due to multiple failed logins: {user.Email}", ip);
            ModelState.AddModelError(string.Empty, "Account locked due to multiple failed login attempts. Please try again later.");
            return View(model);
        }

        await _auditLogService.LogAsync(user.Id, "Failed Login Attempt", "ApplicationUser", user.Id, $"Failed password attempt for user: {user.Email}", ip);
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
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
            Gender = Gender.Male,
            Status = CustomerStatus.Active
        };

        var result = await _customerService.CreateCustomerAsync(customer, model.Password);
        if (!result.Success)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err);
            }
            return View(model);
        }

        TempData["SuccessMessage"] = "Registration successful! You may now sign in with your credentials.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _auditLogService.LogAsync(userId, "User Logout", "ApplicationUser", userId, "User logged out.", ip);
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _auditLogService.LogAsync(user.Id, "Password Changed", "ApplicationUser", user.Id, "User updated their account password.", ip);

        TempData["SuccessMessage"] = "Your password has been changed successfully.";
        return RedirectToAction("Index", "Dashboard");
    }
}
