using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Repositories;

public class LoanRepository : Repository<Loan>, ILoanRepository
{
    public LoanRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.Customer)
            .Include(l => l.Account)
            .Include(l => l.LoanPayments.OrderByDescending(p => p.PaymentDate))
            .FirstOrDefaultAsync(l => l.LoanNumber == loanNumber, cancellationToken);
    }

    public async Task<Loan?> GetLoanWithDetailsAsync(int loanId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.Customer)
            .Include(l => l.Account)
            .Include(l => l.LoanPayments.OrderByDescending(p => p.PaymentDate))
            .FirstOrDefaultAsync(l => l.LoanId == loanId, cancellationToken);
    }

    public async Task<IEnumerable<Loan>> GetLoansByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(l => l.Account)
            .Include(l => l.LoanPayments)
            .Where(l => l.CustomerId == customerId)
            .OrderByDescending(l => l.ApplicationDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateNextLoanNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _dbSet.CountAsync(l => l.ApplicationDate.Year == year, cancellationToken);
        return $"{BankingConstants.LoanNumberPrefix}{year}-{(count + 1):D4}";
    }
}
