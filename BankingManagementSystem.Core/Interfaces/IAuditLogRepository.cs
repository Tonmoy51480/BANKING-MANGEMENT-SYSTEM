using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100, CancellationToken cancellationToken = default);
}
