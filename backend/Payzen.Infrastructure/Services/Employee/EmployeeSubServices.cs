using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Employee;

// ════════════════════════════════════════════════════════════
// CONTRATS
// ════════════════════════════════════════════════════════════

public class EmployeeContractService : IEmployeeContractService
{
    private readonly AppDbContext _db;
    private readonly IEmployeeEventLogService _eventLog;

    public EmployeeContractService(AppDbContext db, IEmployeeEventLogService eventLog)
    {
        _db = db;
        _eventLog = eventLog;
    }

    public async Task<ServiceResult<IEnumerable<EmployeeContractReadDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _db
            .EmployeeContracts.AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .Include(c => c.Employee)
            .Include(c => c.Company)
            .Include(c => c.JobPosition)
            .Include(c => c.ContractType)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeContractReadDto>>.Ok(list.Select(MapContract));
    }

    public async Task<ServiceResult<IEnumerable<EmployeeContractReadDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    )
    {
        var list = await _db
            .EmployeeContracts.Where(c => c.EmployeeId == employeeId && c.DeletedAt == null)
            .Include(c => c.Employee)
            .Include(c => c.Company)
            .Include(c => c.JobPosition)
            .Include(c => c.ContractType)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeContractReadDto>>.Ok(list.Select(MapContract));
    }

    public async Task<ServiceResult<EmployeeContractReadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _db
            .EmployeeContracts.Include(c => c.Employee)
            .Include(c => c.Company)
            .Include(c => c.JobPosition)
            .Include(c => c.ContractType)
            .FirstOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, ct);
        return c == null
            ? ServiceResult<EmployeeContractReadDto>.Fail("Contrat introuvable.")
            : ServiceResult<EmployeeContractReadDto>.Ok(MapContract(c));
    }

    public async Task<ServiceResult<EmployeeContractReadDto>> CreateAsync(
        EmployeeContractCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var c = new EmployeeContract
        {
            EmployeeId = dto.EmployeeId,
            CompanyId = dto.CompanyId,
            JobPositionId = dto.JobPositionId,
            ContractTypeId = dto.ContractTypeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            CreatedBy = createdBy,
        };
        _db.EmployeeContracts.Add(c);
        await _db.SaveChangesAsync(ct);
        var created = await _db
            .EmployeeContracts.Include(x => x.Employee)
            .Include(x => x.Company)
            .Include(x => x.JobPosition)
            .Include(x => x.ContractType)
            .FirstAsync(x => x.Id == c.Id, ct);
        var contractInfo =
            $"{created.ContractType?.ContractTypeName} — {created.JobPosition?.Name} (dès {created.StartDate:dd/MM/yyyy})";
        await _eventLog.LogSimpleEventAsync(
            dto.EmployeeId,
            EmployeeEventLogNames.ContractCreated,
            null,
            contractInfo,
            createdBy,
            ct
        );
        return ServiceResult<EmployeeContractReadDto>.Ok(MapContract(created));
    }

    public async Task<ServiceResult<EmployeeContractReadDto>> UpdateAsync(
        int id,
        EmployeeContractUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var c = await _db
            .EmployeeContracts.Include(x => x.JobPosition)
            .Include(x => x.ContractType)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c == null)
            return ServiceResult<EmployeeContractReadDto>.Fail("Contrat introuvable.");

        var pendingContractLogs = new List<Func<Task>>();

        if (dto.JobPositionId is > 0 && dto.JobPositionId != c.JobPositionId)
        {
            var oldName = c.JobPosition?.Name;
            var newName = await _db
                .JobPositions.AsNoTracking()
                .Where(j => j.Id == dto.JobPositionId.Value)
                .Select(j => j.Name)
                .FirstOrDefaultAsync(ct);
            pendingContractLogs.Add(() =>
                _eventLog.LogSimpleEventAsync(
                    c.EmployeeId,
                    EmployeeEventLogNames.JobPositionChanged,
                    oldName,
                    newName,
                    updatedBy,
                    ct
                )
            );
            c.JobPositionId = dto.JobPositionId.Value;
        }
        if (dto.ContractTypeId is > 0 && dto.ContractTypeId != c.ContractTypeId)
        {
            var oldType = c.ContractType?.ContractTypeName;
            var newType = await _db
                .ContractTypes.AsNoTracking()
                .Where(ct2 => ct2.Id == dto.ContractTypeId.Value)
                .Select(ct2 => ct2.ContractTypeName)
                .FirstOrDefaultAsync(ct);
            pendingContractLogs.Add(() =>
                _eventLog.LogSimpleEventAsync(
                    c.EmployeeId,
                    EmployeeEventLogNames.ContractTypeChanged,
                    oldType,
                    newType,
                    updatedBy,
                    ct
                )
            );
            c.ContractTypeId = dto.ContractTypeId.Value;
        }
        if (dto.StartDate.HasValue)
            c.StartDate = dto.StartDate.Value;
        if (dto.EndDate != null)
        {
            if (dto.EndDate.HasValue)
            {
                var endInfo = dto.EndDate.Value.ToString("dd/MM/yyyy");
                var startStr = c.StartDate.ToString("dd/MM/yyyy");
                pendingContractLogs.Add(() =>
                    _eventLog.LogSimpleEventAsync(
                        c.EmployeeId,
                        EmployeeEventLogNames.ContractTerminated,
                        startStr,
                        endInfo,
                        updatedBy,
                        ct
                    )
                );
            }
            c.EndDate = dto.EndDate;
        }

        c.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        foreach (var runLog in pendingContractLogs)
            await runLog();
        var updated = await _db
            .EmployeeContracts.Include(x => x.Employee)
            .Include(x => x.Company)
            .Include(x => x.JobPosition)
            .Include(x => x.ContractType)
            .FirstAsync(x => x.Id == id, ct);
        return ServiceResult<EmployeeContractReadDto>.Ok(MapContract(updated));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var c = await _db.EmployeeContracts.FindAsync(new object[] { id }, ct);
        if (c == null)
            return ServiceResult.Fail("Contrat introuvable.");
        c.DeletedAt = DateTimeOffset.UtcNow;
        c.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private static EmployeeContractReadDto MapContract(EmployeeContract c) =>
        new()
        {
            Id = c.Id,
            EmployeeId = c.EmployeeId,
            EmployeeFullName = c.Employee != null ? $"{c.Employee.FirstName} {c.Employee.LastName}" : string.Empty,
            CompanyId = c.CompanyId,
            CompanyName = c.Company?.CompanyName ?? string.Empty,
            JobPositionId = c.JobPositionId,
            JobPositionName = c.JobPosition?.Name ?? string.Empty,
            ContractTypeId = c.ContractTypeId,
            ContractTypeName = c.ContractType?.ContractTypeName ?? string.Empty,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            CreatedAt = c.CreatedAt.DateTime,
        };
}

// ════════════════════════════════════════════════════════════
// SALAIRES
// ════════════════════════════════════════════════════════════

public class EmployeeSalaryService : IEmployeeSalaryService
{
    private readonly AppDbContext _db;
    private readonly IEmployeeEventLogService _eventLog;

    public EmployeeSalaryService(AppDbContext db, IEmployeeEventLogService eventLog)
    {
        _db = db;
        _eventLog = eventLog;
    }

    public async Task<ServiceResult<IEnumerable<EmployeeSalaryReadDto>>> GetAllSalariesAsync(
        CancellationToken ct = default
    )
    {
        var list = await _db
            .EmployeeSalaries.AsNoTracking()
            .Where(s => s.DeletedAt == null)
            .Include(s => s.Employee)
            .OrderByDescending(s => s.EffectiveDate)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeSalaryReadDto>>.Ok(list.Select(MapSalary));
    }

