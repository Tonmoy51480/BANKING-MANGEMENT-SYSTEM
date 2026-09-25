using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Interfaces;

namespace BankingManagementSystem.Infrastructure.Services;

public class BeneficiaryService : IBeneficiaryService
{
    private readonly IBeneficiaryRepository _beneficiaryRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public BeneficiaryService(
        IBeneficiaryRepository beneficiaryRepository,
        ICustomerRepository customerRepository,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork)
    {
        _beneficiaryRepository = beneficiaryRepository;
        _customerRepository = customerRepository;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<Beneficiary>> AddBeneficiaryAsync(int customerId, string accountNumber, string name, string bankName, string? nickname, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null)
        {
            return ServiceResult<Beneficiary>.Failed("Customer not found.");
        }

        var beneficiary = new Beneficiary
        {
            CustomerId = customerId,
            AccountNumber = accountNumber.Trim(),
            Name = name.Trim(),
            BankName = bankName.Trim(),
            Nickname = string.IsNullOrWhiteSpace(nickname) ? null : nickname.Trim(),
            Status = true,
            CreatedAt = DateTime.UtcNow
        };

        await _beneficiaryRepository.AddAsync(beneficiary, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            customer.UserId,
            "Beneficiary Added",
            "Beneficiary",
            beneficiary.BeneficiaryId.ToString(),
            $"Added beneficiary {beneficiary.Name} ({beneficiary.AccountNumber} - {beneficiary.BankName})",
            null,
            cancellationToken);

        return ServiceResult<Beneficiary>.Ok(beneficiary, "Beneficiary added successfully.");
    }

    public async Task<ServiceResult> DeleteBeneficiaryAsync(int beneficiaryId, int customerId, CancellationToken cancellationToken = default)
    {
        var beneficiary = await _beneficiaryRepository.GetByIdAsync(beneficiaryId, cancellationToken);
        if (beneficiary == null || beneficiary.CustomerId != customerId)
        {
            return ServiceResult.Failed("Beneficiary not found.");
        }

        _beneficiaryRepository.Remove(beneficiary);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Ok("Beneficiary removed successfully.");
    }

    public async Task<IEnumerable<Beneficiary>> GetBeneficiariesByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _beneficiaryRepository.GetBeneficiariesByCustomerIdAsync(customerId, cancellationToken);
    }
}
