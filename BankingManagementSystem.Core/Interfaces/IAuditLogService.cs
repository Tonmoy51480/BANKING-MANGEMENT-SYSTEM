using BankingManagementSystem.Core.Entities;

namespace BankingManagementSystem.Core.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string? userId, string action, string? entityName, string? entityId, string? description, string? ipAddress, CancellationToken cancellationToken = default);
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100, CancellationToken cancellationToken = default);
}
