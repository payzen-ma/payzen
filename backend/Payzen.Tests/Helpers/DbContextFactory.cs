using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Tests.Helpers;

/// <summary>
/// Crée un AppDbContext en mémoire pour les tests d'intégration.
/// Chaque test obtient une base isolée via un nom unique (Guid).
/// </summary>
public static class DbContextFactory
{
    public static AppDbContext CreateInMemory(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            // Désactive les warnings de transactions (non supportées en InMemory)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
