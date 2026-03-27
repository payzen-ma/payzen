using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Company;
using Payzen.Domain.Entities.Referentiel;
using Payzen.Infrastructure.Persistence;
using Payzen.Infrastructure.Services.Employee;

namespace Payzen.Tests.Integration;

/// <summary>
/// Tests d'intégration EmployeeService — CRUD de base avec EF InMemory.
/// </summary>
public class EmployeeServiceTests : IDisposable
{
    private readonly AppDbContext  _db;
    private readonly EmployeeService _svc;
    private const int CompanyId = 1;
    private const int UserId    = 99;

    public EmployeeServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db  = new AppDbContext(opts);
        var env = new Mock<IWebHostEnvironment>().Object;
        var eventLog = new Mock<IEmployeeEventLogService>().Object;
        var invitationService = new Mock<IInvitationService>().Object;
        var leaveRecalc = new Mock<ILeaveBalanceRecalculationService>().Object;
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<EmployeeService>>().Object;
        _svc = new EmployeeService(_db, env, eventLog, invitationService, leaveRecalc, logger);

        // Données référentielles minimales
        _db.Companies.Add(new Company
        {
            Id          = CompanyId,
            CompanyName = "Société Test SARL",
            Email = "societe@test.ma",
            PhoneNumber = "0600000000",
            CompanyAddress = "Adresse Test",
            CityId = 1,
            CountryId = 1,
            CreatedBy   = UserId
        });
        _db.Genders.Add(new Gender
        {
            Id = 1,
            Code = "M",
            NameFr = "Homme",
            NameAr = "HommeAR",
            NameEn = "Man",
            IsActive = true,
            CreatedBy = 0
        });
        _db.Statuses.Add(new Status
        {
            Id = 1,
            Code = "Active",
            NameFr = "Actif",
            NameAr = "ActifAR",
            NameEn = "Active",
            IsActive = true,
            CreatedBy = 0
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task CreateEmployee_DonnéesValides_Succès()
    {
        var dto = new EmployeeCreateDto
        {
            FirstName   = "Youssef",
            LastName    = "Alami",
            Email       = "youssef@test.ma",
            CinNumber   = "AA123456",
            Phone       = "0600000001",
            CompanyId   = CompanyId,
            GenderId    = 1,
            StatusId    = 1,
            DateOfBirth = new DateOnly(1990, 5, 15)
        };

        var result = await _svc.CreateAsync(dto, UserId);

        result.Success.Should().BeTrue();
        result.Data!.FirstName.Should().Be("Youssef");
        result.Data.CompanyId.Should().Be(CompanyId);
    }

    [Fact]
    public async Task GetById_EmployéExistant_RetourneEmployé()
    {
        var dto = new EmployeeCreateDto
        {
            FirstName   = "Fatima",
            LastName    = "Benali",
            Email       = "fatima@test.ma",
            CinNumber   = "AB987654",
            Phone       = "0600000002",
            CompanyId   = CompanyId,
            GenderId    = 1,
            StatusId    = 1,
            DateOfBirth = new DateOnly(1988, 3, 20)
        };
        var created = await _svc.CreateAsync(dto, UserId);
        var id = created.Data!.Id;

        var result = await _svc.GetByIdAsync(id);

        result.Success.Should().BeTrue();
        result.Data!.Email.Should().Be("fatima@test.ma");
    }

    [Fact]
    public async Task GetById_IDInexistant_Echoue()
    {
        var result = await _svc.GetByIdAsync(9999);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("introuvable");
    }

    [Fact]
    public async Task DeleteEmployee_SoftDelete_DispAraîtDesListes()
    {
        var dto = new EmployeeCreateDto
        {
            FirstName   = "Hamid",
            LastName    = "Tazi",
            Email       = "hamid@test.ma",
            CinNumber   = "AC111222",
            Phone       = "0600000003",
            CompanyId   = CompanyId,
            GenderId    = 1,
            StatusId    = 1,
            DateOfBirth = new DateOnly(1985, 7, 10)
        };
        var created = await _svc.CreateAsync(dto, UserId);
        var id      = created.Data!.Id;

        await _svc.DeleteAsync(id, UserId);

        // Vérifier que l'employé n'apparaît plus dans GetAll
        var list = await _svc.GetAllAsync(CompanyId);
        list.Data!.Employees.Should().NotContain(e => e.Id == id.ToString());
    }

    [Fact]
    public async Task GetAll_ParCompany_RetourneSeulementSesEmployés()
    {
        // Créer 2 employés pour CompanyId=1
        for (int i = 0; i < 2; i++)
        {
            await _svc.CreateAsync(new EmployeeCreateDto
            {
                FirstName   = $"Prénom{i}",
                LastName    = $"Nom{i}",
                Email       = $"test{i}@test.ma",
                CinNumber   = $"CIN{i + 1:000}",
                Phone       = $"06000000{i + 10}",
                CompanyId   = CompanyId,
                GenderId    = 1,
                StatusId    = 1,
                DateOfBirth = new DateOnly(1990, 1, 1)
            }, UserId);
        }

        var result = await _svc.GetAllAsync(CompanyId);

        result.Success.Should().BeTrue();
        result.Data!.TotalEmployees.Should().Be(2);
        result.Data!.Employees.Should().HaveCount(2);
    }
}
