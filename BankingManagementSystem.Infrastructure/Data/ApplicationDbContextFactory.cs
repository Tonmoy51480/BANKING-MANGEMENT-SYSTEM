using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BankingManagementSystem.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=BankingManagementDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