    public async Task<ServiceResult<IEnumerable<EmployeeSalaryReadDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    )
    {
        var list = await _db
            .EmployeeSalaries.Where(s => s.EmployeeId == employeeId && s.DeletedAt == null)
            .Include(s => s.Employee)
            .Include(s => s.Components)
            .OrderByDescending(s => s.EffectiveDate)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeSalaryReadDto>>.Ok(list.Select(MapSalary));
    }

    public async Task<ServiceResult<EmployeeSalaryReadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var s = await _db
            .EmployeeSalaries.Include(x => x.Components)
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        return s == null
            ? ServiceResult<EmployeeSalaryReadDto>.Fail("Salaire introuvable.")
            : ServiceResult<EmployeeSalaryReadDto>.Ok(MapSalary(s));
    }

    public async Task<ServiceResult<IEnumerable<EmployeeSalaryReadDto>>> GetByContractAsync(
        int contractId,
        CancellationToken ct = default
    )
    {
        var contractExists = await _db.EmployeeContracts.AnyAsync(c => c.Id == contractId && c.DeletedAt == null, ct);
        if (!contractExists)
            return ServiceResult<IEnumerable<EmployeeSalaryReadDto>>.Fail("Contrat introuvable.");

        var list = await _db
            .EmployeeSalaries.Where(s => s.ContractId == contractId && s.DeletedAt == null)
            .Include(s => s.Employee)
            .Include(s => s.Components)
            .OrderByDescending(s => s.EffectiveDate)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeSalaryReadDto>>.Ok(list.Select(MapSalary));
    }

    public async Task<ServiceResult<EmployeeSalaryReadDto>> CreateAsync(
        EmployeeSalaryCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null, ct);
        if (!employeeExists)
            return ServiceResult<EmployeeSalaryReadDto>.Fail("Employé introuvable.");

        var contract = await _db.EmployeeContracts.FirstOrDefaultAsync(
            c => c.Id == dto.ContractId && c.DeletedAt == null,
            ct
        );
        if (contract == null)
            return ServiceResult<EmployeeSalaryReadDto>.Fail("Contrat introuvable.");

        if (contract.EmployeeId != dto.EmployeeId)
            return ServiceResult<EmployeeSalaryReadDto>.Fail("Le contrat ne correspond pas à l'employé spécifié.");

        if (dto.EndDate.HasValue && dto.EndDate.Value < dto.EffectiveDate)
            return ServiceResult<EmployeeSalaryReadDto>.Fail("La date de fin doit être après la date d'effet.");

        var s = new EmployeeSalary
        {
            EmployeeId = dto.EmployeeId,
            ContractId = dto.ContractId,
            BaseSalary = dto.BaseSalary,
            BaseSalaryHourly = dto.BaseSalaryHourly,
            EffectiveDate = dto.EffectiveDate,
            EndDate = dto.EndDate,
            CreatedBy = createdBy,
        };
        _db.EmployeeSalaries.Add(s);
        await _db.SaveChangesAsync(ct);
        await _eventLog.LogSimpleEventAsync(
            dto.EmployeeId,
            EmployeeEventLogNames.SalaryUpdated,
            null,
            dto.BaseSalary?.ToString("F2"),
            createdBy,
            ct
        );
        var created = await _db.EmployeeSalaries.Include(x => x.Employee).FirstAsync(x => x.Id == s.Id, ct);
        return ServiceResult<EmployeeSalaryReadDto>.Ok(MapSalary(created));
    }

    public async Task<ServiceResult<EmployeeSalaryReadDto>> UpdateAsync(
        int id,
        EmployeeSalaryUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var s = await _db.EmployeeSalaries.FindAsync(new object[] { id }, ct);
        if (s == null || s.DeletedAt != null)
            return ServiceResult<EmployeeSalaryReadDto>.Fail("Salaire introuvable.");

        var nextEffectiveDate = dto.EffectiveDate ?? s.EffectiveDate;
        if (dto.EndDate.HasValue && dto.EndDate.Value < nextEffectiveDate)
            return ServiceResult<EmployeeSalaryReadDto>.Fail("La date de fin doit être après la date d'effet.");

        if (dto.BaseSalary != null && dto.BaseSalary != s.BaseSalary)
        {
            await _eventLog.LogSimpleEventAsync(
                s.EmployeeId,
                EmployeeEventLogNames.SalaryUpdated,
                s.BaseSalary?.ToString("F2"),
                dto.BaseSalary.Value.ToString("F2"),
                updatedBy,
                ct
            );
            s.BaseSalary = dto.BaseSalary;
        }
        if (dto.BaseSalaryHourly != null)
            s.BaseSalaryHourly = dto.BaseSalaryHourly;
        if (dto.EffectiveDate != null)
            s.EffectiveDate = dto.EffectiveDate.Value;
        if (dto.EndDate != null)
            s.EndDate = dto.EndDate;
        s.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        var updated = await _db.EmployeeSalaries.Include(x => x.Employee).FirstAsync(x => x.Id == id, ct);
        return ServiceResult<EmployeeSalaryReadDto>.Ok(MapSalary(updated));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var s = await _db.EmployeeSalaries.FindAsync(new object[] { id }, ct);
        if (s == null || s.DeletedAt != null)
            return ServiceResult.Fail("Salaire introuvable.");

        var hasComponents = await _db.EmployeeSalaryComponents.AnyAsync(
            c => c.EmployeeSalaryId == id && c.DeletedAt == null,
            ct
        );
        if (hasComponents)
            return ServiceResult.Fail("Impossible de supprimer ce salaire car il contient des composants.");

        s.DeletedAt = DateTimeOffset.UtcNow;
        s.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>> GetAllSalaryComponentsAsync(
        CancellationToken ct = default
    )
    {
        var list = await _db
            .EmployeeSalaryComponents.AsNoTracking()
            .Where(c => c.DeletedAt == null)
            .OrderByDescending(c => c.EffectiveDate)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>.Ok(list.Select(MapComp));
    }

    public async Task<ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>> GetComponentsAsync(
        int salaryId,
        CancellationToken ct = default
    )
    {
        var salaryExists = await _db.EmployeeSalaries.AnyAsync(s => s.Id == salaryId && s.DeletedAt == null, ct);
        if (!salaryExists)
            return ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>.Fail("Salaire introuvable.");

        var list = await _db
            .EmployeeSalaryComponents.Where(c => c.EmployeeSalaryId == salaryId && c.DeletedAt == null)
            .Select(c => MapComp(c))
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<EmployeeSalaryComponentReadDto>> GetComponentByIdAsync(
        int id,
        CancellationToken ct = default
    )
    {
        var c = await _db.EmployeeSalaryComponents.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        return c == null
            ? ServiceResult<EmployeeSalaryComponentReadDto>.Fail("Composante introuvable.")
            : ServiceResult<EmployeeSalaryComponentReadDto>.Ok(MapComp(c));
    }

    public async Task<ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>> GetComponentsByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    )
    {
        var salaryId = await _db
            .EmployeeSalaries.Where(s => s.EmployeeId == employeeId && s.DeletedAt == null)
            .OrderByDescending(s => s.EffectiveDate)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);
        if (!salaryId.HasValue)
            return ServiceResult<IEnumerable<EmployeeSalaryComponentReadDto>>.Ok(
                Array.Empty<EmployeeSalaryComponentReadDto>()
            );
        return await GetComponentsAsync(salaryId.Value, ct);
    }

    public async Task<ServiceResult<EmployeeSalaryComponentReadDto>> CreateComponentAsync(
        EmployeeSalaryComponentCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var salaryExists = await _db.EmployeeSalaries.AnyAsync(
            s => s.Id == dto.EmployeeSalaryId && s.DeletedAt == null,
            ct
        );
        if (!salaryExists)
            return ServiceResult<EmployeeSalaryComponentReadDto>.Fail("Salaire introuvable.");

        if (dto.EndDate.HasValue && dto.EndDate.Value < dto.EffectiveDate)
            return ServiceResult<EmployeeSalaryComponentReadDto>.Fail(
                "La date de fin doit être après la date d'effet."
            );

        var c = new EmployeeSalaryComponent
        {
            EmployeeSalaryId = dto.EmployeeSalaryId,
            ComponentType = dto.ComponentType,
            IsTaxable = dto.IsTaxable,
            IsSocial = true,
            IsCIMR = false,
            Amount = dto.Amount,
            EffectiveDate = dto.EffectiveDate,
            EndDate = dto.EndDate,
            CreatedBy = createdBy,
        };
        _db.EmployeeSalaryComponents.Add(c);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeSalaryComponentReadDto>.Ok(MapComp(c));
    }

    public async Task<ServiceResult<EmployeeSalaryComponentReadDto>> UpdateComponentAsync(
        int id,
        EmployeeSalaryComponentUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var c = await _db.EmployeeSalaryComponents.FindAsync(new object[] { id }, ct);
        if (c == null || c.DeletedAt != null)
            return ServiceResult<EmployeeSalaryComponentReadDto>.Fail("Composante introuvable.");
        if (dto.EffectiveDate.HasValue && dto.EffectiveDate.Value > c.EffectiveDate)
        {
            c.EndDate = dto.EffectiveDate.Value.AddDays(-1);
            c.UpdatedBy = updatedBy;

            var nextVersion = new EmployeeSalaryComponent
            {
                EmployeeSalaryId = c.EmployeeSalaryId,
                ComponentType = dto.ComponentType ?? c.ComponentType,
                IsTaxable = dto.IsTaxable ?? c.IsTaxable,
                IsSocial = c.IsSocial,
                IsCIMR = c.IsCIMR,
                Amount = dto.Amount ?? c.Amount,
                EffectiveDate = dto.EffectiveDate.Value,
                EndDate = dto.EndDate,
                CreatedBy = updatedBy,
            };

            _db.EmployeeSalaryComponents.Add(nextVersion);
            await _db.SaveChangesAsync(ct);
            return ServiceResult<EmployeeSalaryComponentReadDto>.Ok(MapComp(nextVersion));
        }

        if (dto.ComponentType != null)
            c.ComponentType = dto.ComponentType;
        if (dto.Amount != null)
            c.Amount = dto.Amount.Value;
        if (dto.IsTaxable.HasValue)
            c.IsTaxable = dto.IsTaxable.Value;
        if (dto.EffectiveDate.HasValue)
            c.EffectiveDate = dto.EffectiveDate.Value;
        if (dto.EndDate.HasValue)
        {
            if (dto.EndDate.Value < c.EffectiveDate)
                return ServiceResult<EmployeeSalaryComponentReadDto>.Fail(
                    "La date de fin doit être après la date d'effet."
                );
            c.EndDate = dto.EndDate;
        }
        c.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeSalaryComponentReadDto>.Ok(MapComp(c));
    }

    public async Task<ServiceResult> DeleteComponentAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var c = await _db.EmployeeSalaryComponents.FindAsync(new object[] { id }, ct);
        if (c == null || c.DeletedAt != null)
            return ServiceResult.Fail("Composante introuvable.");
        c.DeletedAt = DateTimeOffset.UtcNow;
        c.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<object>> ReviseSalaryComponentAsync(
        int id,
        EmployeeSalaryComponentUpdateDto dto,
        int userId,
        CancellationToken ct = default
    )
    {
        var old = await _db.EmployeeSalaryComponents.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (old == null)
            return ServiceResult<object>.Fail("Composante introuvable.");
        var newEff = dto.EffectiveDate ?? DateTime.UtcNow;
        if (newEff <= old.EffectiveDate)
            return ServiceResult<object>.Fail("La nouvelle date effective doit être supérieure à l'ancienne.");
        old.EndDate = dto.EffectiveDate ?? DateTime.UtcNow;
        old.DeletedAt = DateTimeOffset.UtcNow;
        old.DeletedBy = userId;
        var neu = new EmployeeSalaryComponent
        {
            EmployeeSalaryId = old.EmployeeSalaryId,
            ComponentType = dto.ComponentType ?? old.ComponentType,
            Amount = dto.Amount ?? old.Amount,
            IsTaxable = dto.IsTaxable ?? old.IsTaxable,
            IsSocial = old.IsSocial,
            IsCIMR = old.IsCIMR,
            EffectiveDate = newEff,
            EndDate = dto.EndDate,
            CreatedBy = userId,
        };
        _db.EmployeeSalaryComponents.Add(neu);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<object>.Ok(
            new
            {
                Message = "Composant révisé avec succès",
                OldVersion = new { old.Id, old.EndDate },
                NewVersion = new
                {
                    neu.Id,
                    neu.Amount,
                    neu.EffectiveDate,
                    neu.EndDate,
                },
            }
        );
    }

    private static EmployeeSalaryReadDto MapSalary(EmployeeSalary s) =>
        new()
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            EmployeeFullName = s.Employee != null ? $"{s.Employee.FirstName} {s.Employee.LastName}" : string.Empty,
            ContractId = s.ContractId,
            BaseSalary = s.BaseSalary,
            BaseSalaryHourly = s.BaseSalaryHourly,
            EffectiveDate = s.EffectiveDate,
            EndDate = s.EndDate,
            CreatedAt = s.CreatedAt.DateTime,
        };

    private static EmployeeSalaryComponentReadDto MapComp(EmployeeSalaryComponent c) =>
        new()
        {
            Id = c.Id,
            EmployeeSalaryId = c.EmployeeSalaryId,
            ComponentType = c.ComponentType,
            IsTaxable = c.IsTaxable,
            Amount = c.Amount,
            EffectiveDate = c.EffectiveDate,
            EndDate = c.EndDate,
            CreatedAt = c.CreatedAt.DateTime,
        };
}

