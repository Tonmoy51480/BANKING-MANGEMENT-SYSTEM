using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Services;
using Moq;
using Xunit;

namespace BankingManagementSystem.Tests.Unit;

public class TransferServiceTests
{
    private readonly Mock<ITransferRepository> _mockTransferRepo;
    private readonly Mock<IAccountRepository> _mockAccountRepo;
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly TransferService _transferService;

    public TransferServiceTests()
    {
        _mockTransferRepo = new Mock<ITransferRepository>();
        _mockAccountRepo = new Mock<IAccountRepository>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _transferService = new TransferService(
            _mockTransferRepo.Object,
            _mockAccountRepo.Object,
            _mockTransactionRepo.Object,
            _mockAuditLogService.Object,
            _mockNotificationService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task TransferFundsAsync_ValidRequest_UpdatesBalancesAndCommitsTransaction()
    {
        // Arrange
        var senderAcc = new Account
        {
            AccountId = 1,
            AccountNumber = "BD-BNK-10000001",
            Balance = 5000.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active,
            Customer = new Customer { CustomerId = 1, FullName = "Sender User", UserId = "user-1" }
        };

        var receiverAcc = new Account
        {
            AccountId = 2,
            AccountNumber = "BD-BNK-10000002",
            Balance = 2000.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active,
            Customer = new Customer { CustomerId = 2, FullName = "Receiver User", UserId = "user-2" }
        };

        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsByNumberAsync("BD-BNK-10000001", default)).ReturnsAsync(senderAcc);
        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsByNumberAsync("BD-BNK-10000002", default)).ReturnsAsync(receiverAcc);
        _mockTransferRepo.Setup(r => r.GenerateNextTransferReferenceAsync(default)).ReturnsAsync("TRF-20261003-000001");
        _mockTransactionRepo.Setup(r => r.GenerateNextTransactionReferenceAsync(default)).ReturnsAsync("TXN-20261003-000010");

        // Act
        var result = await _transferService.TransferFundsAsync(
            "BD-BNK-10000001",
            "BD-BNK-10000002",
            1000.00m,
            "Rent payment",
            "user-1",
            "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(4000.00m, senderAcc.Balance);
        Assert.Equal(3000.00m, receiverAcc.Balance);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(default), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(default), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(default), Times.Never);
    }

    [Fact]
    public async Task TransferFundsAsync_SameAccount_FailsValidation()
    {
        // Act
        var result = await _transferService.TransferFundsAsync(
            "BD-BNK-10000001",
            "BD-BNK-10000001",
            500.00m,
            "Self transfer",
            "user-1",
            "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be the same", result.Message);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(default), Times.Never);
    }

    [Fact]
    public async Task TransferFundsAsync_InsufficientSenderFunds_FailsValidation()
    {
        // Arrange
        var senderAcc = new Account
        {
            AccountId = 1,
            AccountNumber = "BD-BNK-10000001",
            Balance = 400.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active
        };
        var receiverAcc = new Account
        {
            AccountId = 2,
            AccountNumber = "BD-BNK-10000002",
            Balance = 1000.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active
        };

        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsByNumberAsync("BD-BNK-10000001", default)).ReturnsAsync(senderAcc);
        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsByNumberAsync("BD-BNK-10000002", default)).ReturnsAsync(receiverAcc);

        // Act
        var result = await _transferService.TransferFundsAsync(
            "BD-BNK-10000001",
            "BD-BNK-10000002",
            500.00m,
            "Transfer",
            "user-1",
            "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Insufficient funds", result.Message);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(default), Times.Never);
    }

    [Fact]
    public async Task TransferFundsAsync_ExceptionDuringSave_RollsBackTransaction()
    {
        // Arrange
        var senderAcc = new Account
        {
            AccountId = 1,
            AccountNumber = "BD-BNK-10000001",
            Balance = 5000.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active
        };
        var receiverAcc = new Account
        {
            AccountId = 2,
            AccountNumber = "BD-BNK-10000002",
            Balance = 2000.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active
        };

        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsByNumberAsync("BD-BNK-10000001", default)).ReturnsAsync(senderAcc);
        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsByNumberAsync("BD-BNK-10000002", default)).ReturnsAsync(receiverAcc);
        _mockTransferRepo.Setup(r => r.GenerateNextTransferReferenceAsync(default)).ReturnsAsync("TRF-20261003-000001");

        // Simulate database exception during SaveChangesAsync inside the transaction
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default)).ThrowsAsync(new InvalidOperationException("DB Concurrency Lock Error"));

        // Act
        var result = await _transferService.TransferFundsAsync(
            "BD-BNK-10000001",
            "BD-BNK-10000002",
            1000.00m,
            "Rent payment",
            "user-1",
            "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("rolled back", result.Message);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(default), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(default), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(default), Times.Never);
    }
}
