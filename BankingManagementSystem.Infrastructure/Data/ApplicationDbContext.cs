using BankingManagementSystem.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BankingManagementSystem.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountType> AccountTypes => Set<AccountType>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Customer Configurations
        builder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(c => c.CustomerId);
            entity.Property(c => c.CustomerCode).IsRequired().HasMaxLength(20);
            entity.HasIndex(c => c.CustomerCode).IsUnique();
            entity.Property(c => c.FullName).IsRequired().HasMaxLength(100);
            entity.Property(c => c.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(c => c.Email).IsUnique();
            entity.Property(c => c.Phone).IsRequired().HasMaxLength(20);
            entity.Property(c => c.NID).IsRequired().HasMaxLength(30);
            entity.HasIndex(c => c.NID).IsUnique();
            entity.Property(c => c.Address).IsRequired().HasMaxLength(250);
            entity.Property(c => c.Occupation).HasMaxLength(100);
            entity.Property(c => c.ProfileImage).HasMaxLength(255);

            entity.HasOne(c => c.User)
                  .WithOne(u => u.Customer)
                  .HasForeignKey<Customer>(c => c.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Branch Configurations
        builder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branches");
            entity.HasKey(b => b.BranchId);
            entity.Property(b => b.BranchCode).IsRequired().HasMaxLength(20);
            entity.HasIndex(b => b.BranchCode).IsUnique();
            entity.Property(b => b.BranchName).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Address).IsRequired().HasMaxLength(200);
            entity.Property(b => b.Phone).HasMaxLength(20);
            entity.Property(b => b.Email).HasMaxLength(100);
            entity.Property(b => b.RoutingNumber).HasMaxLength(50);
        });

        // AccountType Configurations
        builder.Entity<AccountType>(entity =>
        {
            entity.ToTable("AccountTypes");
            entity.HasKey(a => a.AccountTypeId);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(a => a.Code).IsUnique();
            entity.Property(a => a.Description).HasMaxLength(255);
            entity.Property(a => a.MinimumBalance).HasColumnType("decimal(18,2)");
            entity.Property(a => a.InterestRate).HasColumnType("decimal(5,2)");
            entity.Property(a => a.DailyWithdrawalLimit).HasColumnType("decimal(18,2)");
        });

        // Account Configurations
        builder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts");
            entity.HasKey(a => a.AccountId);
            entity.Property(a => a.AccountNumber).IsRequired().HasMaxLength(30);
            entity.HasIndex(a => a.AccountNumber).IsUnique();
            entity.Property(a => a.Balance).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(a => a.MinimumBalance).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(a => a.RowVersion).IsRowVersion();

            entity.HasOne(a => a.Customer)
                  .WithMany(c => c.Accounts)
                  .HasForeignKey(a => a.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.AccountType)
                  .WithMany(at => at.Accounts)
                  .HasForeignKey(a => a.AccountTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Branch)
                  .WithMany(b => b.Accounts)
                  .HasForeignKey(a => a.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Transaction Configurations
        builder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(t => t.TransactionId);
            entity.Property(t => t.TransactionReference).IsRequired().HasMaxLength(50);
            entity.HasIndex(t => t.TransactionReference).IsUnique();
            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(t => t.BalanceAfterTransaction).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(t => t.Description).HasMaxLength(250);
            entity.Property(t => t.PerformedByUserId).HasMaxLength(450);

            entity.HasOne(t => t.Account)
                  .WithMany(a => a.Transactions)
                  .HasForeignKey(t => t.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Transfer)
                  .WithMany(tr => tr.Transactions)
                  .HasForeignKey(t => t.TransferId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(t => t.TransactionDate);
            entity.HasIndex(t => t.TransactionType);
        });

        // Transfer Configurations
        builder.Entity<Transfer>(entity =>
        {
            entity.ToTable("Transfers");
            entity.HasKey(t => t.TransferId);
            entity.Property(t => t.TransferReference).IsRequired().HasMaxLength(50);
            entity.HasIndex(t => t.TransferReference).IsUnique();
            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(t => t.Description).HasMaxLength(250);
            entity.Property(t => t.InitiatedByUserId).HasMaxLength(450);

            entity.HasOne(t => t.SenderAccount)
                  .WithMany(a => a.OutgoingTransfers)
                  .HasForeignKey(t => t.SenderAccountId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.ReceiverAccount)
                  .WithMany(a => a.IncomingTransfers)
                  .HasForeignKey(t => t.ReceiverAccountId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => t.TransferDate);
        });

        // Beneficiary Configurations
        builder.Entity<Beneficiary>(entity =>
        {
            entity.ToTable("Beneficiaries");
            entity.HasKey(b => b.BeneficiaryId);
            entity.Property(b => b.AccountNumber).IsRequired().HasMaxLength(30);
            entity.Property(b => b.Name).IsRequired().HasMaxLength(100);
            entity.Property(b => b.BankName).IsRequired().HasMaxLength(100);
            entity.Property(b => b.Nickname).HasMaxLength(50);

            entity.HasOne(b => b.Customer)
                  .WithMany(c => c.Beneficiaries)
                  .HasForeignKey(b => b.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Card Configurations
        builder.Entity<Card>(entity =>
        {
            entity.ToTable("Cards");
            entity.HasKey(c => c.CardId);
            entity.Property(c => c.CardNumberMasked).IsRequired().HasMaxLength(25);
            entity.Property(c => c.CardHolderName).IsRequired().HasMaxLength(100);

            entity.HasOne(c => c.Customer)
                  .WithMany(cust => cust.Cards)
                  .HasForeignKey(c => c.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Account)
                  .WithMany(a => a.Cards)
                  .HasForeignKey(c => c.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Loan Configurations
        builder.Entity<Loan>(entity =>
        {
            entity.ToTable("Loans");
            entity.HasKey(l => l.LoanId);
            entity.Property(l => l.LoanNumber).IsRequired().HasMaxLength(30);
            entity.HasIndex(l => l.LoanNumber).IsUnique();
            entity.Property(l => l.PrincipalAmount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(l => l.InterestRate).HasColumnType("decimal(5,2)").IsRequired();
            entity.Property(l => l.MonthlyPayment).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(l => l.TotalRepaymentAmount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(l => l.RemainingBalance).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(l => l.ApprovedByUserId).HasMaxLength(450);
            entity.Property(l => l.Remarks).HasMaxLength(500);

            entity.HasOne(l => l.Customer)
                  .WithMany(c => c.Loans)
                  .HasForeignKey(l => l.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Account)
                  .WithMany(a => a.Loans)
                  .HasForeignKey(l => l.AccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // LoanPayment Configurations
        builder.Entity<LoanPayment>(entity =>
        {
            entity.ToTable("LoanPayments");
            entity.HasKey(lp => lp.PaymentId);
            entity.Property(lp => lp.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(lp => lp.PrincipalComponent).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(lp => lp.InterestComponent).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(lp => lp.RemainingBalance).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(lp => lp.TransactionReference).IsRequired().HasMaxLength(50);
            entity.Property(lp => lp.Notes).HasMaxLength(250);

            entity.HasOne(lp => lp.Loan)
                  .WithMany(l => l.LoanPayments)
                  .HasForeignKey(lp => lp.LoanId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Employee Configurations
        builder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employees");
            entity.HasKey(e => e.EmployeeId);
            entity.Property(e => e.EmployeeCode).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.EmployeeCode).IsUnique();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Designation).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);

            entity.HasOne(e => e.User)
                  .WithOne(u => u.Employee)
                  .HasForeignKey<Employee>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Branch)
                  .WithMany(b => b.Employees)
                  .HasForeignKey(e => e.BranchId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog Configurations
        builder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(a => a.AuditLogId);
            entity.Property(a => a.UserId).HasMaxLength(450);
            entity.Property(a => a.Action).IsRequired().HasMaxLength(100);
            entity.Property(a => a.EntityName).HasMaxLength(100);
            entity.Property(a => a.EntityId).HasMaxLength(100);
            entity.Property(a => a.Description).HasMaxLength(1000);
            entity.Property(a => a.IpAddress).HasMaxLength(50);
            entity.HasIndex(a => a.CreatedAt);
        });

        // Notification Configurations
        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(n => n.NotificationId);
            entity.Property(n => n.UserId).IsRequired().HasMaxLength(450);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(150);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(500);
            entity.Property(n => n.Type).HasMaxLength(50);
            entity.HasIndex(n => n.UserId);
        });
    }
}
