using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface IBeneficiaryRepository : IRepository<Beneficiary>
{
    Task<IEnumerable<Beneficiary>> GetBeneficiariesByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
}
