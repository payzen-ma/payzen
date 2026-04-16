using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Payzen.Infrastructure.Persistence;

/// <summary>
/// Utilisé par `dotnet ef migrations add` et `dotnet ef database update`
/// sans démarrer l'API.
/// </summary>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Charge appsettings.json depuis Payzen.Api
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../Payzen.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            config.GetConnectionString("DefaultConnection"),
            sql => sql.MigrationsAssembly("Payzen.Infrastructure")
        );

        return new AppDbContext(optionsBuilder.Options);
    }
}
