using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;

namespace BankingManagementSystem.Core.Interfaces;

public interface ICardService
{
    Task<ServiceResult<Card>> IssueCardAsync(int customerId, int accountId, CardType cardType, string cardHolderName, CancellationToken cancellationToken = default);
    Task<ServiceResult> ChangeCardStatusAsync(int cardId, CardStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Card>> GetCardsByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
}
