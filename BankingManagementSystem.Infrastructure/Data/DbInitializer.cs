using BankingManagementSystem.Core.Constants;
using BankingManagementSystem.Core.Entities;
using BankingManagementSystem.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BankingManagementSystem.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Ensure database is created and migrated
        await context.Database.MigrateAsync();

        // 1. Seed Roles
        string[] roles = { Roles.Admin, Roles.Employee, Roles.Customer };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role, $"System {role} Role"));
            }
        }

        // 2. Seed Branches
        if (!await context.Branches.AnyAsync())
        {
            var branches = new List<Branch>
            {
                new()
                {
                    BranchCode = "BR-HQ-01",
                    BranchName = "Metropolitan Headquarters",
                    Address = "100 Financial District Blvd, Suite 500, New York, NY",
                    Phone = "+1 (212) 555-0100",
                    Email = "hq@bankingsystem.com",
                    RoutingNumber = "021000021",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddYears(-2)
                },
                new()
                {
                    BranchCode = "BR-UPT-02",
                    BranchName = "Uptown Financial Centre",
                    Address = "450 Lexington Avenue, New York, NY",
                    Phone = "+1 (212) 555-0200",
                    Email = "uptown@bankingsystem.com",
                    RoutingNumber = "021000089",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddYears(-2)
                },
                new()
                {
                    BranchCode = "BR-TECH-03",
                    BranchName = "Silicon Valley Tech Branch",
                    Address = "3000 Sand Hill Road, Menlo Park, CA",
                    Phone = "+1 (650) 555-0300",
                    Email = "silicon@bankingsystem.com",
                    RoutingNumber = "121000358",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddYears(-1)
                }
            };

            await context.Branches.AddRangeAsync(branches);
            await context.SaveChangesAsync();
        }

        var branchesList = await context.Branches.ToListAsync();
        var mainBranch = branchesList[0];
        var uptownBranch = branchesList[1];
        var techBranch = branchesList[2];

        // 3. Seed Account Types
        if (!await context.AccountTypes.AnyAsync())
        {
            var accountTypes = new List<AccountType>
            {
                new()
                {
                    Name = "Savings Account",
                    Code = "SAVINGS",
                    Description = "High-yield interest bearing account for personal savings and wealth growth.",
                    MinimumBalance = BankingConstants.DefaultSavingsMinBalance,
                    InterestRate = 4.50m,
                    DailyWithdrawalLimit = BankingConstants.DefaultDailyWithdrawalLimit,
                    IsActive = true
                },
                new()
                {
                    Name = "Current Account",
                    Code = "CURRENT",
                    Description = "Designed for daily business transactions with zero limits on frequent deposits.",
                    MinimumBalance = BankingConstants.DefaultCurrentMinBalance,
                    InterestRate = 1.00m,
                    DailyWithdrawalLimit = 10000.00m,
                    IsActive = true
                },
                new()
                {
                    Name = "Salary Account",
                    Code = "SALARY",
                    Description = "Zero-balance corporate payroll account with comprehensive banking perks.",
                    MinimumBalance = BankingConstants.DefaultSalaryMinBalance,
                    InterestRate = 3.50m,
                    DailyWithdrawalLimit = 3000.00m,
                    IsActive = true
                }
            };

            await context.AccountTypes.AddRangeAsync(accountTypes);
            await context.SaveChangesAsync();
        }

        var accountTypesList = await context.AccountTypes.ToListAsync();
        var savingsType = accountTypesList.First(t => t.Code == "SAVINGS");
        var currentType = accountTypesList.First(t => t.Code == "CURRENT");
        var salaryType = accountTypesList.First(t => t.Code == "SALARY");

        // 4. Seed Admin User
        var adminEmail = "admin@bank.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                PhoneNumber = "+1 (555) 019-2831",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddYears(-1)
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            }
        }

        // 5. Seed 2 Employees
        var emp1Email = "manager@bank.com";
        var emp1User = await userManager.FindByEmailAsync(emp1Email);
        if (emp1User == null)
        {
            emp1User = new ApplicationUser
            {
                UserName = emp1Email,
                Email = emp1Email,
                FullName = "Marcus Vance",
                PhoneNumber = "+1 (555) 019-1122",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-10)
            };
            var result = await userManager.CreateAsync(emp1User, "Employee@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(emp1User, Roles.Employee);

                var employee = new Employee
                {
                    EmployeeCode = "EMP-1001",
                    UserId = emp1User.Id,
                    BranchId = mainBranch.BranchId,
                    FullName = emp1User.FullName,
                    Email = emp1Email,
                    Phone = emp1User.PhoneNumber,
                    Designation = "Branch Operations Manager",
                    Department = "Operations",
                    HireDate = DateTime.UtcNow.AddMonths(-10),
                    IsActive = true
                };
                await context.Employees.AddAsync(employee);
            }
        }

        var emp2Email = "teller@bank.com";
        var emp2User = await userManager.FindByEmailAsync(emp2Email);
        if (emp2User == null)
        {
            emp2User = new ApplicationUser
            {
                UserName = emp2Email,
                Email = emp2Email,
                FullName = "Sarah Jenkins",
                PhoneNumber = "+1 (555) 019-3344",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-8)
            };
            var result = await userManager.CreateAsync(emp2User, "Employee@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(emp2User, Roles.Employee);

                var employee = new Employee
                {
                    EmployeeCode = "EMP-1002",
                    UserId = emp2User.Id,
                    BranchId = uptownBranch.BranchId,
                    FullName = emp2User.FullName,
                    Email = emp2Email,
                    Phone = emp2User.PhoneNumber,
                    Designation = "Senior Bank Teller",
                    Department = "Customer Service",
                    HireDate = DateTime.UtcNow.AddMonths(-8),
                    IsActive = true
                };
                await context.Employees.AddAsync(employee);
            }
        }
        await context.SaveChangesAsync();

        // 6. Seed 5 Customers with Accounts, Transactions, Cards, Beneficiaries, and Loans
        if (!await context.Customers.AnyAsync())
        {
            var customerSeedData = new[]
            {
                new { Email = "customer1@bank.com", Name = "Jonathan Reynolds", Phone = "+1 (555) 771-0001", Nid = "NID-99281001", Dob = new DateTime(1988, 4, 15), Address = "742 Evergreen Terrace, Springfield", Gender = Gender.Male, Job = "Senior Software Architect", Branch = mainBranch },
                new { Email = "customer2@bank.com", Name = "Eleanor Vance", Phone = "+1 (555) 771-0002", Nid = "NID-99281002", Dob = new DateTime(1992, 8, 22), Address = "124 Conch Street, Pacific Bay", Gender = Gender.Female, Job = "Product Marketing Lead", Branch = uptownBranch },
                new { Email = "customer3@bank.com", Name = "Alexander Wright", Phone = "+1 (555) 771-0003", Nid = "NID-99281003", Dob = new DateTime(1985, 11, 30), Address = "221B Baker Street, Metro City", Gender = Gender.Male, Job = "Civil Engineer & Contractor", Branch = techBranch },
                new { Email = "customer4@bank.com", Name = "Sophia Martinez", Phone = "+1 (555) 771-0004", Nid = "NID-99281004", Dob = new DateTime(1995, 2, 18), Address = "432 Park Avenue Apt 14B, New York", Gender = Gender.Female, Job = "FinTech Financial Analyst", Branch = mainBranch },
                new { Email = "customer5@bank.com", Name = "David K. Sterling", Phone = "+1 (555) 771-0005", Nid = "NID-99281005", Dob = new DateTime(1980, 7, 9), Address = "88 Pinecrest Lane, Silicon Valley", Gender = Gender.Male, Job = "Venture Capital Partner", Branch = techBranch }
            };

            int custIndex = 1;
            var createdCustomers = new List<Customer>();
            var createdAccounts = new List<Account>();

            foreach (var cData in customerSeedData)
            {
                var custUser = new ApplicationUser
                {
                    UserName = cData.Email,
                    Email = cData.Email,
                    FullName = cData.Name,
                    PhoneNumber = cData.Phone,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };

                await userManager.CreateAsync(custUser, "Customer@123");
                await userManager.AddToRoleAsync(custUser, Roles.Customer);

                var customer = new Customer
                {
                    CustomerCode = $"CUST-{10000 + custIndex}",
                    UserId = custUser.Id,
                    FullName = cData.Name,
                    Email = cData.Email,
                    Phone = cData.Phone,
                    NID = cData.Nid,
                    DateOfBirth = cData.Dob,
                    Address = cData.Address,
                    Gender = cData.Gender,
                    Occupation = cData.Job,
                    Status = CustomerStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };

                await context.Customers.AddAsync(customer);
                await context.SaveChangesAsync();
                createdCustomers.Add(customer);

                // Create Primary Savings Account
                var savingsAccount = new Account
                {
                    AccountNumber = $"BD-BNK-{10000000 + (custIndex * 2) - 1}",
                    CustomerId = customer.CustomerId,
                    AccountTypeId = savingsType.AccountTypeId,
                    BranchId = cData.Branch.BranchId,
                    Balance = 15000.00m + (custIndex * 2500m),
                    MinimumBalance = savingsType.MinimumBalance,
                    Status = AccountStatus.Active,
                    OpenedDate = DateTime.UtcNow.AddMonths(-5),
                    CreatedAt = DateTime.UtcNow.AddMonths(-5)
                };
                await context.Accounts.AddAsync(savingsAccount);

                // Create Secondary Current Account
                var currentAccount = new Account
                {
                    AccountNumber = $"BD-BNK-{10000000 + (custIndex * 2)}",
                    CustomerId = customer.CustomerId,
                    AccountTypeId = currentType.AccountTypeId,
                    BranchId = cData.Branch.BranchId,
                    Balance = 8000.00m + (custIndex * 1500m),
                    MinimumBalance = currentType.MinimumBalance,
                    Status = AccountStatus.Active,
                    OpenedDate = DateTime.UtcNow.AddMonths(-4),
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                };
                await context.Accounts.AddAsync(currentAccount);
                await context.SaveChangesAsync();

                createdAccounts.Add(savingsAccount);
                createdAccounts.Add(currentAccount);

                // Initial Seed Transactions
                var depositTxn = new Transaction
                {
                    TransactionReference = $"TXN-20260401-{custIndex:D6}",
                    AccountId = savingsAccount.AccountId,
                    TransactionType = TransactionType.DEPOSIT,
                    Amount = savingsAccount.Balance,
                    BalanceAfterTransaction = savingsAccount.Balance,
                    Description = "Initial Account Opening Deposit",
                    Status = TransactionStatus.Completed,
                    TransactionDate = DateTime.UtcNow.AddMonths(-5),
                    PerformedByUserId = emp1User?.Id
                };
                await context.Transactions.AddAsync(depositTxn);

                // Issue Debit Card
                var card = new Card
                {
                    CustomerId = customer.CustomerId,
                    AccountId = savingsAccount.AccountId,
                    CardHolderName = customer.FullName.ToUpper(),
                    CardNumberMasked = $"4532 **** **** {1000 + custIndex * 111}",
                    CardType = CardType.Debit,
                    IssueDate = DateTime.UtcNow.AddMonths(-4),
                    ExpiryDate = DateTime.UtcNow.AddYears(3),
                    Status = CardStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                };
                await context.Cards.AddAsync(card);

                // Add Beneficiary for Customer
                var beneficiary = new Beneficiary
                {
                    CustomerId = customer.CustomerId,
                    AccountNumber = $"BD-BNK-990000{custIndex}",
                    Name = $"Family Contact {custIndex}",
                    BankName = "Global Standard Bank",
                    Nickname = "Family Member",
                    Status = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-3)
                };
                await context.Beneficiaries.AddAsync(beneficiary);

                custIndex++;
            }
            await context.SaveChangesAsync();

            // Seed Sample Transfers between Customer 1 and Customer 2
            var senderAcc = createdAccounts[0]; // Customer 1 Savings
            var receiverAcc = createdAccounts[2]; // Customer 2 Savings

            var transferRef = "TRF-20260815-000001";
            var transfer = new Transfer
            {
                TransferReference = transferRef,
                SenderAccountId = senderAcc.AccountId,
                ReceiverAccountId = receiverAcc.AccountId,
                Amount = 1200.00m,
                Description = "Consulting fee payment",
                TransferDate = DateTime.UtcNow.AddMonths(-1),
                Status = TransferStatus.Completed,
                InitiatedByUserId = createdCustomers[0].UserId
            };
            await context.Transfers.AddAsync(transfer);
            await context.SaveChangesAsync();

            var debitTxn = new Transaction
            {
                TransactionReference = "TXN-20260815-000001",
                AccountId = senderAcc.AccountId,
                TransactionType = TransactionType.TRANSFER_OUT,
                Amount = 1200.00m,
                BalanceAfterTransaction = senderAcc.Balance,
                Description = $"Transfer to {receiverAcc.AccountNumber} ({createdCustomers[1].FullName})",
                Status = TransactionStatus.Completed,
                TransactionDate = DateTime.UtcNow.AddMonths(-1),
                PerformedByUserId = createdCustomers[0].UserId,
                TransferId = transfer.TransferId
            };
            var creditTxn = new Transaction
            {
                TransactionReference = "TXN-20260815-000001-CR",
                AccountId = receiverAcc.AccountId,
                TransactionType = TransactionType.TRANSFER_IN,
                Amount = 1200.00m,
                BalanceAfterTransaction = receiverAcc.Balance,
                Description = $"Transfer from {senderAcc.AccountNumber} ({createdCustomers[0].FullName})",
                Status = TransactionStatus.Completed,
                TransactionDate = DateTime.UtcNow.AddMonths(-1),
                PerformedByUserId = createdCustomers[0].UserId,
                TransferId = transfer.TransferId
            };
            await context.Transactions.AddRangeAsync(debitTxn, creditTxn);

            // Seed Loans:
            // 1 Active Loan for Customer 1
            var loan1 = new Loan
            {
                LoanNumber = "LN-2026-0001",
                CustomerId = createdCustomers[0].CustomerId,
                AccountId = createdAccounts[0].AccountId,
                LoanType = LoanType.Personal,
                PrincipalAmount = 10000.00m,
                InterestRate = 8.50m,
                DurationMonths = 24,
                MonthlyPayment = 454.56m,
                TotalRepaymentAmount = 10909.44m,
                RemainingBalance = 9091.20m,
                Status = LoanStatus.Active,
                ApplicationDate = DateTime.UtcNow.AddMonths(-4),
                ApprovedDate = DateTime.UtcNow.AddMonths(-4).AddDays(1),
                ApprovedByUserId = adminUser.Id,
                Remarks = "Approved based on high credit score and steady salary deposit.",
                CreatedAt = DateTime.UtcNow.AddMonths(-4)
            };
            await context.Loans.AddAsync(loan1);
            await context.SaveChangesAsync();

            // Seed 4 payments for loan1
            for (int p = 1; p <= 4; p++)
            {
                var payment = new LoanPayment
                {
                    LoanId = loan1.LoanId,
                    Amount = 454.56m,
                    PrincipalComponent = 383.65m,
                    InterestComponent = 70.91m,
                    PaymentDate = DateTime.UtcNow.AddMonths(-4 + p),
                    RemainingBalance = 10909.44m - (454.56m * p),
                    TransactionReference = $"TXN-LOAN-00000{p}",
                    Notes = $"Monthly Installment #{p}"
                };
                await context.LoanPayments.AddAsync(payment);
            }

            // 1 Pending Loan for Customer 3
            var loan2 = new Loan
            {
                LoanNumber = "LN-2026-0002",
                CustomerId = createdCustomers[2].CustomerId,
                AccountId = createdAccounts[4].AccountId,
                LoanType = LoanType.Home,
                PrincipalAmount = 75000.00m,
                InterestRate = 6.25m,
                DurationMonths = 120,
                MonthlyPayment = 842.15m,
                TotalRepaymentAmount = 101058.00m,
                RemainingBalance = 101058.00m,
                Status = LoanStatus.Pending,
                ApplicationDate = DateTime.UtcNow.AddDays(-3),
                Remarks = "First-time home mortgage request for residential apartment.",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            };
            await context.Loans.AddAsync(loan2);

            // Seed Audit Logs
            var auditLogs = new List<AuditLog>
            {
                new()
                {
                    UserId = adminUser.Id,
                    Action = "System Initialization",
                    EntityName = "Database",
                    EntityId = "1",
                    Description = "System setup completed and baseline configuration seeded.",
                    IpAddress = "127.0.0.1",
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                },
                new()
                {
                    UserId = adminUser.Id,
                    Action = "Loan Approved",
                    EntityName = "Loan",
                    EntityId = loan1.LoanId.ToString(),
                    Description = "Approved Personal Loan LN-2026-0001 for customer Jonathan Reynolds",
                    IpAddress = "192.168.1.10",
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                }
            };
            await context.AuditLogs.AddRangeAsync(auditLogs);

            // Seed Initial Notifications
            var notifications = new List<Notification>
            {
                new()
                {
                    UserId = createdCustomers[0].UserId!,
                    Title = "Welcome to SecureBank!",
                    Message = "Your digital banking profile has been successfully set up. You can now manage accounts, transfer funds, and apply for credit.",
                    Type = "Success",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddMonths(-5)
                },
                new()
                {
                    UserId = createdCustomers[0].UserId!,
                    Title = "Loan Approved",
                    Message = "Your personal loan LN-2026-0001 has been approved and $10,000.00 was disbursed to your savings account.",
                    Type = "Success",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-4)
                }
            };
            await context.Notifications.AddRangeAsync(notifications);

            await context.SaveChangesAsync();
        }
    }
}
