using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, int count = 20, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var unread = await _dbSet.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(cancellationToken);
        foreach (var item in unread)
        {
            item.IsRead = true;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}
