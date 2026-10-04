using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Services;
using Moq;
using Xunit;

namespace BankingManagementSystem.Tests.Unit;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<IAccountRepository> _mockAccountRepo;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly TransactionService _transactionService;

    public TransactionServiceTests()
    {
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockAccountRepo = new Mock<IAccountRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _transactionService = new TransactionService(
            _mockTransactionRepo.Object,
            _mockAccountRepo.Object,
            _mockAuditLogService.Object,
            _mockNotificationService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task DepositAsync_PositiveAmount_IncreasesBalanceAndSucceeds()
    {
        // Arrange
        var account = new Account
        {
            AccountId = 1,
            AccountNumber = "BD-BNK-10000001",
            Balance = 1000.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active,
            Customer = new Customer { CustomerId = 1, FullName = "John Doe", UserId = "user-1" }
        };

        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsAsync(1, default)).ReturnsAsync(account);
        _mockTransactionRepo.Setup(r => r.GenerateNextTransactionReferenceAsync(default)).ReturnsAsync("TXN-20261003-000001");

        // Act
        var result = await _transactionService.DepositAsync(1, 500.00m, "Test Deposit", "admin-1", "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1500.00m, account.Balance);
        Assert.Equal(1500.00m, result.Data!.BalanceAfterTransaction);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(default), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task DepositAsync_ZeroOrNegativeAmount_ReturnsFailedResult(decimal invalidAmount)
    {
        // Act
        var result = await _transactionService.DepositAsync(1, invalidAmount, "Invalid deposit", "admin-1", "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("must be strictly greater than zero", result.Message);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(default), Times.Never);
    }

    [Fact]
    public async Task WithdrawAsync_SufficientFunds_DecreasesBalanceAndSucceeds()
    {
        // Arrange
        var account = new Account
        {
            AccountId = 1,
            AccountNumber = "BD-BNK-10000001",
            Balance = 1000.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active,
            Customer = new Customer { CustomerId = 1, FullName = "John Doe", UserId = "user-1" },
            AccountType = new AccountType { DailyWithdrawalLimit = 5000.00m }
        };

        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsAsync(1, default)).ReturnsAsync(account);
        _mockTransactionRepo.Setup(r => r.GenerateNextTransactionReferenceAsync(default)).ReturnsAsync("TXN-20261003-000002");
        _mockTransactionRepo.Setup(r => r.GetDailyWithdrawalTotalAsync(1, It.IsAny<DateTime>(), default)).ReturnsAsync(200.00m);

        // Act
        var result = await _transactionService.WithdrawAsync(1, 300.00m, "Test Withdrawal", "admin-1", "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(700.00m, account.Balance);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task WithdrawAsync_AmountGreaterThanBalance_ReturnsInsufficientFunds()
    {
        // Arrange
        var account = new Account
        {
            AccountId = 1,
            Balance = 500.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active
        };

        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsAsync(1, default)).ReturnsAsync(account);

        // Act
        var result = await _transactionService.WithdrawAsync(1, 600.00m, "Exceeds balance", "admin-1", "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Insufficient funds", result.Message);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(default), Times.Never);
    }

    [Fact]
    public async Task WithdrawAsync_ViolatesMinimumBalance_ReturnsError()
    {
        // Arrange
        var account = new Account
        {
            AccountId = 1,
            Balance = 500.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active
        };

        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsAsync(1, default)).ReturnsAsync(account);

        // Act
        var result = await _transactionService.WithdrawAsync(1, 450.00m, "Drop below min balance", "admin-1", "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("minimum balance", result.Message);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(default), Times.Never);
    }

    [Fact]
    public async Task WithdrawAsync_ExceedsDailyLimit_ReturnsError()
    {
        // Arrange
        var account = new Account
        {
            AccountId = 1,
            Balance = 10000.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active,
            AccountType = new AccountType { DailyWithdrawalLimit = 2000.00m }
        };

        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsAsync(1, default)).ReturnsAsync(account);
        _mockTransactionRepo.Setup(r => r.GetDailyWithdrawalTotalAsync(1, It.IsAny<DateTime>(), default)).ReturnsAsync(1800.00m);

        // Act: Attempt to withdraw $500 when only $200 daily limit remains
        var result = await _transactionService.WithdrawAsync(1, 500.00m, "Exceeds daily limit", "admin-1", "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("daily limit", result.Message);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(default), Times.Never);
    }
}