// ════════════════════════════════════════════════════════════
// DOCUMENTS
// ════════════════════════════════════════════════════════════

public class EmployeeDocumentService : IEmployeeDocumentService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public EmployeeDocumentService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<ServiceResult<IEnumerable<EmployeeDocumentReadDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _db
            .EmployeeDocuments.AsNoTracking()
            .Where(d => d.DeletedAt == null)
            .Include(d => d.Employee)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeDocumentReadDto>>.Ok(entities.Select(Map));
    }

    public async Task<ServiceResult<EmployeeDocumentReadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var d = await _db
            .EmployeeDocuments.Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        return d == null
            ? ServiceResult<EmployeeDocumentReadDto>.Fail("Document introuvable.")
            : ServiceResult<EmployeeDocumentReadDto>.Ok(Map(d));
    }

    public async Task<ServiceResult<IEnumerable<EmployeeDocumentReadDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    )
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == employeeId && e.DeletedAt == null, ct);
        if (!employeeExists)
            return ServiceResult<IEnumerable<EmployeeDocumentReadDto>>.Fail("Employé introuvable.");

        var list = await _db
            .EmployeeDocuments.Where(d => d.EmployeeId == employeeId && d.DeletedAt == null)
            .Include(d => d.Employee)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => Map(d))
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeDocumentReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<EmployeeDocumentReadDto>> CreateAsync(
        EmployeeDocumentCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null, ct);
        if (!employeeExists)
            return ServiceResult<EmployeeDocumentReadDto>.Fail("Employé introuvable.");

        var d = new EmployeeDocument
        {
            EmployeeId = dto.EmployeeId,
            Name = dto.Name,
            FilePath = dto.FilePath,
            DocumentType = dto.DocumentType,
            ExpirationDate = dto.ExpirationDate,
            CreatedBy = createdBy,
        };
        _db.EmployeeDocuments.Add(d);
        await _db.SaveChangesAsync(ct);
        var created = await _db.EmployeeDocuments.Include(x => x.Employee).FirstAsync(x => x.Id == d.Id, ct);
        return ServiceResult<EmployeeDocumentReadDto>.Ok(Map(created));
    }

    public async Task<ServiceResult<EmployeeDocumentReadDto>> UpdateAsync(
        int id,
        EmployeeDocumentUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var d = await _db.EmployeeDocuments.FindAsync(new object[] { id }, ct);
        if (d == null || d.DeletedAt != null)
            return ServiceResult<EmployeeDocumentReadDto>.Fail("Document introuvable.");
        if (dto.Name != null)
            d.Name = dto.Name;
        if (dto.FilePath != null)
            d.FilePath = dto.FilePath;
        if (dto.ExpirationDate.HasValue)
            d.ExpirationDate = dto.ExpirationDate;
        if (dto.DocumentType != null)
            d.DocumentType = dto.DocumentType;
        d.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        var updated = await _db.EmployeeDocuments.Include(x => x.Employee).FirstAsync(x => x.Id == id, ct);
        return ServiceResult<EmployeeDocumentReadDto>.Ok(Map(updated));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var d = await _db.EmployeeDocuments.FindAsync(new object[] { id }, ct);
        if (d == null || d.DeletedAt != null)
            return ServiceResult.Fail("Document introuvable.");
        d.DeletedAt = DateTimeOffset.UtcNow;
        d.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private static EmployeeDocumentReadDto Map(EmployeeDocument d) =>
        new()
        {
            Id = d.Id,
            EmployeeId = d.EmployeeId,
            EmployeeFullName =
                d.Employee != null ? $"{d.Employee.FirstName} {d.Employee.LastName}".Trim() : string.Empty,
            Name = d.Name,
            FilePath = d.FilePath,
            DocumentType = d.DocumentType,
            ExpirationDate = d.ExpirationDate,
            CreatedAt = d.CreatedAt.DateTime,
        };
}

