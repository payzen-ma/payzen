using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Api.Extensions;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.Interfaces;

namespace Payzen.Api.Controllers.Payroll;

/// <summary>
/// Import de fichiers de pointage XLSX/CSV (format Sage) et lecture des pointages.
/// Délègue toute la logique à ITimesheetImportService (Clean Architecture).
/// </summary>
[Route("api/timesheets")]
[ApiController]
[Authorize]
public class TimesheetImportController : ControllerBase
{
    private readonly ITimesheetImportService _svc;

    public TimesheetImportController(ITimesheetImportService svc)
    {
        _svc = svc;
    }

    [HttpGet]
    public async Task<ActionResult> GetTimesheets(
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int? companyId = null,
        CancellationToken ct = default
    )
    {
        if (month < 1 || month > 12)
            return BadRequest(new { Message = "Mois invalide." });
        if (year < 2020 || year > 2100)
            return BadRequest(new { Message = "Année invalide." });

        int? userId = null;
        try
        {
            userId = User.GetUserId();
        }
        catch { }

        var r = await _svc.GetTimesheetsAsync(month, year, companyId, userId, ct);
        if (!r.Success)
            return BadRequest(new { Message = r.Error });
        return Ok(r.Data);
    }
}
