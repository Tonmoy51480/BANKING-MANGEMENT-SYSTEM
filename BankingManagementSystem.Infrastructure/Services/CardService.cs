using BankingManagementSystem.Core.DTOs;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;

namespace BankingManagementSystem.Infrastructure.Services;

public class CardService : ICardService
{
    private readonly ICardRepository _cardRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public CardService(
        ICardRepository cardRepository,
        ICustomerRepository customerRepository,
        IAccountRepository accountRepository,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork)
    {
        _cardRepository = cardRepository;
        _customerRepository = customerRepository;
        _accountRepository = accountRepository;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<Card>> IssueCardAsync(int customerId, int accountId, CardType cardType, string cardHolderName, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null || customer.Status != CustomerStatus.Active)
        {
            return ServiceResult<Card>.Failed("Customer not found or not active.");
        }

        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null || account.CustomerId != customerId || account.Status != AccountStatus.Active)
        {
            return ServiceResult<Card>.Failed("Account is not active or does not belong to the customer.");
        }

        // Generate masked card number for PCI-DSS compliance demo
        var rand = new Random();
        var prefix = cardType == CardType.Debit ? "4" : "5";
        var prefixDigits = $"{prefix}{rand.Next(100, 999)}";
        var lastDigits = rand.Next(1000, 9999).ToString();
        var maskedNumber = $"{prefixDigits} **** **** {lastDigits}";

        var card = new Card
        {
            CustomerId = customerId,
            AccountId = accountId,
            CardHolderName = string.IsNullOrWhiteSpace(cardHolderName) ? customer.FullName : cardHolderName.Trim(),
            CardNumberMasked = maskedNumber,
            CardType = cardType,
            IssueDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(4),
            Status = CardStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _cardRepository.AddAsync(card, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            customer.UserId,
            "Card Issued",
            "Card",
            card.CardId.ToString(),
            $"Issued {cardType} card {maskedNumber} for account {account.AccountNumber}",
            null,
            cancellationToken);

        return ServiceResult<Card>.Ok(card, "Card issued successfully.");
    }

    public async Task<ServiceResult> ChangeCardStatusAsync(int cardId, CardStatus status, CancellationToken cancellationToken = default)
    {
        var card = await _cardRepository.GetByIdAsync(cardId, cancellationToken);
        if (card == null)
        {
            return ServiceResult.Failed("Card not found.");
        }

        card.Status = status;
        _cardRepository.Update(card);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAsync(
            null,
            $"Card Status Changed to {status}",
            "Card",
            card.CardId.ToString(),
            $"Card {card.CardNumberMasked} status updated to {status}",
            null,
            cancellationToken);

        return ServiceResult.Ok($"Card status changed to {status}.");
    }

    public async Task<IEnumerable<Card>> GetCardsByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _cardRepository.GetCardsByCustomerIdAsync(customerId, cancellationToken);
    }
}
