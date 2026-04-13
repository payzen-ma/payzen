using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.Interfaces;
using Payzen.Api.Extensions;

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

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TimesheetImportResultDto>> ImportTimesheet(
        IFormFile file,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] string mode = "monthly",
        [FromQuery] int? half = null,
        [FromQuery] int? companyId = null,
        CancellationToken ct = default)
        {
        if (file == null || file.Length == 0)
            return BadRequest(new
                {
                Message = "Aucun fichier fourni."
                });
        if (month < 1 || month > 12)
            return BadRequest(new
                {
                Message = "Le mois doit être compris entre 1 et 12."
                });
        if (year < 2020 || year > 2100)
            return BadRequest(new
                {
                Message = "Année invalide."
                });

        mode = (mode ?? "monthly").Trim().ToLowerInvariant();
        if (mode != "monthly" && mode != "bi_monthly")
            return BadRequest(new
                {
                Message = "Le mode doit être 'monthly' ou 'bi_monthly'."
                });
        if (mode == "bi_monthly" && (half == null || half != 1 && half != 2))
            return BadRequest(new
                {
                Message = "Le paramètre 'half' doit être 1 ou 2 pour le mode 'bi_monthly'."
                });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".csv")
            return BadRequest(new
                {
                Message = "Le fichier doit être au format XLSX ou CSV."
                });

        int? userId = null;
        try
            {
            userId = User.GetUserId();
            }
        catch { }

        await using var stream = file.OpenReadStream();
        var r = await _svc.ImportFromFileAsync(stream, file.FileName, month, year, mode, half, companyId, userId, ct);

        if (!r.Success)
            {
            if (r.Error?.Contains("Société non trouvée") == true)
                return NotFound(new
                    {
                    Message = r.Error
                    });
            return BadRequest(new
                {
                Message = r.Error
                });
            }
        return Ok(r.Data);
        }

    [HttpGet]
    public async Task<ActionResult> GetTimesheets(
        [FromQuery] int month, [FromQuery] int year,
        [FromQuery] int? companyId = null, CancellationToken ct = default)
        {
        if (month < 1 || month > 12)
            return BadRequest(new
                {
                Message = "Mois invalide."
                });
        if (year < 2020 || year > 2100)
            return BadRequest(new
                {
                Message = "Année invalide."
                });

        int? userId = null;
        try
            {
            userId = User.GetUserId();
            }
        catch { }

        var r = await _svc.GetTimesheetsAsync(month, year, companyId, userId, ct);
        if (!r.Success)
            return BadRequest(new
                {
                Message = r.Error
                });
        return Ok(r.Data);
        }
    }