// ════════════════════════════════════════════════════════════
// ADRESSES
// ════════════════════════════════════════════════════════════

public class EmployeeAddressService : IEmployeeAddressService
{
    private readonly AppDbContext _db;
    private readonly IEmployeeEventLogService _eventLog;

    public EmployeeAddressService(AppDbContext db, IEmployeeEventLogService eventLog)
    {
        _db = db;
        _eventLog = eventLog;
    }

    public async Task<ServiceResult<IEnumerable<EmployeeAddressReadDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _db
            .EmployeeAddresses.AsNoTracking()
            .Where(a => a.DeletedAt == null)
            .Include(a => a.City)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeAddressReadDto>>.Ok(entities.Select(Map));
    }

    public async Task<ServiceResult<EmployeeAddressReadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var a = await _db
            .EmployeeAddresses.Include(x => x.Employee)
            .Include(x => x.City)
                .ThenInclude(c => c!.Country)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        return a == null
            ? ServiceResult<EmployeeAddressReadDto>.Fail("Adresse introuvable.")
            : ServiceResult<EmployeeAddressReadDto>.Ok(Map(a));
    }

    public async Task<ServiceResult<IEnumerable<EmployeeAddressReadDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    )
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == employeeId && e.DeletedAt == null, ct);
        if (!employeeExists)
            return ServiceResult<IEnumerable<EmployeeAddressReadDto>>.Fail("Employé introuvable.");

        var list = await _db
            .EmployeeAddresses.Where(a => a.EmployeeId == employeeId && a.DeletedAt == null)
            .Include(a => a.Employee)
            .Include(a => a.City)
                .ThenInclude(c => c!.Country)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => Map(a))
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeAddressReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<EmployeeAddressReadDto>> CreateAsync(
        EmployeeAddressCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var employeeExists = await _db.Employees.AnyAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null, ct);
        if (!employeeExists)
            return ServiceResult<EmployeeAddressReadDto>.Fail("Employé introuvable.");

        var city = await _db
            .Cities.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == dto.CityId && c.DeletedAt == null, ct);
        if (city == null)
            return ServiceResult<EmployeeAddressReadDto>.Fail("Ville introuvable.");

        var countryExists = await _db.Countries.AnyAsync(c => c.Id == dto.CountryId && c.DeletedAt == null, ct);
        if (!countryExists)
            return ServiceResult<EmployeeAddressReadDto>.Fail("Pays introuvable.");

        if (city.CountryId != dto.CountryId)
            return ServiceResult<EmployeeAddressReadDto>.Fail("La ville ne correspond pas au pays spécifié.");

        var a = new EmployeeAddress
        {
            EmployeeId = dto.EmployeeId,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            ZipCode = dto.ZipCode,
            CityId = dto.CityId,
            CreatedBy = createdBy,
        };
        _db.EmployeeAddresses.Add(a);
        await _db.SaveChangesAsync(ct);
        var created = await _db
            .EmployeeAddresses.Include(x => x.Employee)
            .Include(x => x.City)
                .ThenInclude(c => c!.Country)
            .FirstAsync(x => x.Id == a.Id, ct);
        var newAddrStr = $"{dto.AddressLine1}, {created.City?.CityName}";
        await _eventLog.LogSimpleEventAsync(
            dto.EmployeeId,
            EmployeeEventLogNames.AddressCreated,
            null,
            newAddrStr,
            createdBy,
            ct
        );
        return ServiceResult<EmployeeAddressReadDto>.Ok(Map(created));
    }

    public async Task<ServiceResult<EmployeeAddressReadDto>> UpdateAsync(
        int id,
        EmployeeAddressUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var a = await _db
            .EmployeeAddresses.Include(x => x.City)
                .ThenInclude(c => c!.Country)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a == null || a.DeletedAt != null)
            return ServiceResult<EmployeeAddressReadDto>.Fail("Adresse introuvable.");

        var oldAddrStr = $"{a.AddressLine1}, {a.City?.CityName}";

        if (dto.AddressLine1 != null)
            a.AddressLine1 = dto.AddressLine1;
        if (dto.AddressLine2 != null)
            a.AddressLine2 = dto.AddressLine2;
        if (dto.ZipCode != null)
            a.ZipCode = dto.ZipCode;
        if (dto.CityId != null)
        {
            var cityExists = await _db.Cities.AnyAsync(c => c.Id == dto.CityId.Value && c.DeletedAt == null, ct);
            if (!cityExists)
                return ServiceResult<EmployeeAddressReadDto>.Fail("Ville introuvable.");
            a.CityId = dto.CityId.Value;
        }

        if (dto.CountryId.HasValue)
        {
            var countryExists = await _db.Countries.AnyAsync(
                c => c.Id == dto.CountryId.Value && c.DeletedAt == null,
                ct
            );
            if (!countryExists)
                return ServiceResult<EmployeeAddressReadDto>.Fail("Pays introuvable.");

            var currentCityId = dto.CityId ?? a.CityId;
            var city = await _db
                .Cities.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == currentCityId && c.DeletedAt == null, ct);
            if (city == null || city.CountryId != dto.CountryId.Value)
                return ServiceResult<EmployeeAddressReadDto>.Fail("La ville ne correspond pas au pays spécifié.");
        }

        a.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        var updated = await _db
            .EmployeeAddresses.Include(x => x.Employee)
            .Include(x => x.City)
                .ThenInclude(c => c!.Country)
            .FirstAsync(x => x.Id == id, ct);
        var newAddrStr = $"{updated.AddressLine1}, {updated.City?.CityName}";
        await _eventLog.LogSimpleEventAsync(
            a.EmployeeId,
            EmployeeEventLogNames.AddressUpdated,
            oldAddrStr,
            newAddrStr,
            updatedBy,
            ct
        );
        return ServiceResult<EmployeeAddressReadDto>.Ok(Map(updated));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var a = await _db.EmployeeAddresses.FindAsync(new object[] { id }, ct);
        if (a == null || a.DeletedAt != null)
            return ServiceResult.Fail("Adresse introuvable.");
        a.DeletedAt = DateTimeOffset.UtcNow;
        a.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private static EmployeeAddressReadDto Map(EmployeeAddress a) =>
        new()
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeFullName =
                a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}".Trim() : string.Empty,
            AddressLine1 = a.AddressLine1,
            AddressLine2 = a.AddressLine2,
            ZipCode = a.ZipCode,
            CityId = a.CityId,
            CityName = a.City?.CityName ?? string.Empty,
            CountryId = a.City?.CountryId ?? 0,
            CountryName = a.City?.Country?.CountryName ?? string.Empty,
            CreatedAt = a.CreatedAt.DateTime,
        };
}

// ════════════════════════════════════════════════════════════
// FAMILLE (enfants + conjoint)
// ════════════════════════════════════════════════════════════

public class EmployeeFamilyService : IEmployeeFamilyService
{
    private readonly AppDbContext _db;

