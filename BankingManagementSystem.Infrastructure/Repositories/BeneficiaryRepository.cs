using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Repositories;

public class BeneficiaryRepository : Repository<Beneficiary>, IBeneficiaryRepository
{
    public BeneficiaryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Beneficiary>> GetBeneficiariesByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
