using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BankingManagementSystem.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(
        ICustomerRepository customerRepository,
        UserManager<ApplicationUser> userManager,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _userManager = userManager;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<Customer>> CreateCustomerAsync(Customer customer, string? password = null, CancellationToken cancellationToken = default)
    {
        if (await _customerRepository.GetByEmailAsync(customer.Email, cancellationToken) != null)
        {
            return ServiceResult<Customer>.Failed($"Customer with email '{customer.Email}' already exists.");
        }

        if (await _customerRepository.GetByNidAsync(customer.NID, cancellationToken) != null)
        {
            return ServiceResult<Customer>.Failed($"Customer with NID '{customer.NID}' already exists.");
        }

        // Auto-generate customer code
        customer.CustomerCode = await _customerRepository.GenerateNextCustomerCodeAsync(cancellationToken);
        customer.CreatedAt = DateTime.UtcNow;
        customer.Status = CustomerStatus.Active;

        // If password is provided, create linked Identity user
        if (!string.IsNullOrWhiteSpace(password))
        {
            var user = new ApplicationUser
            {
                UserName = customer.Email,
                Email = customer.Email,
                FullName = customer.FullName,
                PhoneNumber = customer.Phone,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var userResult = await _userManager.CreateAsync(user, password);
            if (!userResult.Succeeded)
            {
                var errors = userResult.Errors.Select(e => e.Description).ToList();
                return ServiceResult<Customer>.Failed(errors, "Failed to create user account for customer.");
            }

            await _userManager.AddToRoleAsync(user, Roles.Customer);
            customer.UserId = user.Id;
        }

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            customer.UserId,
            "Customer Created",
            "Customer",
            customer.CustomerId.ToString(),
            $"Customer created: {customer.FullName} ({customer.CustomerCode})",
            null,
            cancellationToken);

        return ServiceResult<Customer>.Ok(customer, "Customer registered successfully.");
    }

    public async Task<ServiceResult<Customer>> UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var existing = await _customerRepository.GetByIdAsync(customer.CustomerId, cancellationToken);
        if (existing == null)
        {
            return ServiceResult<Customer>.Failed("Customer not found.");
        }

        // Check unique email if modified
        if (!string.Equals(existing.Email, customer.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _customerRepository.GetByEmailAsync(customer.Email, cancellationToken);
            if (emailExists != null && emailExists.CustomerId != customer.CustomerId)
            {
                return ServiceResult<Customer>.Failed("Email is already used by another customer.");
            }
        }

        // Check unique NID if modified
        if (existing.NID != customer.NID)
        {
            var nidExists = await _customerRepository.GetByNidAsync(customer.NID, cancellationToken);
            if (nidExists != null && nidExists.CustomerId != customer.CustomerId)
            {
                return ServiceResult<Customer>.Failed("NID is already used by another customer.");
            }
        }

        existing.FullName = customer.FullName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.NID = customer.NID;
        existing.DateOfBirth = customer.DateOfBirth;
        existing.Address = customer.Address;
        existing.Gender = customer.Gender;
        existing.Occupation = customer.Occupation;
        if (!string.IsNullOrEmpty(customer.ProfileImage))
        {
            existing.ProfileImage = customer.ProfileImage;
        }
        existing.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            existing.UserId,
            "Profile Updated",
            "Customer",
            existing.CustomerId.ToString(),
            $"Customer details updated for {existing.FullName} ({existing.CustomerCode})",
            null,
            cancellationToken);

        return ServiceResult<Customer>.Ok(existing, "Customer updated successfully.");
    }

    public async Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetByIdAsync(customerId, cancellationToken);
    }

    public async Task<Customer?> GetCustomerByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetByUserIdAsync(userId, cancellationToken);
    }

    public async Task<Customer?> GetCustomerDetailsAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _customerRepository.GetCustomerWithDetailsAsync(customerId, cancellationToken);
    }

    public async Task<ServiceResult> ChangeCustomerStatusAsync(int customerId, bool isActive, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
        {
            return ServiceResult.Failed("Customer not found.");
        }

        customer.Status = isActive ? CustomerStatus.Active : CustomerStatus.Inactive;
        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            customer.UserId,
            isActive ? "Customer Activated" : "Customer Deactivated",
            "Customer",
            customer.CustomerId.ToString(),
            $"Customer status changed to {(isActive ? "Active" : "Inactive")}",
            null,
            cancellationToken);

        return ServiceResult.Ok($"Customer successfully {(isActive ? "activated" : "deactivated")}.");
    }
}
