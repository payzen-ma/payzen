using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Payzen.Application.DTOs.Auth;
using Payzen.Domain.Entities.Auth;
using Payzen.Infrastructure.Persistence;
using Payzen.Infrastructure.Services.Auth;

namespace Payzen.Tests.Integration;

/// <summary>
/// Tests d'intégration de AuthService — Entra login, CreateUser.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly AuthService _svc;
    private const int AdminId = 1;

    public AuthServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new AppDbContext(opts);

        // Configuration minimale pour JwtService
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "test_secret_key_must_be_at_least_32_chars_long",
                ["JwtSettings:Issuer"] = "PayzenTest",
                ["JwtSettings:Audience"] = "PayzenClient",
                ["JwtSettings:ExpiresInMinutes"] = "60"
            })
            .Build();

        var jwtService = new JwtService(config, _db);
        _svc = new AuthService(_db, jwtService);

        // Seed minimal RBAC requis par LoginWithEntraAsync (rôles de fallback)
        _db.Roles.AddRange(
            new Roles { Id = 1, Name = "Employee", Description = "Employee", CreatedBy = 0 },
            new Roles { Id = 2, Name = "Visitor", Description = "Visitor", CreatedBy = 0 }
        );

        // Seed un user admin de test (sans mot de passe, auth via Entra)
        _db.Users.Add(new Users
        {
            Id = AdminId,
            Email = "admin@test.ma",
            Username = "admin",
            IsActive = true,
            CreatedBy = 0
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task EntraLogin_EmailEtOid_RetourneToken()
    {
        var dto = new EntraLoginRequestDto { Email = "admin@test.ma", ExternalId = "oid-test-123" };

        var result = await _svc.LoginWithEntraAsync(dto);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EntraLogin_EmailInvalide_EchoueValidation()
    {
        var dto = new EntraLoginRequestDto { Email = "not-an-email", ExternalId = "oid-test-123" };

        var result = await _svc.LoginWithEntraAsync(dto);

        result.Success.Should().BeFalse();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task CreateUser_NouvelEmail_Succès()
    {
        var dto = new UserCreateDto
        {
            Email = "nouveau@test.ma",
            Username = "nouveau",
            IsActive = true
        };

        var result = await _svc.CreateUserAsync(dto, AdminId);

        result.Success.Should().BeTrue();
        result.Data!.Email.Should().Be("nouveau@test.ma");
    }

    [Fact]
    public async Task CreateUser_EmailDéjàExistant_Echoue()
    {
        var dto = new UserCreateDto
        {
            Email = "admin@test.ma", // déjà dans la DB
            Username = "admin2",
            IsActive = true
        };

        var result = await _svc.CreateUserAsync(dto, AdminId);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
