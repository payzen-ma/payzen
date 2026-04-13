using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Dashboard;
using Payzen.Application.Interfaces;

namespace Payzen.Api.Controllers.Dashboard;

// ── Dashboard (employés client) — GET api/dashboard/employees ─────────────────

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
    {
    private readonly IDashboardService _svc;
    public DashboardController(IDashboardService svc) => _svc = svc;

    [HttpGet("employees")]
    public async Task<ActionResult> GetEmployees()
        {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
        if (userIdClaim == null)
            return Unauthorized(new
                {
                Message = "Utilisateur non authentifié"
                });

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return BadRequest(new
                {
                Message = "ID utilisateur invalide"
                });

        var r = await _svc.GetEmployeesDashboardAsync(userId);
        if (!r.Success)
            {
            if (r.Error == "Utilisateur non trouvé")
                return NotFound(new
                    {
                    Message = r.Error
                    });
            if (r.Error == "L'utilisateur n'est pas associé à un employé")
                return BadRequest(new
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
    }

// ── Dashboard HR — aligné sur payzen_backend.Controllers.Dashboard.DashboardHrController

[ApiController]
[Route("api/dashboard/hr")]
[Authorize]
public class DashboardHrController : ControllerBase
    {
    private readonly IDashboardService _dashboardHrService;

    public DashboardHrController(IDashboardService dashboardHrService)
        {
        _dashboardHrService = dashboardHrService;
        }

    /// <summary>
    /// Raw payload for client-side filtering and aggregation.
    /// GET /api/dashboard/hr/raw?month=yyyy-MM
    /// </summary>
    [HttpGet("raw")]
    [Produces("application/json")]
    public async Task<ActionResult<DashboardHrRawDto>> GetHrDashboardRaw(
        [FromQuery] string? month = null,
        CancellationToken cancellationToken = default)
        {
        try
            {
            var result = await _dashboardHrService.GetHrDashboardRawAsync(ReadCompanyIdHeader(), month, cancellationToken);
            return Ok(result);
            }
        catch (ArgumentException ex)
            {
            return BadRequest(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (KeyNotFoundException ex)
            {
            return NotFound(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (UnauthorizedAccessException ex)
            {
            return StatusCode(StatusCodes.Status403Forbidden, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (Exception ex)
            {
            return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        }

    /// <summary>
    /// Full payload for the new tabbed HR dashboard.
    /// GET /api/dashboard/hr?month=yyyy-MM
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<DashboardHrDto>> GetHrDashboard(
        [FromQuery] string? month = null,
        CancellationToken cancellationToken = default)
        {
        try
            {
            var result = await _dashboardHrService.GetHrDashboardAsync(ReadCompanyIdHeader(), month, cancellationToken);
            return Ok(result);
            }
        catch (ArgumentException ex)
            {
            return BadRequest(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (KeyNotFoundException ex)
            {
            return NotFound(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (UnauthorizedAccessException ex)
            {
            return StatusCode(StatusCodes.Status403Forbidden, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (Exception ex)
            {
            return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        }

    /// <summary>
    /// Section 1 only: Vue Globale RH.
    /// GET /api/dashboard/hr/global?month=yyyy-MM
    /// </summary>
    [HttpGet("global")]
    [Produces("application/json")]
    public async Task<ActionResult<DashboardHrVueGlobaleDto>> GetVueGlobale(
        [FromQuery] string? month = null,
        CancellationToken cancellationToken = default)
        {
        try
            {
            var result = await _dashboardHrService.GetVueGlobaleAsync(ReadCompanyIdHeader(), month, cancellationToken);
            return Ok(result);
            }
        catch (ArgumentException ex)
            {
            return BadRequest(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (KeyNotFoundException ex)
            {
            return NotFound(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (UnauthorizedAccessException ex)
            {
            return StatusCode(StatusCodes.Status403Forbidden, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (Exception ex)
            {
            return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        }

    /// <summary>
    /// Section 2 only: Mouvements RH.
    /// GET /api/dashboard/hr/movements?month=yyyy-MM
    /// </summary>
    [HttpGet("movements")]
    [Produces("application/json")]
    public async Task<ActionResult<DashboardHrMouvementsDto>> GetMouvementsRh(
        [FromQuery] string? month = null,
        CancellationToken cancellationToken = default)
        {
        try
            {
            var result = await _dashboardHrService.GetMouvementsRhAsync(ReadCompanyIdHeader(), month, cancellationToken);
            return Ok(result);
            }
        catch (ArgumentException ex)
            {
            return BadRequest(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (KeyNotFoundException ex)
            {
            return NotFound(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (UnauthorizedAccessException ex)
            {
            return StatusCode(StatusCodes.Status403Forbidden, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (Exception ex)
            {
            return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        }

    /// <summary>
    /// Section 3 only: Masse Salariale.
    /// GET /api/dashboard/hr/payroll?month=yyyy-MM
    /// </summary>
    [HttpGet("payroll")]
    [Produces("application/json")]
    public async Task<ActionResult<DashboardHrMasseSalarialeDto>> GetMasseSalariale(
        [FromQuery] string? month = null,
        CancellationToken cancellationToken = default)
        {
        try
            {
            var result = await _dashboardHrService.GetMasseSalarialeAsync(ReadCompanyIdHeader(), month, cancellationToken);
            return Ok(result);
            }
        catch (ArgumentException ex)
            {
            return BadRequest(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (KeyNotFoundException ex)
            {
            return NotFound(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (UnauthorizedAccessException ex)
            {
            return StatusCode(StatusCodes.Status403Forbidden, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (Exception ex)
            {
            return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        }

    /// <summary>
    /// Section 4 only: Parité &amp; Diversité.
    /// GET /api/dashboard/hr/parity?month=yyyy-MM
    /// </summary>
    [HttpGet("parity")]
    [Produces("application/json")]
    public async Task<ActionResult<DashboardHrPariteDiversiteDto>> GetPariteDiversite(
        [FromQuery] string? month = null,
        CancellationToken cancellationToken = default)
        {
        try
            {
            var result = await _dashboardHrService.GetPariteDiversiteAsync(ReadCompanyIdHeader(), month, cancellationToken);
            return Ok(result);
            }
        catch (ArgumentException ex)
            {
            return BadRequest(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (KeyNotFoundException ex)
            {
            return NotFound(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (UnauthorizedAccessException ex)
            {
            return StatusCode(StatusCodes.Status403Forbidden, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (Exception ex)
            {
            return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        }

    /// <summary>
    /// Section 5 only: Conformité Sociale.
    /// GET /api/dashboard/hr/compliance?month=yyyy-MM
    /// </summary>
    [HttpGet("compliance")]
    [Produces("application/json")]
    public async Task<ActionResult<DashboardHrConformiteSocialeDto>> GetConformiteSociale(
        [FromQuery] string? month = null,
        CancellationToken cancellationToken = default)
        {
        try
            {
            var result = await _dashboardHrService.GetConformiteSocialeAsync(ReadCompanyIdHeader(), month, cancellationToken);
            return Ok(result);
            }
        catch (ArgumentException ex)
            {
            return BadRequest(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (KeyNotFoundException ex)
            {
            return NotFound(new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (UnauthorizedAccessException ex)
            {
            return StatusCode(StatusCodes.Status403Forbidden, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        catch (Exception ex)
            {
            return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                Message = ex.Message,
                RequestedMonth = month
                });
            }
        }

    private int? ReadCompanyIdHeader()
        {
        if (!Request.Headers.TryGetValue("X-Company-Id", out var values))
            {
            return null;
            }

        return int.TryParse(values.FirstOrDefault(), out var companyId) ? companyId : null;
        }
    }

// ── Dashboard BackOffice ──────────────────────────────────────────────────────

[ApiController]
[Route("api/dashboard/summary")]
[Authorize]
public class DashboardBackOfficeController : ControllerBase
    {
    private readonly IDashboardService _svc;
    public DashboardBackOfficeController(IDashboardService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetSummary()
        {
        var r = await _svc.GetBackofficeSummaryAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            r.Error
            });
        }
    }

// ── Dashboard Expert ──────────────────────────────────────────────────────────

[ApiController]
[Route("api/dashboard/expert")]
[Authorize]
public class DashboardExpertController : ControllerBase
    {
    private readonly IDashboardService _svc;
    public DashboardExpertController(IDashboardService svc) => _svc = svc;

    [HttpGet("{expertCompanyId:int}")]
    public async Task<ActionResult> GetExpert(int expertCompanyId)
        {
        var r = await _svc.GetExpertDashboardAsync(expertCompanyId);
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            r.Error
            });
        }
    }


// ── Dashboard Employee ──────────────────────────────────────────────────────────

[ApiController]
[Route("api/dashboard/employee")]
[Authorize]
public class DashboardEmployeeController : ControllerBase
    {
    private readonly IDashboardService _svc;
    public DashboardEmployeeController(IDashboardService svc) => _svc = svc;
    [HttpGet]
    public async Task<ActionResult> GetEmployeeDashboard()
        {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");

        if (userIdClaim == null)
            return Unauthorized(new
                {
                Message = "Utilisateur non authentifié"
                });

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return BadRequest(new
                {
                Message = "ID utilisateur invalide"
                });

        var r = await _svc.GetEmployeeDashboardAsync(userId);

        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            r.Error
            });
        }
    }

// ── Dashboard CEO ──────────────────────────────────────────────────────────────

[ApiController]
[Route("api/DashboardCeo")]
[Authorize]
public class DashboardCeoController : ControllerBase
    {
    private readonly IDashboardService _svc;
    public DashboardCeoController(IDashboardService svc) => _svc = svc;

    [HttpGet("GetCeoDashboardData")]
    public async Task<ActionResult> GetCeoDashboardData(
        [FromQuery] string? parity = null,
        [FromQuery] string? fromMonth = null,
        [FromQuery] string? toMonth = null,
        CancellationToken ct = default)
        {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
        if (userIdClaim == null)
            return Unauthorized(new
                {
                Message = "Utilisateur non authentifié"
                });

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return BadRequest(new
                {
                Message = "ID utilisateur invalide"
                });

        var r = await _svc.GetCeoDashboardDataAsync(userId, parity, fromMonth, toMonth, ct);
        return r.Success ? Ok(r.Data) : BadRequest(new
            {
            Message = r.Error
            });
        }
    }
