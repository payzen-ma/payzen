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

    public CnssPreetabliController(ICnssPreetabliService svc) => _svc = svc;

    [HttpPost("parse")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Parse([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file == null)
            return BadRequest(new { message = "Aucun fichier fourni." });

        var result = await _svc.ParseAsync(file, ct);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { message = result.Error });
    }
}
