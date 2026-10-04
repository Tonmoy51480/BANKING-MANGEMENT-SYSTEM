using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Repositories;

public class TransferRepository : Repository<Transfer>, ITransferRepository
{
    public TransferRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Transfer?> GetByReferenceAsync(string reference, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.SenderAccount)
                .ThenInclude(sa => sa.Customer)
            .Include(t => t.ReceiverAccount)
                .ThenInclude(ra => ra.Customer)
            .Include(t => t.Transactions)
            .FirstOrDefaultAsync(t => t.TransferReference == reference, cancellationToken);
    }

    public async Task<IEnumerable<Transfer>> GetTransfersByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.SenderAccount)
            .Include(t => t.ReceiverAccount)
            .Where(t => t.SenderAccountId == accountId || t.ReceiverAccountId == accountId)
            .OrderByDescending(t => t.TransferDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<string> GenerateNextTransferReferenceAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var count = await _dbSet.CountAsync(t => t.TransferDate.Date == DateTime.UtcNow.Date, cancellationToken);
        return $"{BankingConstants.TransferRefPrefix}{today}-{(count + 1):D6}";
    }
}
