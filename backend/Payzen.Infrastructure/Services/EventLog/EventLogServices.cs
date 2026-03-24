using Microsoft.EntityFrameworkCore;
using Payzen.Application.Interfaces;
using Payzen.Domain.Entities.Events;
using Payzen.Domain.Entities.Leave;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Infrastructure.Services.EventLog;

public class CompanyEventLogService : ICompanyEventLogService
{
    private readonly AppDbContext _db;
    public CompanyEventLogService(AppDbContext db) => _db = db;

    public async Task LogEventAsync(int companyId, string eventName, string? oldValue,
        int? oldValueId, string? newValue, int? newValueId, int createdBy,
        CancellationToken ct = default)
    {
        _db.CompanyEventLogs.Add(new CompanyEventLog
        {
            companyId   = companyId,
            eventName   = eventName,
            oldValue    = oldValue,
            oldValueId  = oldValueId,
            newValue    = newValue,
            newValueId  = newValueId,
            CreatedAt   = DateTimeOffset.UtcNow,
            CreatedBy   = createdBy
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task LogSimpleEventAsync(int companyId, string eventName,
        string? oldValue, string? newValue, int createdBy, CancellationToken ct = default)
        => await LogEventAsync(companyId, eventName, oldValue, null, newValue, null, createdBy, ct);
}

public class EmployeeEventLogService : IEmployeeEventLogService
{
    private readonly AppDbContext _db;
    public EmployeeEventLogService(AppDbContext db) => _db = db;

    public async Task LogEventAsync(int employeeId, string eventName, string? oldValue,
        int? oldValueId, string? newValue, int? newValueId, int createdBy,
        CancellationToken ct = default)
    {
        _db.EmployeeEventLogs.Add(new EmployeeEventLog
        {
            employeeId  = employeeId,
            eventName   = eventName,
            oldValue    = oldValue,
            oldValueId  = oldValueId,
            newValue    = newValue,
            newValueId  = newValueId,
            CreatedAt   = DateTimeOffset.UtcNow,
            CreatedBy   = createdBy
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task LogSimpleEventAsync(int employeeId, string eventName,
        string? oldValue, string? newValue, int createdBy, CancellationToken ct = default)
        => await LogEventAsync(employeeId, eventName, oldValue, null, newValue, null, createdBy, ct);
}

public class LeaveEventLogService : ILeaveEventLogService
{
    private readonly AppDbContext _db;
    public LeaveEventLogService(AppDbContext db) => _db = db;

    public async Task LogEventAsync(int companyId, int? employeeId, int? leaveRequestId,
        string eventName, string? oldValue, string? newValue, int createdBy,
        CancellationToken ct = default)
    {
        _db.LeaveAuditLogs.Add(new LeaveAuditLog
        {
            CompanyId      = companyId,
            EmployeeId     = employeeId,
            LeaveRequestId = leaveRequestId,
            EventName      = eventName,
            OldValue       = oldValue,
            NewValue       = newValue,
            CreatedAt      = DateTimeOffset.UtcNow,
            CreatedBy      = createdBy
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task LogLeaveRequestEventAsync(int companyId, int? employeeId,
        int leaveRequestId, string eventName, string? oldValue, string? newValue,
        int createdBy, CancellationToken ct = default)
        => await LogEventAsync(companyId, employeeId, leaveRequestId,
            eventName, oldValue, newValue, createdBy, ct);
}
