using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderByDescending(a => a.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}
