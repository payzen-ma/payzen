using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.Interfaces;

namespace Payzen.Api.Controllers.Payroll;

[ApiController]
[Route("api/cnss/preetabli")]
[Authorize]
public class CnssPreetabliController : ControllerBase
{
    private readonly ICnssPreetabliService _svc;
    private readonly ICnssBdsService _bdsSvc;

    public CnssPreetabliController(ICnssPreetabliService svc, ICnssBdsService bdsSvc)
    {
        _svc = svc;
        _bdsSvc = bdsSvc;
    }

    [HttpPost("parse")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Parse([FromQuery] int companyId, [FromForm] IFormFile file, CancellationToken ct)
    {
        if (file == null)
            return BadRequest(new { message = "Aucun fichier fourni." });

        var result = await _svc.ParseAsync(companyId, file, ct);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { message = result.Error });
    }

    [HttpGet("latest")]
    public async Task<IActionResult> Latest([FromQuery] int companyId, [FromQuery] string? period, CancellationToken ct)
    {
        var result = await _svc.GetLatestAsync(companyId, period, ct);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { message = result.Error });
    }

    [HttpPost("generate-bds")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> GenerateBds(
        [FromQuery] int companyId,
        [FromForm] IFormFile file,
        CancellationToken ct
    )
    {
        if (file == null)
            return BadRequest(new { message = "Aucun fichier préétabli fourni." });

        var result = await _bdsSvc.GeneratePrincipalBdsAsync(companyId, file, ct);
        if (!result.Success || result.Data == null || result.Data.Content.Length == 0)
            return BadRequest(new { message = result.Error ?? "Génération e-BDS impossible.", errors = result.Errors });

        if (result.Data.Warnings.Count > 0)
        {
            Response.Headers.Append("X-CNSS-Warnings-Count", result.Data.Warnings.Count.ToString());
            Response.Headers.Append("X-CNSS-Warnings", string.Join(" | ", result.Data.Warnings.Take(20)));
        }

        return File(result.Data.Content, "text/plain", result.Data.FileName);
    }
}
