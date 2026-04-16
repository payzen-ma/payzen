using Microsoft.EntityFrameworkCore;
using Payzen.Application.Common;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Auth;
using Payzen.Domain.Entities.Employee;
using Payzen.Domain.Enums;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.Employee;

/// <summary>Parité avec l’ancien <c>EmployeeOvertimeController</c> (détection type, split jour/nuit, règles de majoration, workflow).</summary>
public partial class EmployeeOvertimeService : IEmployeeOvertimeService
{
    private readonly AppDbContext _db;

    public EmployeeOvertimeService(AppDbContext db) => _db = db;

    public async Task<ServiceResult<IEnumerable<EmployeeOvertimeListDto>>> GetAllAsync(
        int? companyId,
        int? employeeId,
        OvertimeStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? isProcessedInPayroll,
        CancellationToken ct = default
    )
    {
        var q = _db.EmployeeOvertimes.AsNoTracking().Where(o => o.DeletedAt == null);
        if (companyId.HasValue)
            q = q.Where(o => o.Employee.CompanyId == companyId.Value);
        if (employeeId.HasValue)
            q = q.Where(o => o.EmployeeId == employeeId.Value);
        if (status.HasValue)
            q = q.Where(o => o.Status == status.Value);
        if (fromDate.HasValue)
            q = q.Where(o => o.OvertimeDate >= fromDate.Value);
        if (toDate.HasValue)
            q = q.Where(o => o.OvertimeDate <= toDate.Value);
        if (isProcessedInPayroll.HasValue)
            q = q.Where(o => o.IsProcessedInPayroll == isProcessedInPayroll.Value);

        var rows = await q.Include(o => o.Employee)
            .Include(o => o.Holiday)
            .OrderByDescending(o => o.OvertimeDate)
            .ThenByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var list = rows.Select(o => new EmployeeOvertimeListDto
            {
                Id = o.Id,
                EmployeeFullName = o.Employee != null ? $"{o.Employee.FirstName} {o.Employee.LastName}" : string.Empty,
                OvertimeDate = o.OvertimeDate,
                OvertimeType = o.OverTimeType,
                OvertimeTypeDescription = OvertimeTypeHelper.GetDescription(o.OverTimeType),
                StartTime = o.StartTime,
                EndTime = o.EndTime,
                HolidayName = o.Holiday?.NameFr,
                RateRuleNameApplied = o.RateRuleNameApplied,
                EmployeeComment = o.EmployeeComment,
                DurationInHours = o.DurationInHours,
                RateMultiplierApplied = o.RateMultiplierApplied,
                Status = o.Status,
                StatusDescription = o.Status.ToString(),
                IsProcessedInPayroll = o.IsProcessedInPayroll,
                CreatedAt = o.CreatedAt.DateTime,
            })
            .ToList();

        return ServiceResult<IEnumerable<EmployeeOvertimeListDto>>.Ok(list);
    }

    public async Task<ServiceResult<EmployeeOvertimeStatsDto>> GetStatsAsync(
        int companyId,
        int? employeeId,
        CancellationToken ct = default
    )
    {
        var q = _db
            .EmployeeOvertimes.AsNoTracking()
            .Where(o => o.DeletedAt == null && o.Employee.CompanyId == companyId);
        if (employeeId.HasValue)
            q = q.Where(o => o.EmployeeId == employeeId.Value);

        var totalHours = await q.SumAsync(o => (decimal?)o.DurationInHours, ct) ?? 0m;
        var pending = await q.CountAsync(
            o => o.Status == OvertimeStatus.Draft || o.Status == OvertimeStatus.Submitted,
            ct
        );
        var approved = await q.CountAsync(o => o.Status == OvertimeStatus.Approved, ct);
        var rejected = await q.CountAsync(o => o.Status == OvertimeStatus.Rejected, ct);

        return ServiceResult<EmployeeOvertimeStatsDto>.Ok(
            new EmployeeOvertimeStatsDto
            {
                TotalOvertimeHours = totalHours,
                PendingCount = pending,
                ApprovedCount = approved,
                RejectedCount = rejected,
            }
        );
    }

