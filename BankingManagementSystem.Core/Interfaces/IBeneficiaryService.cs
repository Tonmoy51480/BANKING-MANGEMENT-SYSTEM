using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface IBeneficiaryService
{
    Task<ServiceResult<Beneficiary>> AddBeneficiaryAsync(int customerId, string accountNumber, string name, string bankName, string? nickname, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteBeneficiaryAsync(int beneficiaryId, int customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Beneficiary>> GetBeneficiariesByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
}
