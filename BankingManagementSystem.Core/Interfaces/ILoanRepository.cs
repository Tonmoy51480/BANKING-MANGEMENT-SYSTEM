using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface ILoanRepository : IRepository<Loan>
{
    Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default);
    Task<Loan?> GetLoanWithDetailsAsync(int loanId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Loan>> GetLoansByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<string> GenerateNextLoanNumberAsync(CancellationToken cancellationToken = default);
}
