using BankingManagementSystem.Core.DTOs;

namespace BankingManagementSystem.Core.Interfaces;

public interface IDashboardService
{
    Task<AdminDashboardDto> GetAdminDashboardDataAsync(CancellationToken cancellationToken = default);
    Task<CustomerDashboardDto> GetCustomerDashboardDataAsync(int customerId, CancellationToken cancellationToken = default);
}