    public async Task<ServiceResult<IEnumerable<EmployeeOvertimeListDto>>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken ct = default
    )
    {
        var rows = await _db
            .EmployeeOvertimes.AsNoTracking()
            .Where(o => o.DeletedAt == null && o.EmployeeId == employeeId)
            .Include(o => o.Employee)
            .Include(o => o.Holiday)
            .OrderByDescending(o => o.OvertimeDate)
            .ThenByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        var list = rows.Select(o => new EmployeeOvertimeListDto
            {
                Id = o.Id,
                EmployeeFullName = o.Employee != null ? $"{o.Employee.FirstName} {o.Employee.LastName}" : string.Empty,
                OvertimeDate = o.OvertimeDate,
                OvertimeType = o.OverTimeType,
                OvertimeTypeDescription = OvertimeTypeHelper.GetDescription(o.OverTimeType),
                StartTime = o.StartTime,
                EndTime = o.EndTime,
                HolidayName = o.Holiday?.NameFr,
                RateRuleNameApplied = o.RateRuleNameApplied,
                EmployeeComment = o.EmployeeComment,
                DurationInHours = o.DurationInHours,
                RateMultiplierApplied = o.RateMultiplierApplied,
                Status = o.Status,
                StatusDescription = o.Status.ToString(),
                IsProcessedInPayroll = o.IsProcessedInPayroll,
                CreatedAt = o.CreatedAt.DateTime,
            })
            .ToList();

        return ServiceResult<IEnumerable<EmployeeOvertimeListDto>>.Ok(list);
    }

    public async Task<ServiceResult<EmployeeOvertimeReadDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var dto = await ToReadDtoAsync(id, ct);
        return dto == null
            ? ServiceResult<EmployeeOvertimeReadDto>.Fail("Overtime non trouvé")
            : ServiceResult<EmployeeOvertimeReadDto>.Ok(dto);
    }

    public Task<ServiceResult<EmployeeOvertimeCreateOutcomeDto>> CreateAsync(
        EmployeeOvertimeCreateDto dto,
        int createdBy,
        CancellationToken ct = default
    ) => CreateOvertimesFullAsync(dto, createdBy, ct);

    public async Task<ServiceResult<EmployeeOvertimeReadDto>> UpdateAsync(
        int id,
        EmployeeOvertimeUpdateDto dto,
        int updatedBy,
        CancellationToken ct = default
    )
    {
        var o = await _db.EmployeeOvertimes.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (o == null)
            return ServiceResult<EmployeeOvertimeReadDto>.Fail("Overtime non trouvé");
        if (!o.CanBeModified())
            return ServiceResult<EmployeeOvertimeReadDto>.Fail(
                "Seuls les overtimes en brouillon peuvent être modifiés"
            );

        var hasChanges = false;
        if (dto.OvertimeDate.HasValue && dto.OvertimeDate.Value != o.OvertimeDate)
        {
            o.OvertimeDate = dto.OvertimeDate.Value;
            hasChanges = true;
        }
        if (dto.DurationInHours.HasValue && dto.DurationInHours.Value != o.DurationInHours)
        {
            o.DurationInHours = dto.DurationInHours.Value;
            hasChanges = true;
        }
        if (dto.StartTime.HasValue && dto.StartTime.Value != o.StartTime)
        {
            o.StartTime = dto.StartTime.Value;
            hasChanges = true;
        }
        if (dto.EndTime.HasValue && dto.EndTime.Value != o.EndTime)
        {
            o.EndTime = dto.EndTime.Value;
            hasChanges = true;
        }
        if (!string.IsNullOrWhiteSpace(dto.EmployeeComment) && dto.EmployeeComment != o.EmployeeComment)
        {
            o.EmployeeComment = dto.EmployeeComment.Trim();
            hasChanges = true;
        }

        if (hasChanges)
        {
            o.UpdatedBy = updatedBy;
            o.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        var read = await ToReadDtoAsync(id, ct);
        return read == null
            ? ServiceResult<EmployeeOvertimeReadDto>.Fail("Overtime non trouvé")
            : ServiceResult<EmployeeOvertimeReadDto>.Ok(read);
    }

    public async Task<ServiceResult<EmployeeOvertimeReadDto>> SubmitAsync(
        int id,
        EmployeeOvertimeSubmitDto dto,
        int userId,
        CancellationToken ct = default
    )
    {
        var o = await _db.EmployeeOvertimes.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (o == null)
            return ServiceResult<EmployeeOvertimeReadDto>.Fail("Overtime non trouvé");
        if (o.Status != OvertimeStatus.Draft)
            return ServiceResult<EmployeeOvertimeReadDto>.Fail("Seuls les overtimes en brouillon peuvent être soumis");

        var user = await _db
            .Users.AsNoTracking()
            .Include(u => u.UsersRoles!)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct);
        var isRh =
            user?.UsersRoles?.Any(ur =>
                ur.Role != null && ur.Role.Name.Equals("RH", StringComparison.OrdinalIgnoreCase)
            )
            ?? false;

        if (user?.EmployeeId != o.EmployeeId && !isRh)
            return ServiceResult<EmployeeOvertimeReadDto>.Fail("Accès refusé.");

        o.Status = OvertimeStatus.Submitted;
        o.EmployeeComment = dto.EmployeeComment?.Trim();
        o.UpdatedBy = userId;
        o.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var read = await ToReadDtoAsync(id, ct);
        return read == null
            ? ServiceResult<EmployeeOvertimeReadDto>.Fail("Overtime non trouvé")
            : ServiceResult<EmployeeOvertimeReadDto>.Ok(read);
    }

    public async Task<ServiceResult<EmployeeOvertimeReadDto>> DecideAsync(
        int id,
        EmployeeOvertimeApprovalDto dto,
        int decidedBy,
        CancellationToken ct = default
    )
    {
        if (dto.Status != OvertimeStatus.Approved && dto.Status != OvertimeStatus.Rejected)
            return ServiceResult<EmployeeOvertimeReadDto>.Fail("Status doit être Approved ou Rejected");

        var overtime = await _db
            .EmployeeOvertimes.Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (overtime == null)
            return ServiceResult<EmployeeOvertimeReadDto>.Fail("Overtime non trouvé");

        var user = await _db
            .Users.AsNoTracking()
            .Include(u => u.Employee)
            .Include(u => u.UsersRoles!)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == decidedBy && u.DeletedAt == null, ct);

        var isRh =
            user?.UsersRoles?.Any(ur =>
                ur.Role != null && ur.Role.Name.Equals("RH", StringComparison.OrdinalIgnoreCase)
            )
            ?? false;

        if (user?.EmployeeId != overtime.Employee?.ManagerId && !isRh)
            return ServiceResult<EmployeeOvertimeReadDto>.Fail("Accès refusé.");

        overtime.Status = dto.Status;
        overtime.ManagerComment = dto.ManagerComment?.Trim();
        overtime.ApprovedBy = decidedBy;
        overtime.ApprovedAt = DateTimeOffset.UtcNow;
        overtime.UpdatedBy = decidedBy;
        overtime.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var read = await ToReadDtoAsync(id, ct);
        return read == null
            ? ServiceResult<EmployeeOvertimeReadDto>.Fail("Overtime non trouvé")
            : ServiceResult<EmployeeOvertimeReadDto>.Ok(read);
    }

    public async Task<ServiceResult<EmployeeOvertimeReadDto>> CancelAsync(
        int id,
        int userId,
        CancellationToken ct = default
    )
    {
        var user = await _db
            .Users.Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, ct);

        var overtime = await _db
            .EmployeeOvertimes.Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (overtime == null)
            return ServiceResult<EmployeeOvertimeReadDto>.Fail("Overtime non trouvé");

        if (overtime.Status != OvertimeStatus.Submitted && overtime.Status != OvertimeStatus.Approved)
            return ServiceResult<EmployeeOvertimeReadDto>.Fail(
                "Seuls les overtimes soumis ou approuvés peuvent être annulés"
            );

        var isRhSameCompany = false;
        if (user?.Employee != null)
        {
            var companyId = user.Employee.CompanyId;
            isRhSameCompany = await (
                from ur in _db.UsersRoles.AsNoTracking()
                join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                join u in _db.Users.AsNoTracking() on ur.UserId equals u.Id
                join e in _db.Employees.AsNoTracking() on u.EmployeeId equals e.Id
                where
                    ur.UserId == userId
                    && r.Name.Equals("RH", StringComparison.OrdinalIgnoreCase)
                    && e.CompanyId == companyId
                select 1
            ).AnyAsync(ct);
        }

        if (user?.EmployeeId != overtime.EmployeeId && !isRhSameCompany)
            return ServiceResult<EmployeeOvertimeReadDto>.Fail("Accès refusé.");

        overtime.Status = OvertimeStatus.Cancelled;
        overtime.UpdatedBy = userId;
        overtime.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var read = await ToReadDtoAsync(id, ct);
        return read == null
            ? ServiceResult<EmployeeOvertimeReadDto>.Fail("Overtime non trouvé")
            : ServiceResult<EmployeeOvertimeReadDto>.Ok(read);
    }

    public async Task<ServiceResult> DeleteAsync(int id, int deletedBy, CancellationToken ct = default)
    {
        var o = await _db.EmployeeOvertimes.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (o == null)
            return ServiceResult.Fail("Overtime non trouvé");
        if (o.IsProcessedInPayroll)
            return ServiceResult.Fail("Impossible de supprimer un overtime déjà traité en paie");

        o.DeletedBy = deletedBy;
        o.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    private async Task<EmployeeOvertimeReadDto?> ToReadDtoAsync(int id, CancellationToken ct)
    {
        var o = await _db
            .EmployeeOvertimes.AsNoTracking()
            .Include(x => x.Employee)
            .Include(x => x.Holiday)
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null, ct);
        if (o == null)
            return null;

        var approverName = await GetApproverNameAsync(o.ApprovedBy, ct);
        return MapToReadDto(o, approverName);
    }

    private async Task<string?> GetApproverNameAsync(int? approvedBy, CancellationToken ct)
    {
        if (!approvedBy.HasValue)
            return null;
        var u = await _db
            .Users.AsNoTracking()
            .Include(x => x.Employee)
            .FirstOrDefaultAsync(x => x.Id == approvedBy.Value && x.DeletedAt == null, ct);
        if (u?.Employee != null)
            return $"{u.Employee.FirstName} {u.Employee.LastName}".Trim();
        return u?.Username;
    }

    private static EmployeeOvertimeReadDto MapToReadDto(EmployeeOvertime o, string? approvedByName) =>
        new()
        {
            Id = o.Id,
            EmployeeId = o.EmployeeId,
            EmployeeFullName = o.Employee != null ? $"{o.Employee.FirstName} {o.Employee.LastName}" : string.Empty,
            OvertimeType = o.OverTimeType,
            OvertimeTypeDescription = OvertimeTypeHelper.GetDescription(o.OverTimeType),
            EntryMode = o.EntryMode,
            HolidayId = o.HolidayId,
            HolidayName = o.Holiday?.NameFr,
            OvertimeDate = o.OvertimeDate,
            StartTime = o.StartTime,
            EndTime = o.EndTime,
            CrossesMidnight = o.CrossesMidnight,
            DurationInHours = o.DurationInHours,
            StandardDayHours = o.StandardDayHours,
            RateRuleId = o.RateRuleId,
            RateRuleCodeApplied = o.RateRuleCodeApplied,
            RateRuleNameApplied = o.RateRuleNameApplied,
            RateMultiplierApplied = o.RateMultiplierApplied,
            MultiplierCalculationDetails = o.MultiplierCalculationDetails,
            SplitBatchId = o.SplitBatchId,
            SplitSequence = o.SplitSequence,
            SplitTotalSegments = o.SplitTotalSegments,
            Status = o.Status,
            StatusDescription = o.Status.ToString(),
            EmployeeComment = o.EmployeeComment,
            ManagerComment = o.ManagerComment,
            ApprovedBy = o.ApprovedBy,
            ApprovedByName = approvedByName,
            ApprovedAt = o.ApprovedAt?.DateTime,
            IsProcessedInPayroll = o.IsProcessedInPayroll,
            PayrollBatchId = o.PayrollBatchId,
            ProcessedInPayrollAt = o.ProcessedInPayrollAt?.DateTime,
            CreatedAt = o.CreatedAt.DateTime,
        };
}
