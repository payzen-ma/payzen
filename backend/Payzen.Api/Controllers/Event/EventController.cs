using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payzen.Infrastructure.Persistence;

namespace Payzen.Api.Controllers.Event;

/// <summary>
/// Journal des événements company et employee.
/// Lecture directe du DbContext — les EventLogServices sont write-only (append-only log).
/// </summary>
[ApiController]
[Route("api/events")]
[Authorize]
public class EventController : ControllerBase
    {
    private readonly AppDbContext _db;
    public EventController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] int? companyId,
        [FromQuery] int? employeeId,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
        {
        if (companyId.HasValue)
            {
            var events = await _db.CompanyEventLogs
                .AsNoTracking()
                .Where(e => e.companyId == companyId && e.DeletedAt == null)
                .OrderByDescending(e => e.CreatedAt)
                .Take(Math.Min(limit, 200))
                .Select(e => new { e.Id, e.companyId, e.eventName, e.oldValue, e.newValue, e.CreatedAt })
                .ToListAsync(ct);
            return Ok(events);
            }
        if (employeeId.HasValue)
            {
            var events = await _db.EmployeeEventLogs
                .AsNoTracking()
                .Where(e => e.employeeId == employeeId && e.DeletedAt == null)
                .OrderByDescending(e => e.CreatedAt)
                .Take(Math.Min(limit, 200))
                .Select(e => new { e.Id, e.employeeId, e.eventName, e.oldValue, e.newValue, e.CreatedAt })
                .ToListAsync(ct);
            return Ok(events);
            }
        var allCompany = await _db.CompanyEventLogs.AsNoTracking().Where(e => e.DeletedAt == null).OrderByDescending(e => e.CreatedAt).Take(Math.Min(limit, 100)).Select(e => new { e.Id, e.companyId, e.eventName, e.CreatedAt }).ToListAsync(ct);
        return Ok(allCompany);
        }

    [HttpGet("company/{companyId:int}")]
    public async Task<ActionResult> GetCompanyEvents(
        int companyId,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
        {
        var events = await _db.CompanyEventLogs
            .AsNoTracking()
            .Where(e => e.companyId == companyId && e.DeletedAt == null)
            .OrderByDescending(e => e.CreatedAt)
            .Take(Math.Min(limit, 200))
            .Select(e => new
                {
                e.Id,
                e.companyId,
                e.eventName,
                e.oldValue,
                e.oldValueId,
                e.newValue,
                e.newValueId,
                e.CreatedAt,
                e.CreatedBy
                })
            .ToListAsync(ct);

        return Ok(events);
        }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult> GetEmployeeEvents(
        int employeeId,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
        {
        var events = await _db.EmployeeEventLogs
            .AsNoTracking()
            .Where(e => e.employeeId == employeeId && e.DeletedAt == null)
            .OrderByDescending(e => e.CreatedAt)
            .Take(Math.Min(limit, 200))
            .Select(e => new
                {
                e.Id,
                e.employeeId,
                e.eventName,
                e.oldValue,
                e.oldValueId,
                e.newValue,
                e.newValueId,
                e.CreatedAt,
                e.CreatedBy
                })
            .ToListAsync(ct);

        return Ok(events);
        }
    }
