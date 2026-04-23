using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Api.Extensions;
using Payzen.Application.DTOs.Import;
using Payzen.Application.Interfaces;

namespace Payzen.Api.Controllers.Import;

[Route("api/import/module")]
[ApiController]
[Authorize]
public class ModuleImportController : ControllerBase
{
    private readonly IModuleImportService _moduleImportService;
    private readonly IImportTemplateService _importTemplateService;

    public ModuleImportController(
        IModuleImportService moduleImportService,
        IImportTemplateService importTemplateService
    )
    {
        _moduleImportService = moduleImportService;
        _importTemplateService = importTemplateService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ModuleImportResultDto>> ImportWorkbook(
        IFormFile file,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] string mode = "monthly",
        [FromQuery] int? half = null,
        [FromQuery] int? companyId = null,
        [FromQuery] bool sendWelcomeEmail = false,
        CancellationToken ct = default
    )
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "Aucun fichier fourni." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".xlsx")
            return BadRequest(new { Message = "Le fichier doit être au format Excel .xlsx." });

        int? userId = null;
        try
        {
            userId = User.GetUserId();
        }
        catch
        {
            return BadRequest(new { Message = "L'utilisateur n'est pas authentifié." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _moduleImportService.ImportWorkbookAsync(
            stream,
            file.FileName,
            month,
            year,
            mode,
            half,
            companyId,
            userId,
            sendWelcomeEmail,
            ct
        );

        if (!result.Success)
            return BadRequest(new { Message = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("template")]
    public async Task<ActionResult> DownloadNewEmployeeTemplate(
        [FromQuery] int? companyId = null,
        CancellationToken ct = default
    )
    {
        int? userId = null;
        try
        {
            userId = User.GetUserId();
        }
        catch
        {
            return BadRequest(new { Message = "L'utilisateur n'est pas authentifié." });
        }

        var result = await _importTemplateService.GenerateNewEmployeeTemplateAsync(companyId, userId, ct);
        if (!result.Success || result.Data.Content.Length == 0)
            return BadRequest(new { Message = result.Error ?? "Impossible de générer le template." });

        return File(
            result.Data.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            result.Data.FileName
        );
    }
}