    public EmployeeFamilyService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<EmployeeChildReadDto>> GetChildByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.EmployeeChildren.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        return c == null
            ? ServiceResult<EmployeeChildReadDto>.Fail("Enfant introuvable.")
            : ServiceResult<EmployeeChildReadDto>.Ok(MapChild(c));
    }

    public async Task<ServiceResult<IEnumerable<EmployeeChildReadDto>>> GetChildrenAsync(
        int employeeId,
        CancellationToken ct = default
    )
    {
        var list = await _db
            .EmployeeChildren.Where(c => c.EmployeeId == employeeId)
            .Select(c => MapChild(c))
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeChildReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<EmployeeChildReadDto>> CreateChildAsync(
        EmployeeChildCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var c = new EmployeeChild
        {
            EmployeeId = dto.EmployeeId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            GenderId = dto.GenderId,
            IsDependent = dto.IsDependent,
            IsStudent = dto.IsStudent,
            CreatedBy = createdBy,
        };
        _db.EmployeeChildren.Add(c);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeChildReadDto>.Ok(MapChild(c));
    }

    public async Task<ServiceResult<EmployeeChildReadDto>> UpdateChildAsync(
        int id,
        EmployeeChildUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var c = await _db.EmployeeChildren.FindAsync(new object[] { id }, ct);
        if (c == null)
            return ServiceResult<EmployeeChildReadDto>.Fail("Enfant introuvable.");
        c.IsDependent = dto.IsDependent;
        c.IsStudent = dto.IsStudent;
        c.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeChildReadDto>.Ok(MapChild(c));
    }

    public async Task<ServiceResult> DeleteChildAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var c = await _db.EmployeeChildren.FindAsync(new object[] { id }, ct);
        if (c == null)
            return ServiceResult.Fail("Enfant introuvable.");
        c.DeletedAt = DateTimeOffset.UtcNow;
        c.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<IEnumerable<EmployeeSpouseReadDto>>> GetSpousesAsync(
        int employeeId,
        CancellationToken ct = default
    )
    {
        var list = await _db
            .EmployeeSpouses.Where(s => s.EmployeeId == employeeId)
            .Select(s => MapSpouse(s))
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeSpouseReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<EmployeeSpouseReadDto>> CreateSpouseAsync(
        EmployeeSpouseCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var s = new EmployeeSpouse
        {
            EmployeeId = dto.EmployeeId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            GenderId = dto.GenderId,
            CinNumber = dto.CinNumber,
            IsDependent = dto.IsDependent,
            CreatedBy = createdBy,
        };
        _db.EmployeeSpouses.Add(s);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeSpouseReadDto>.Ok(MapSpouse(s));
    }

    public async Task<ServiceResult<EmployeeSpouseReadDto>> UpdateSpouseAsync(
        int id,
        EmployeeSpouseUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var s = await _db.EmployeeSpouses.FindAsync(new object[] { id }, ct);
        if (s == null || s.DeletedAt != null)
            return ServiceResult<EmployeeSpouseReadDto>.Fail("Conjoint introuvable.");
        ApplySpouseUpdate(s, dto);
        s.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeSpouseReadDto>.Ok(MapSpouse(s));
    }

    public async Task<ServiceResult<EmployeeSpouseReadDto>> UpdateSpouseByEmployeeAsync(
        int employeeId,
        EmployeeSpouseUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var s = await _db.EmployeeSpouses.FirstOrDefaultAsync(
            x => x.EmployeeId == employeeId && x.DeletedAt == null,
            ct
        );
        if (s == null)
            return ServiceResult<EmployeeSpouseReadDto>.Fail("Aucun conjoint trouvé.");
        ApplySpouseUpdate(s, dto);
        s.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeSpouseReadDto>.Ok(MapSpouse(s));
    }

    public async Task<ServiceResult> DeleteSpouseAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var s = await _db.EmployeeSpouses.FindAsync(new object[] { id }, ct);
        if (s == null)
            return ServiceResult.Fail("Conjoint introuvable.");
        s.DeletedAt = DateTimeOffset.UtcNow;
        s.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteSpouseByEmployeeAsync(
        int employeeId,
        int deletedBy,
        CancellationToken ct = default
    )
    {
        var s = await _db.EmployeeSpouses.FirstOrDefaultAsync(
            x => x.EmployeeId == employeeId && x.DeletedAt == null,
            ct
        );
        if (s == null)
            return ServiceResult.Fail("Aucun conjoint trouvé.");
        s.DeletedAt = DateTimeOffset.UtcNow;
        s.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private static void ApplySpouseUpdate(EmployeeSpouse s, EmployeeSpouseUpdateDto dto)
    {
        s.FirstName = dto.FirstName.Trim();
        s.LastName = dto.LastName.Trim();
        s.DateOfBirth = dto.DateOfBirth;
        s.GenderId = dto.GenderId;
        s.CinNumber = string.IsNullOrWhiteSpace(dto.CinNumber) ? null : dto.CinNumber.Trim();
        s.IsDependent = dto.IsDependent;
    }

    private static EmployeeChildReadDto MapChild(EmployeeChild c) =>
        new()
        {
            Id = c.Id,
            EmployeeId = c.EmployeeId,
            FirstName = c.FirstName,
            LastName = c.LastName,
            DateOfBirth = c.DateOfBirth,
            IsDependent = c.IsDependent,
            IsStudent = c.IsStudent,
        };

    private static EmployeeSpouseReadDto MapSpouse(EmployeeSpouse s) =>
        new()
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            FirstName = s.FirstName,
            LastName = s.LastName,
            DateOfBirth = s.DateOfBirth,
            GenderId = s.GenderId,
            CinNumber = s.CinNumber,
            IsDependent = s.IsDependent,
        };
}

// ════════════════════════════════════════════════════════════
// POINTAGE (Attendance)
// ════════════════════════════════════════════════════════════

public class EmployeeAttendanceService : IEmployeeAttendanceService
{
    private readonly AppDbContext _db;

    public EmployeeAttendanceService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>> GetAllAsync(
        DateOnly? startDate,
        DateOnly? endDate,
        int? employeeId,
        AttendanceStatus? status,
        bool includeBreaks = false,
        CancellationToken ct = default
    )
    {
        var q = _db.EmployeeAttendances.AsNoTracking().Where(a => a.DeletedAt == null);
        if (startDate.HasValue)
            q = q.Where(a => a.WorkDate >= startDate.Value);
        if (endDate.HasValue)
            q = q.Where(a => a.WorkDate <= endDate.Value);
        if (employeeId.HasValue)
            q = q.Where(a => a.EmployeeId == employeeId.Value);
        if (status.HasValue)
            q = q.Where(a => a.Status == status.Value);
        if (includeBreaks)
            q = q.Include(a => a.Breaks);
        var list = await q.OrderByDescending(a => a.WorkDate).ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>.Ok(
            list.Select(a => MapAttendance(a, includeBreaks))
        );
    }

    public async Task<ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>> GetByEmployeeAsync(
        int employeeId,
        DateOnly? from,
        DateOnly? to,
        bool includeBreaks = false,
        CancellationToken ct = default
    )
    {
        var q = _db.EmployeeAttendances.AsNoTracking().Where(a => a.EmployeeId == employeeId && a.DeletedAt == null);
        if (from.HasValue)
            q = q.Where(a => a.WorkDate >= from.Value);
        if (to.HasValue)
            q = q.Where(a => a.WorkDate <= to.Value);
        if (includeBreaks)
            q = q.Include(a => a.Breaks);
        var list = await q.OrderByDescending(a => a.WorkDate).ToListAsync(ct);
        return ServiceResult<IEnumerable<EmployeeAttendanceReadDto>>.Ok(
            list.Select(a => MapAttendance(a, includeBreaks))
        );
    }

    public async Task<ServiceResult<EmployeeAttendanceReadDto>> GetByIdAsync(
        int id,
        bool includeBreaks = false,
        CancellationToken ct = default
    )
    {
        IQueryable<EmployeeAttendance> q = _db
            .EmployeeAttendances.AsNoTracking()
            .Where(x => x.Id == id && x.DeletedAt == null);
        if (includeBreaks)
            q = q.Include(a => a.Breaks);
        var a = await q.FirstOrDefaultAsync(ct);
        if (a == null)
            return ServiceResult<EmployeeAttendanceReadDto>.Fail("Pointage introuvable.");
        return ServiceResult<EmployeeAttendanceReadDto>.Ok(MapAttendance(a, includeBreaks));
    }

    public async Task<ServiceResult<EmployeeAttendanceReadDto>> CreateAsync(
        EmployeeAttendanceCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var a = new EmployeeAttendance
        {
            EmployeeId = dto.EmployeeId,
            WorkDate = dto.WorkDate,
            CheckIn = dto.CheckIn,
            CheckOut = dto.CheckOut,
            WorkedHours = 0m,
            Status = AttendanceStatus.Present,
            Source = dto.Source,
            CreatedBy = createdBy,
        };
        _db.EmployeeAttendances.Add(a);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAttendanceReadDto>.Ok(MapAttendance(a, false));
    }

    public async Task<ServiceResult<EmployeeAttendanceReadDto>> UpdateAsync(
        int id,
        EmployeeAttendanceUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var a = await _db.EmployeeAttendances.FindAsync(new object[] { id }, ct);
        if (a == null)
            return ServiceResult<EmployeeAttendanceReadDto>.Fail("Pointage introuvable.");
        if (dto.CheckIn.HasValue)
            a.CheckIn = dto.CheckIn;
        if (dto.CheckOut.HasValue)
            a.CheckOut = dto.CheckOut;
        a.UpdatedBy = updatedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAttendanceReadDto>.Ok(MapAttendance(a, false));
    }

    public async Task<ServiceResult<EmployeeAttendanceReadDto>> PutAsync(
        int id,
        EmployeeAttendanceCreateDto dto,
        int userId,
        CancellationToken ct = default
    )
    {
        var a = await _db.EmployeeAttendances.FindAsync(new object[] { id }, ct);
        if (a == null || a.DeletedAt != null)
            return ServiceResult<EmployeeAttendanceReadDto>.Fail("Pointage introuvable.");
        a.CheckIn = dto.CheckIn;
        a.CheckOut = dto.CheckOut;
        a.Source = dto.Source;
        if (a.CheckIn.HasValue && a.CheckOut.HasValue)
            a.WorkedHours = (decimal)(a.CheckOut.Value - a.CheckIn.Value).TotalHours;
        a.UpdatedBy = userId;
        a.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAttendanceReadDto>.Ok(MapAttendance(a, false));
    }

    public async Task<ServiceResult<EmployeeAttendanceReadDto>> CheckInAsync(
        int employeeId,
        EmployeeAttendanceCreateDto? dto,
        int userId,
        CancellationToken ct = default
    )
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        var attendance = await _db.EmployeeAttendances.FirstOrDefaultAsync(
            a => a.EmployeeId == employeeId && a.WorkDate == date && a.DeletedAt == null,
            ct
        );

        if (attendance != null && attendance.CheckIn.HasValue)
            return ServiceResult<EmployeeAttendanceReadDto>.Fail("L'employé a déjà pointé son entrée aujourd'hui.");

        if (attendance == null)
        {
            attendance = new EmployeeAttendance
            {
                EmployeeId = employeeId,
                WorkDate = date,
                CheckIn = now,
                CheckOut = null,
                WorkedHours = 0m,
                Status = AttendanceStatus.Present,
                Source = dto?.Source ?? AttendanceSource.Manual,
                CreatedBy = userId,
            };
            _db.EmployeeAttendances.Add(attendance);
        }
        else
        {
            attendance.CheckIn = now;
            attendance.Status = AttendanceStatus.Present;
            attendance.UpdatedBy = userId;
            attendance.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAttendanceReadDto>.Ok(MapAttendance(attendance, false));
    }

    public async Task<ServiceResult<EmployeeAttendanceReadDto>> CheckOutAsync(
        int employeeId,
        int? attendanceId,
        int userId,
        CancellationToken ct = default
    )
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        EmployeeAttendance? att;
        if (attendanceId.HasValue)
            att = await _db.EmployeeAttendances.FirstOrDefaultAsync(
                a => a.Id == attendanceId.Value && a.EmployeeId == employeeId,
                ct
            );
        else
            att = await _db
                .EmployeeAttendances.OrderByDescending(a => a.WorkDate)
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == date && a.CheckOut == null, ct);
        if (att == null)
            return ServiceResult<EmployeeAttendanceReadDto>.Fail("Pointage introuvable ou déjà clôturé.");
        att.CheckOut = TimeOnly.FromDateTime(DateTime.UtcNow);
        if (att.CheckIn.HasValue && att.CheckOut.HasValue)
            att.WorkedHours = (decimal)(att.CheckOut.Value - att.CheckIn.Value).TotalHours;
        att.UpdatedBy = userId;
        att.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAttendanceReadDto>.Ok(MapAttendance(att, false));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var a = await _db.EmployeeAttendances.FindAsync(new object[] { id }, ct);
        if (a == null)
            return ServiceResult.Fail("Pointage introuvable.");
        a.DeletedAt = DateTimeOffset.UtcNow;
        a.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public Task<ServiceResult<TimesheetImportResultDto>> ImportTimesheetAsync(
        int companyId,
        int month,
        int year,
        IEnumerable<object> rows,
        int userId,
        CancellationToken ct = default
    )
    {
        // Import timesheet géré par TimesheetImportController (api/timesheets/import)
        return Task.FromResult(
            ServiceResult<TimesheetImportResultDto>.Fail("Utilisez l'endpoint POST api/timesheets/import directement.")
        );
    }

    private static EmployeeAttendanceReadDto MapAttendance(EmployeeAttendance a, bool includeBreaks)
    {
        var dto = new EmployeeAttendanceReadDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            WorkDate = a.WorkDate,
            CheckIn = a.CheckIn,
            CheckOut = a.CheckOut,
            WorkedHours = a.WorkedHours,
            BreakMinutesApplied = a.BreakMinutesApplied,
            Status = a.Status,
            Source = a.Source,
        };
        if (includeBreaks && a.Breaks != null && a.Breaks.Count > 0)
        {
            dto.Breaks = a
                .Breaks.OrderBy(b => b.BreakStart)
                .Select(b => new EmployeeAttendanceBreakReadDto
                {
                    Id = b.Id,
                    BreakStart = b.BreakStart,
                    BreakEnd = b.BreakEnd,
                    BreakType = b.BreakType ?? string.Empty,
                    CreatedAt = b.CreatedAt,
                    ModifiedAt = b.UpdatedAt,
                })
                .ToList();
        }
        return dto;
    }
}

