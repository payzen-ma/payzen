using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Payzen.Application.DTOs.Leave;
using Payzen.Domain.Entities.Company;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Entities.Leave;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;
using Payzen.Infrastructure.Services;
using Payzen.Infrastructure.Services.EventLog;
using Payzen.Infrastructure.Services.Leave;

namespace Payzen.Tests.Integration;

/// <summary>
/// Tests d'intégration de LeaveService — base EF InMemory.
/// Teste le workflow complet : Create → Submit → Approve/Reject.
/// </summary>
public class LeaveServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly LeaveService _svc;

    // ── Données de test partagées ─────────────────────────────────────────
    private const int CompanyId  = 1;
    private const int EmployeeId = 10;
    private const int UserId     = 99;

    public LeaveServiceTests()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db  = new AppDbContext(opts);
        _svc = new LeaveService(
            _db,
            new WorkingDaysCalculatorService(_db),
            new LeaveEventLogService(_db),
            new LeaveBalanceRecalculationService(_db));

        // Seed minimal requis par le service
        _db.Employees.Add(new Employee
        {
            Id        = EmployeeId,
            CompanyId = CompanyId,
            CinNumber = "AA123456",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-30)),
            Phone     = "0600000000",
            Email     = "employee@test.ma",
            FirstName = "Test",
            LastName  = "Employé",
            CreatedBy = UserId
        });
        _db.LeaveTypes.Add(new LeaveType
        {
            Id          = 1,
            CompanyId   = CompanyId,
            Scope       = LeaveScope.Company,
            LeaveCode   = "ANNUAL",
            LeaveNameFr = "Congé annuel",
            LeaveNameAr = "Congé annuel",
            LeaveNameEn = "Annual leave",
            LeaveDescription = "Congé annuel test",
            IsActive    = true,
            CreatedBy   = UserId
        });
        _db.LeaveTypePolicies.Add(new LeaveTypePolicy
        {
            Id = 1,
            CompanyId = CompanyId,
            LeaveTypeId = 1,
            IsEnabled = true,
            RequiresBalance = false,
            AllowNegativeBalance = true,
            AccrualMethod = LeaveAccrualMethod.Monthly,
            CreatedBy = UserId
        });
        _db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateLeaveRequest_ValidData_ReturnsOk()
    {
        var dto = new LeaveRequestCreateDto
        {
            LeaveTypeId  = 1,
            StartDate    = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            EndDate      = DateOnly.FromDateTime(DateTime.Today.AddDays(11)),
            EmployeeNote = "Vacances été"
        };

        var result = await _svc.CreateLeaveRequestAsync(EmployeeId, dto, UserId);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be(LeaveRequestStatus.Draft);
        result.Data.EmployeeId.Should().Be(EmployeeId);
    }

    [Fact]
    public async Task CreateLeaveRequest_EmployeeNotFound_ReturnsFail()
    {
        var dto = new LeaveRequestCreateDto
        {
            LeaveTypeId = 1,
            StartDate   = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(11))
        };

        var result = await _svc.CreateLeaveRequestAsync(9999, dto, UserId); // ID inexistant

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("introuvable");
    }

    [Fact]
    public async Task SubmitLeaveRequest_FromDraft_StatusBecomesSubmitted()
    {
        // Créer une demande en Draft
        var createDto = new LeaveRequestCreateDto
        {
            LeaveTypeId = 1,
            StartDate   = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(9))
        };
        var created = await _svc.CreateLeaveRequestAsync(EmployeeId, createDto, UserId);
        var id      = created.Data!.Id;

        // Soumettre
        var result = await _svc.SubmitLeaveRequestAsync(id, UserId);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(LeaveRequestStatus.Submitted);
        result.Data.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveLeaveRequest_AfterSubmit_StatusApproved()
    {
        var createDto = new LeaveRequestCreateDto
        {
            LeaveTypeId = 1,
            StartDate   = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(9))
        };
        var created = await _svc.CreateLeaveRequestAsync(EmployeeId, createDto, UserId);
        var id      = created.Data!.Id;
        await _svc.SubmitLeaveRequestAsync(id, UserId);

        var result = await _svc.ApproveLeaveRequestAsync(id, "Approuvé !", UserId);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(LeaveRequestStatus.Approved);
        result.Data.DecisionComment.Should().Be("Approuvé !");
    }

    [Fact]
    public async Task RejectLeaveRequest_AfterSubmit_StatusRejected()
    {
        var createDto = new LeaveRequestCreateDto
        {
            LeaveTypeId = 1,
            StartDate   = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(9))
        };
        var created = await _svc.CreateLeaveRequestAsync(EmployeeId, createDto, UserId);
        var id      = created.Data!.Id;
        await _svc.SubmitLeaveRequestAsync(id, UserId);

        var result = await _svc.RejectLeaveRequestAsync(id, "Période chargée", UserId);

        result.Success.Should().BeTrue();
        result.Data!.Status.Should().Be(LeaveRequestStatus.Rejected);
    }

    [Fact]
    public async Task DeleteLeaveRequest_SoftDelete_DisparaîtDesRequêtes()
    {
        var createDto = new LeaveRequestCreateDto
        {
            LeaveTypeId = 1,
            StartDate   = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(9))
        };
        var created = await _svc.CreateLeaveRequestAsync(EmployeeId, createDto, UserId);
        var id      = created.Data!.Id;

        await _svc.DeleteLeaveRequestAsync(id, UserId);

        // Le global filter ExcludeDeletedAt doit masquer la demande
        var listResult = await _svc.GetLeaveRequestsAsync(CompanyId, EmployeeId, null);
        listResult.Data.Should().NotContain(r => r.Id == id);
    }

    [Fact]
    public async Task GetLeaveRequests_PourEmployé_RetourneSesDemandes()
    {
        // Créer 2 demandes pour l'employé
        for (int i = 0; i < 2; i++)
        {
            var dto = new LeaveRequestCreateDto
            {
                LeaveTypeId = 1,
                StartDate   = DateOnly.FromDateTime(DateTime.Today.AddDays(7 + i * 14)),
                EndDate     = DateOnly.FromDateTime(DateTime.Today.AddDays(9 + i * 14))
            };
            await _svc.CreateLeaveRequestAsync(EmployeeId, dto, UserId);
        }

        var result = await _svc.GetLeaveRequestsAsync(null, EmployeeId, null);

        result.Success.Should().BeTrue();
        result.Data!.Count().Should().Be(2);
        result.Data.All(r => r.EmployeeId == EmployeeId).Should().BeTrue();
    }
}
