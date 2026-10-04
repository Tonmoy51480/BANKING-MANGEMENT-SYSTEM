using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using BankingManagementSystem.Core.Interfaces;
using BankingManagementSystem.Infrastructure.Services;
using Moq;
using Xunit;

namespace BankingManagementSystem.Tests.Unit;

public class LoanServiceTests
{
    private readonly Mock<ILoanRepository> _mockLoanRepo;
    private readonly Mock<IRepository<LoanPayment>> _mockLoanPaymentRepo;
    private readonly Mock<IAccountRepository> _mockAccountRepo;
    private readonly Mock<ITransactionRepository> _mockTransactionRepo;
    private readonly Mock<ICustomerRepository> _mockCustomerRepo;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly LoanService _loanService;

    public LoanServiceTests()
    {
        _mockLoanRepo = new Mock<ILoanRepository>();
        _mockLoanPaymentRepo = new Mock<IRepository<LoanPayment>>();
        _mockAccountRepo = new Mock<IAccountRepository>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockCustomerRepo = new Mock<ICustomerRepository>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _loanService = new LoanService(
            _mockLoanRepo.Object,
            _mockLoanPaymentRepo.Object,
            _mockAccountRepo.Object,
            _mockTransactionRepo.Object,
            _mockCustomerRepo.Object,
            _mockAuditLogService.Object,
            _mockNotificationService.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public void CalculateMonthlyPayment_ValidParameters_ComputesCorrectEMI()
    {
        // Principal: $10,000, 8.5% annual rate, 24 months
        // Standard financial formula gives approx $454.56
        var emi = _loanService.CalculateMonthlyPayment(10000m, 8.5m, 24);

        Assert.Equal(454.56m, emi);
    }

    [Fact]
    public void CalculateMonthlyPayment_ZeroInterest_DividesEvenly()
    {
        // Principal: $12,000, 0% annual rate, 12 months -> $1,000
        var emi = _loanService.CalculateMonthlyPayment(12000m, 0m, 12);

        Assert.Equal(1000m, emi);
    }

    [Fact]
    public async Task ApproveLoanAsync_PendingLoan_DisbursesFundsToCustomerAccount()
    {
        // Arrange
        var customer = new Customer { CustomerId = 1, FullName = "Jane Doe", UserId = "user-1" };
        var account = new Account
        {
            AccountId = 10,
            AccountNumber = "BD-BNK-10000010",
            Balance = 1500.00m,
            Status = AccountStatus.Active,
            Customer = customer
        };
        var loan = new Loan
        {
            LoanId = 5,
            LoanNumber = "LN-2026-0005",
            CustomerId = 1,
            AccountId = 10,
            PrincipalAmount = 5000.00m,
            Status = LoanStatus.Pending,
            Customer = customer
        };

        _mockLoanRepo.Setup(r => r.GetLoanWithDetailsAsync(5, default)).ReturnsAsync(loan);
        _mockAccountRepo.Setup(r => r.GetByIdAsync(10, default)).ReturnsAsync(account);
        _mockTransactionRepo.Setup(r => r.GenerateNextTransactionReferenceAsync(default)).ReturnsAsync("TXN-20261003-000099");

        // Act
        var result = await _loanService.ApproveLoanAsync(5, "admin-1", "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(6500.00m, account.Balance); // $1500 + $5000 disbursed
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(default), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task PayLoanInstallmentAsync_ValidAmount_DeductsFromAccountAndReducesRemainingBalance()
    {
        // Arrange
        var customer = new Customer { CustomerId = 1, FullName = "Jane Doe", UserId = "user-1" };
        var account = new Account
        {
            AccountId = 10,
            AccountNumber = "BD-BNK-10000010",
            Balance = 3000.00m,
            MinimumBalance = 100.00m,
            Status = AccountStatus.Active,
            Customer = customer
        };
        var loan = new Loan
        {
            LoanId = 5,
            LoanNumber = "LN-2026-0005",
            CustomerId = 1,
            AccountId = 10,
            PrincipalAmount = 5000.00m,
            RemainingBalance = 4000.00m,
            Status = LoanStatus.Active,
            InterestRate = 6.0m
        };

        _mockLoanRepo.Setup(r => r.GetLoanWithDetailsAsync(5, default)).ReturnsAsync(loan);
        _mockAccountRepo.Setup(r => r.GetAccountWithDetailsAsync(10, default)).ReturnsAsync(account);
        _mockTransactionRepo.Setup(r => r.GenerateNextTransactionReferenceAsync(default)).ReturnsAsync("TXN-20261003-000100");

        // Act: Pay $500 installment
        var result = await _loanService.PayLoanInstallmentAsync(5, 500.00m, "user-1", "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2500.00m, account.Balance);
        Assert.Equal(3500.00m, loan.RemainingBalance);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }
}