// ════════════════════════════════════════════════════════════
// ABSENCES
// ════════════════════════════════════════════════════════════

public class EmployeeAbsenceService : IEmployeeAbsenceService
{
    private readonly AppDbContext _db;

    public EmployeeAbsenceService(AppDbContext db) => _db = db;

    private static string DurationDescription(AbsenceDurationType t) =>
        t switch
        {
            AbsenceDurationType.FullDay => "Journée entière",
            AbsenceDurationType.HalfDay => "Demi-journée",
            AbsenceDurationType.Hourly => "Horaire",
            _ => t.ToString(),
        };

    private static string? HalfDayDescription(AbsenceDurationType dt, bool? isMorning) =>
        dt == AbsenceDurationType.HalfDay
            ? (
                isMorning == true ? "Matin"
                : isMorning == false ? "Après-midi"
                : null
            )
            : null;

    private async Task<bool> IsRhOrAdminAsync(int userId, CancellationToken ct)
    {
        // Pas de StringComparison dans LINQ → EF ne peut pas traduire en SQL
        return await _db
            .UsersRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId && ur.DeletedAt == null)
            .Join(
                _db.Roles.AsNoTracking().Where(r => r.DeletedAt == null),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Name
            )
            .AnyAsync(n => n.ToLower() == "rh" || n.ToLower() == "admin", ct);
    }

    private async Task<EmployeeAbsenceReadDto> ToReadDtoAsync(EmployeeAbsence a, CancellationToken ct)
    {
        var emp = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == a.EmployeeId, ct);
        string? decisionName = null;
        string? createdName = null;
        if (a.DecisionBy.HasValue)
        {
            decisionName = await _db
                .Users.AsNoTracking()
                .Where(u => u.Id == a.DecisionBy.Value)
                .Select(u => u.Username)
                .FirstOrDefaultAsync(ct);
        }
        if (a.CreatedBy > 0)
        {
            createdName = await _db
                .Users.AsNoTracking()
                .Where(u => u.Id == a.CreatedBy)
                .Select(u => u.Username)
                .FirstOrDefaultAsync(ct);
        }

        var fn = emp?.FirstName ?? "";
        var ln = emp?.LastName ?? "";
        return new EmployeeAbsenceReadDto
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeFirstName = fn,
            EmployeeLastName = ln,
            EmployeeFullName = $"{fn} {ln}".Trim(),
            AbsenceDate = a.AbsenceDate,
            AbsenceDateFormatted = a.AbsenceDate.ToString("yyyy-MM-dd"),
            DurationType = a.DurationType,
            DurationTypeDescription = DurationDescription(a.DurationType),
            IsMorning = a.IsMorning,
            HalfDayDescription = HalfDayDescription(a.DurationType, a.IsMorning),
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            AbsenceType = a.AbsenceType,
            Reason = a.Reason,
            Status = a.Status,
            StatusDescription = a.Status.ToString(),
            DecisionAt = a.DecisionAt?.UtcDateTime,
            DecisionBy = a.DecisionBy,
            DecisionByName = decisionName,
            DecisionComment = a.DecisionComment,
            CreatedAt = a.CreatedAt.UtcDateTime,
            CreatedBy = a.CreatedBy,
            CreatedByName = createdName,
        };
    }

    public async Task<ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    )
    {
        var rows = await _db
            .EmployeeAbsences.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.DeletedAt == null)
            .OrderByDescending(a => a.AbsenceDate)
            .ToListAsync(ct);
        var list = new List<EmployeeAbsenceReadDto>();
        foreach (var a in rows)
            list.Add(await ToReadDtoAsync(a, ct));
        return ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>> GetByCompanyAsync(
        int companyId,
        int? employeeId,
        DateOnly? startDate,
        DateOnly? endDate,
        AbsenceDurationType? durationType,
        AbsenceStatus? status,
        string? absenceType,
        int limit,
        CancellationToken ct = default
    )
    {
        if (limit <= 0 || limit > 10000)
            return ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>.Fail("Le limit doit être entre 1 et 10000.");

        var companyExists = await _db
            .Companies.AsNoTracking()
            .AnyAsync(c => c.Id == companyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>.Fail("Société non trouvée");

        var query = _db
            .EmployeeAbsences.AsNoTracking()
            .Where(a => a.DeletedAt == null && a.Employee.CompanyId == companyId && a.Employee.DeletedAt == null);

        if (employeeId.HasValue)
        {
            var ok = await _db.Employees.AnyAsync(
                e => e.Id == employeeId && e.CompanyId == companyId && e.DeletedAt == null,
                ct
            );
            if (!ok)
                return ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>.Fail("Employé non trouvé pour cette société");
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (startDate.HasValue)
            query = query.Where(a => a.AbsenceDate >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(a => a.AbsenceDate <= endDate.Value);
        if (durationType.HasValue)
            query = query.Where(a => a.DurationType == durationType.Value);
        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(absenceType))
        {
            var t = absenceType.Trim();
            query = query.Where(a => a.AbsenceType == t);
        }

        var absences = await query.OrderByDescending(a => a.AbsenceDate).Take(limit).ToListAsync(ct);

        var list = new List<EmployeeAbsenceReadDto>();
        foreach (var a in absences)
            list.Add(await ToReadDtoAsync(a, ct));
        return ServiceResult<IEnumerable<EmployeeAbsenceReadDto>>.Ok(list);
    }

    public async Task<ServiceResult<IEnumerable<string>>> GetDistinctTypesAsync(
        int companyId,
        CancellationToken ct = default
    )
    {
        var companyExists = await _db
            .Companies.AsNoTracking()
            .AnyAsync(c => c.Id == companyId && c.DeletedAt == null, ct);
        if (!companyExists)
            return ServiceResult<IEnumerable<string>>.Fail("Société non trouvée");

        var types = await _db
            .EmployeeAbsences.AsNoTracking()
            .Where(a => a.DeletedAt == null && a.Employee.CompanyId == companyId && a.Employee.DeletedAt == null)
            .Select(a => a.AbsenceType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);
        return ServiceResult<IEnumerable<string>>.Ok(types);
    }

    public async Task<ServiceResult<EmployeeAbsenceReadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var a = await _db
            .EmployeeAbsences.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (a == null)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Absence introuvable.");
        return ServiceResult<EmployeeAbsenceReadDto>.Ok(await ToReadDtoAsync(a, ct));
    }

    public async Task<ServiceResult<EmployeeAbsenceStatsDto>> GetStatsAsync(
        int companyId,
        int? employeeId,
        CancellationToken ct = default
    )
    {
        var company = await _db
            .Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId && c.DeletedAt == null, ct);
        if (company == null)
            return ServiceResult<EmployeeAbsenceStatsDto>.Fail("Société non trouvée");

        var query = _db
            .EmployeeAbsences.AsNoTracking()
            .Where(a => a.DeletedAt == null && a.Employee.CompanyId == companyId && a.Employee.DeletedAt == null);

        string? employeeFullName = null;
        if (employeeId.HasValue)
        {
            var empRow = await _db
                .Employees.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == employeeId && e.CompanyId == companyId && e.DeletedAt == null, ct);
            if (empRow == null)
                return ServiceResult<EmployeeAbsenceStatsDto>.Fail("Employé non trouvé");
            query = query.Where(a => a.EmployeeId == employeeId.Value);
            employeeFullName = $"{empRow.FirstName} {empRow.LastName}";
        }

        var agg = await query
            .GroupBy(a => 1)
            .Select(g => new
            {
                TotalAbsences = g.Count(),
                FullDayAbsences = g.Count(a => a.DurationType == AbsenceDurationType.FullDay),
                HalfDayAbsences = g.Count(a => a.DurationType == AbsenceDurationType.HalfDay),
                HourlyAbsences = g.Count(a => a.DurationType == AbsenceDurationType.Hourly),
                SubmittedCount = g.Count(a => a.Status == AbsenceStatus.Submitted),
                ApprovedCount = g.Count(a => a.Status == AbsenceStatus.Approved),
                RejectedCount = g.Count(a => a.Status == AbsenceStatus.Rejected),
                CancelledCount = g.Count(a => a.Status == AbsenceStatus.Cancelled),
            })
            .FirstOrDefaultAsync(ct);

        var stats = new EmployeeAbsenceStatsDto
        {
            CompanyId = companyId,
            CompanyName = company.CompanyName,
            EmployeeId = employeeId,
            EmployeeFullName = employeeFullName,
            TotalAbsences = agg?.TotalAbsences ?? 0,
            FullDayAbsences = agg?.FullDayAbsences ?? 0,
            HalfDayAbsences = agg?.HalfDayAbsences ?? 0,
            HourlyAbsences = agg?.HourlyAbsences ?? 0,
            SubmittedCount = agg?.SubmittedCount ?? 0,
            ApprovedCount = agg?.ApprovedCount ?? 0,
            RejectedCount = agg?.RejectedCount ?? 0,
            CancelledCount = agg?.CancelledCount ?? 0,
            GeneratedAt = DateTimeOffset.UtcNow,
        };

        var byType = await query
            .GroupBy(a => a.AbsenceType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        stats.AbsencesByType = byType.ToDictionary(x => x.Type, x => x.Count);

        var byMonthRaw = await query
            .GroupBy(a => new { a.AbsenceDate.Year, a.AbsenceDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count(),
            })
            .ToListAsync(ct);
        stats.AbsencesByMonth = byMonthRaw
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToDictionary(x => $"{x.Year:0000}-{x.Month:00}", x => x.Count);

        return ServiceResult<EmployeeAbsenceStatsDto>.Ok(stats);
    }

    public async Task<ServiceResult<EmployeeAbsenceReadDto>> CreateAsync(
        EmployeeAbsenceCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    )
    {
        var employee = await _db
            .Employees.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null, ct);
        if (employee == null)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Employé non trouvé");

        if (dto.DurationType == AbsenceDurationType.HalfDay && !dto.IsMorning.HasValue)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("IsMorning est requis pour une demi-journée");

        if (dto.DurationType == AbsenceDurationType.Hourly)
        {
            if (!dto.StartTime.HasValue || !dto.EndTime.HasValue)
                return ServiceResult<EmployeeAbsenceReadDto>.Fail(
                    "StartTime et EndTime sont requis pour une absence horaire"
                );
            if (dto.StartTime >= dto.EndTime)
                return ServiceResult<EmployeeAbsenceReadDto>.Fail("L'heure de fin doit être après l'heure de début");
        }

        if (dto.DurationType == AbsenceDurationType.Hourly)
        {
            var overlapping = await _db
                .EmployeeAbsences.Where(a =>
                    a.DeletedAt == null
                    && a.EmployeeId == dto.EmployeeId
                    && a.AbsenceDate == dto.AbsenceDate
                    && a.DurationType == AbsenceDurationType.Hourly
                    && a.Status != AbsenceStatus.Rejected
                    && a.Status != AbsenceStatus.Cancelled
                )
                .ToListAsync(ct);
            foreach (var existing in overlapping)
            {
                if (
                    existing.StartTime.HasValue
                    && existing.EndTime.HasValue
                    && dto.StartTime.HasValue
                    && dto.EndTime.HasValue
                )
                {
                    if (dto.StartTime < existing.EndTime && dto.EndTime > existing.StartTime)
                        return ServiceResult<EmployeeAbsenceReadDto>.Fail(
                            $"Cette tranche horaire chevauche une absence existante ({existing.StartTime:HH\\:mm} - {existing.EndTime:HH\\:mm})"
                        );
                }
            }

            var fullOrHalf = await _db.EmployeeAbsences.AnyAsync(
                a =>
                    a.DeletedAt == null
                    && a.EmployeeId == dto.EmployeeId
                    && a.AbsenceDate == dto.AbsenceDate
                    && (a.DurationType == AbsenceDurationType.FullDay || a.DurationType == AbsenceDurationType.HalfDay)
                    && a.Status != AbsenceStatus.Rejected
                    && a.Status != AbsenceStatus.Cancelled,
                ct
            );
            if (fullOrHalf)
                return ServiceResult<EmployeeAbsenceReadDto>.Fail(
                    "Une absence journée entière ou demi-journée existe déjà pour cette date"
                );
        }
        else
        {
            var exists = await _db.EmployeeAbsences.AnyAsync(
                a =>
                    a.DeletedAt == null
                    && a.EmployeeId == dto.EmployeeId
                    && a.AbsenceDate == dto.AbsenceDate
                    && a.Status != AbsenceStatus.Rejected
                    && a.Status != AbsenceStatus.Cancelled,
                ct
            );
            if (exists)
                return ServiceResult<EmployeeAbsenceReadDto>.Fail("Une absence existe déjà pour cette date");
        }

        var isRh = await IsRhOrAdminAsync(createdBy, ct);
        AbsenceStatus initial;
        if (dto.Status.HasValue)
            initial = dto.Status.Value;
        else
            initial = isRh ? AbsenceStatus.Approved : AbsenceStatus.Draft;

        var absence = new EmployeeAbsence
        {
            EmployeeId = dto.EmployeeId,
            AbsenceDate = dto.AbsenceDate,
            DurationType = dto.DurationType,
            IsMorning = dto.DurationType == AbsenceDurationType.HalfDay ? dto.IsMorning : null,
            StartTime = dto.DurationType == AbsenceDurationType.Hourly ? dto.StartTime : null,
            EndTime = dto.DurationType == AbsenceDurationType.Hourly ? dto.EndTime : null,
            AbsenceType = dto.AbsenceType.Trim(),
            Reason = dto.Reason?.Trim(),
            Status = initial,
            DecisionAt = initial == AbsenceStatus.Approved ? DateTimeOffset.UtcNow : null,
            DecisionBy = initial == AbsenceStatus.Approved ? createdBy : null,
            DecisionComment = initial == AbsenceStatus.Approved ? "Approbation automatique (RH/Admin)" : null,
            CreatedBy = createdBy,
        };
        _db.EmployeeAbsences.Add(absence);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAbsenceReadDto>.Ok(await ToReadDtoAsync(absence, ct));
    }

    public async Task<ServiceResult<EmployeeAbsenceReadDto>> UpdateAsync(
        int id,
        EmployeeAbsenceUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var a = await _db.EmployeeAbsences.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (a == null)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Absence introuvable.");
        if (a.Status != AbsenceStatus.Draft)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail(
                "Seules les absences en brouillon peuvent être modifiées"
            );

        if (dto.AbsenceDate.HasValue)
            a.AbsenceDate = dto.AbsenceDate.Value;
        if (dto.DurationType.HasValue)
            a.DurationType = dto.DurationType.Value;
        if (dto.IsMorning.HasValue)
            a.IsMorning = dto.IsMorning.Value;
        if (dto.StartTime.HasValue)
            a.StartTime = dto.StartTime;
        if (dto.EndTime.HasValue)
            a.EndTime = dto.EndTime;
        if (dto.AbsenceType != null)
            a.AbsenceType = dto.AbsenceType.Trim();
        if (dto.Reason != null)
            a.Reason = dto.Reason.Trim();
        a.UpdatedBy = updatedBy;
        a.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAbsenceReadDto>.Ok(await ToReadDtoAsync(a, ct));
    }

    public async Task<ServiceResult<EmployeeAbsenceReadDto>> DecideAsync(
        int id,
        EmployeeAbsenceDecisionDto dto,
        int decidedBy,
        CancellationToken ct = default
    )
    {
        var a = await _db
            .EmployeeAbsences.Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (a == null)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Absence introuvable.");
        if (
            dto.Status != AbsenceStatus.Approved
            && dto.Status != AbsenceStatus.Rejected
            && dto.Status != AbsenceStatus.Cancelled
        )
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Le statut doit être Approved, Rejected ou Cancelled");
        if (a.Status != AbsenceStatus.Submitted)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail(
                $"Impossible de modifier une absence avec le statut {a.Status}"
            );

        a.Status = dto.Status;
        a.DecisionAt = DateTimeOffset.UtcNow;
        a.DecisionBy = decidedBy;
        a.DecisionComment = dto.DecisionComment?.Trim();
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAbsenceReadDto>.Ok(await ToReadDtoAsync(a, ct));
    }

    public async Task<ServiceResult<EmployeeAbsenceReadDto>> SubmitAsync(
        int id,
        int userId,
        CancellationToken ct = default
    )
    {
        var a = await _db
            .EmployeeAbsences.Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (a == null)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Absence introuvable.");
        if (a.Status != AbsenceStatus.Draft)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail(
                $"Impossible de soumettre une absence avec le statut {a.Status}"
            );

        var user = await _db
            .Users.Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct);
        if (user?.Employee == null)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Utilisateur non associé à un employé");
        if (user.EmployeeId != a.EmployeeId)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Accès refusé.");

        a.Status = AbsenceStatus.Submitted;
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAbsenceReadDto>.Ok(await ToReadDtoAsync(a, ct));
    }

    public async Task<ServiceResult<EmployeeAbsenceReadDto>> ApproveAsync(
        int id,
        int userId,
        string? comment,
        CancellationToken ct = default
    )
    {
        var a = await _db
            .EmployeeAbsences.Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (a == null)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Absence introuvable.");
        if (a.Status != AbsenceStatus.Submitted)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail(
                $"Impossible d'approuver une absence avec le statut {a.Status}"
            );

        a.Status = AbsenceStatus.Approved;
        a.DecisionAt = DateTimeOffset.UtcNow;
        a.DecisionBy = userId;
        a.DecisionComment = comment?.Trim();
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAbsenceReadDto>.Ok(await ToReadDtoAsync(a, ct));
    }

    public async Task<ServiceResult<EmployeeAbsenceReadDto>> RejectAsync(
        int id,
        int userId,
        string reason,
        CancellationToken ct = default
    )
    {
        var a = await _db
            .EmployeeAbsences.Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (a == null)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Absence introuvable.");
        if (a.Status != AbsenceStatus.Submitted)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail(
                $"Impossible de rejeter une absence avec le statut {a.Status}"
            );

        a.Status = AbsenceStatus.Rejected;
        a.DecisionAt = DateTimeOffset.UtcNow;
        a.DecisionBy = userId;
        a.DecisionComment = reason.Trim();
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAbsenceReadDto>.Ok(await ToReadDtoAsync(a, ct));
    }

    public async Task<ServiceResult<EmployeeAbsenceReadDto>> CancelAsync(
        int id,
        EmployeeAbsenceCancellationDto dto,
        int cancelledBy,
        CancellationToken ct = default
    )
    {
        var a = await _db
            .EmployeeAbsences.Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (a == null)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Absence introuvable.");
        if (a.Status != AbsenceStatus.Submitted && a.Status != AbsenceStatus.Approved)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail(
                $"Impossible d'annuler une absence avec le statut {a.Status}"
            );

        var user = await _db
            .Users.Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == cancelledBy && u.DeletedAt == null, ct);
        if (user?.Employee == null || user.EmployeeId != a.EmployeeId)
            return ServiceResult<EmployeeAbsenceReadDto>.Fail("Accès refusé.");

        a.Status = AbsenceStatus.Cancelled;
        a.DecisionAt = DateTimeOffset.UtcNow;
        a.DecisionBy = cancelledBy;
        a.DecisionComment = dto.Reason?.Trim();
        await _db.SaveChangesAsync(ct);
        return ServiceResult<EmployeeAbsenceReadDto>.Ok(await ToReadDtoAsync(a, ct));
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var a = await _db.EmployeeAbsences.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (a == null)
            return ServiceResult.Fail("Absence introuvable.");
        if (a.Status == AbsenceStatus.Approved)
            return ServiceResult.Fail("Impossible de supprimer une absence approuvée");
        a.DeletedAt = DateTimeOffset.UtcNow;
        a.DeletedBy = deletedBy;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
