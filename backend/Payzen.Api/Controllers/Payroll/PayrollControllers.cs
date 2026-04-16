using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;
using Payzen.Api.Extensions;

namespace Payzen.Api.Controllers.Payroll;

// ── Payroll ───────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IPayrollService _svc;
    public PayrollController(IPayrollService svc) => _svc = svc;

    private static int? NormalizePayHalf(int? half)
    {
        // Convention front : 0 = mensuel, 1 = 1-15, 2 = 16-31.
        return half switch
        {
            null => null,
            0 => null,
            1 => 1,
            2 => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(half), "half doit être null, 0, 1 ou 2.")
        };
    }

    private static bool TryParseResultStatus(string? rawStatus, out Domain.Enums.PayrollResultStatus status)
    {
        status = Domain.Enums.PayrollResultStatus.Pending;
        if (string.IsNullOrWhiteSpace(rawStatus)) return false;

        var raw = rawStatus.Trim().ToUpperInvariant();
        switch (raw)
        {
            case "SUCCESS":
            case "OK":
                status = Domain.Enums.PayrollResultStatus.OK;
                return true;
            case "ERROR":
            case "ERREUR":
            case "FAILED":
                status = Domain.Enums.PayrollResultStatus.Error;
                return true;
            case "PENDING":
            case "EN_ATTENTE":
            case "EN ATTENTE":
                status = Domain.Enums.PayrollResultStatus.Pending;
                return true;
            case "APPROVED":
            case "APPROUVEE":
            case "APPROUVÉE":
                status = Domain.Enums.PayrollResultStatus.Approved;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Calcul paie : soit corps JSON (un employé, parité simulate/calculate interne),
    /// soit paramètres query <c>companyId</c>, <c>month</c>, <c>year</c> comme l’ancien backend (page bulletin).
    /// </summary>
    [HttpPost("calculate")]
    public async Task<ActionResult> Calculate(
        [FromBody] PayrollSimulateRequestDto? dto,
        [FromQuery] int? companyId,
        [FromQuery] int? month,
        [FromQuery] int? year,
        [FromQuery] int? half,
        [FromQuery] bool useNativeEngine = true)
    {
        int? payHalf;
        try { payHalf = NormalizePayHalf(half); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(new { error = ex.Message }); }

        if (companyId is > 0 && month is >= 1 and <= 12 && year is >= 2020)
        {
            var batchDto = new PayrollBatchRequestDto
            {
                CompanyId = companyId.Value,
                PayMonth  = month.Value,
                PayYear   = year.Value,
                PayHalf   = payHalf
            };
            var r = await _svc.BatchCalculateAsync(batchDto, User.GetUserId());
            if (!r.Success)
                return BadRequest(new { error = r.Error });
            return Ok(new
            {
                message   = $"Calcul de paie terminé pour l'entreprise {companyId}, {month}/{year}.",
                engine    = useNativeEngine ? "Moteur natif C#" : "LLM (Gemini)",
                companyId = companyId.Value,
                month     = month.Value,
                year      = year.Value
            });
        }

        if (dto == null)
            return BadRequest(new { error = "Requête invalide : indiquez companyId, month et year en query, ou un corps JSON avec employeeId, payMonth et payYear." });

        dto.PayHalf = payHalf;
        var calc = await _svc.CalculateAsync(dto, User.GetUserId());
        return calc.Success ? Ok(calc.Data) : BadRequest(new { Message = calc.Error });
    }

    [HttpPost("simulate")]
    public async Task<ActionResult> Simulate([FromBody] PayrollSimulateRequestDto dto)
    {
        var r = await _svc.SimulateAsync(dto);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("batch")]
    public async Task<ActionResult> Batch([FromBody] PayrollBatchRequestDto dto)
    {
        var r = await _svc.BatchCalculateAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetResultByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    /// <summary>Parité ancien backend : GET api/payroll/results?month=&year=&companyId=&status=</summary>
    [HttpGet("results")]
    public async Task<ActionResult> GetResults(
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int? companyId,
        [FromQuery] int? half,
        [FromQuery] string? status)
    {
        int? payHalf;
        try { payHalf = NormalizePayHalf(half); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(new { error = ex.Message }); }

        var r = await _svc.GetResultsAsync(
            companyId is > 0 ? companyId : null,
            month,
            year,
            payHalf,
            status);
        return r.Success ? Ok(r.Data) : BadRequest(new { error = r.Error });
    }

    /// <summary>Parité frontend bulletin : détail d’un bulletin (id = PayrollResult.Id). Ne pas confondre avec une société.</summary>
    [HttpGet("results/{id:int}")]
    public async Task<ActionResult> GetBulletinDetail(int id)
    {
        var r = await _svc.GetBulletinDetailAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { error = r.Error });
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetStats([FromQuery] int companyId, [FromQuery] int year, [FromQuery] int month)
    {
        var r = await _svc.GetStatsAsync(companyId, year, month);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("recalculate/{employeeId:int}")]
    public async Task<ActionResult> Recalculate(int employeeId,
        [FromQuery] int month, [FromQuery] int year, [FromQuery] int? half, [FromQuery] bool useNativeEngine = true)
    {
        _ = useNativeEngine;
        int? payHalf;
        try { payHalf = NormalizePayHalf(half); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(new { error = ex.Message }); }

        var r = await _svc.RecalculateForEmployeeAsync(employeeId, month, year, payHalf, User.GetUserId());
        if (!r.Success)
            return BadRequest(new { error = r.Error });
        return Ok(new
        {
            message      = "Recalcul terminé avec succès.",
            employeeId,
            month,
            year,
            status       = r.Data!.Status.ToString(),
            errorMessage = r.Data.ErrorMessage,
            resultId     = r.Data.Id
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteResultAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }

    /// <summary>Parité ancien backend : DELETE api/payroll/results/{id}</summary>
    [HttpDelete("results/{id:int}")]
    public async Task<ActionResult> DeleteResult(int id)
    {
        var r = await _svc.DeleteResultAsync(id, User.GetUserId());
        return r.Success ? Ok(new { message = "Résultat de paie supprimé avec succès." }) : BadRequest(new { error = r.Error });
    }

    [HttpPatch("results/{id:int}/status")]
    public async Task<ActionResult> UpdateResultStatus(int id, [FromBody] PayrollUpdateStatusRequestDto dto)
    {
        if (!TryParseResultStatus(dto?.Status, out var status))
            return BadRequest(new { error = "Statut invalide. Valeurs attendues: PENDING, SUCCESS, ERROR, APPROVED." });

        var r = await _svc.UpdateResultStatusAsync(id, status, User.GetUserId());
        return r.Success ? Ok(new { message = "Statut du bulletin mis à jour avec succès." }) : BadRequest(new { error = r.Error });
    }

    // Alias de compatibilité: certains fronts appellent /api/payroll/{id}/status
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> UpdateResultStatusCompat(int id, [FromBody] PayrollUpdateStatusRequestDto dto)
    {
        return await UpdateResultStatus(id, dto);
    }

    [HttpGet("custom-rules")]
    public async Task<ActionResult> GetCustomRules([FromQuery] int companyId)
    {
        if (companyId <= 0)
            return BadRequest(new { error = "companyId est requis." });

        var r = await _svc.GetCustomRulesAsync(companyId);
        return r.Success ? Ok(r.Data) : BadRequest(new { error = r.Error });
    }

    [HttpPost("custom-rules/preview")]
    public async Task<ActionResult> PreviewCustomRule([FromBody] CreatePayrollCustomRuleRequestDto dto)
    {
        var r = await _svc.PreviewCustomRuleAsync(dto);
        return r.Success ? Ok(new { dslSnippet = r.Data }) : BadRequest(new { error = r.Error });
    }

    [HttpPost("custom-rules")]
    public async Task<ActionResult> CreateCustomRule([FromQuery] int companyId, [FromBody] CreatePayrollCustomRuleRequestDto dto)
    {
        if (companyId <= 0)
            return BadRequest(new { error = "companyId est requis." });

        var r = await _svc.CreateCustomRuleAsync(companyId, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { error = r.Error });
    }

    [HttpDelete("custom-rules/{id:int}")]
    public async Task<ActionResult> DeleteCustomRule(int id)
    {
        var r = await _svc.DeleteCustomRuleAsync(id, User.GetUserId());
        return r.Success ? Ok(new { message = "Règle personnalisée supprimée avec succès." }) : BadRequest(new { error = r.Error });
    }

    /// <summary>Verrouille les bulletins d'une période pour interdire tout futur recalcul</summary>
    [HttpPost("approve")]
    public async Task<ActionResult> Approve(
        [FromQuery] int companyId,
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int? half)
    {
        int? payHalf;
        try { payHalf = NormalizePayHalf(half); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(new { error = ex.Message }); }

        var r = await _svc.ApprovePeriodAsync(companyId, month, year, payHalf, User.GetUserId());
        return r.Success ? Ok(new { message = "La période a été verrouillée avec succès." }) : BadRequest(new { error = r.Error });
    }
}

// ── Salary Preview ────────────────────────────────────────────────────────────

[ApiController]
[Route("api/salary-preview")]
[Authorize]
public class SalaryPreviewController : ControllerBase
{
    private readonly ISalaryPackageService _svc;
    public SalaryPreviewController(ISalaryPackageService svc) => _svc = svc;

    [HttpPost]
    public async Task<ActionResult> Preview([FromBody] SalaryPreviewRequestDto dto)
    {
        var r = await _svc.PreviewAsync(dto);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("seniority-bonus")]
    public ActionResult GetSeniorityBonus() => Ok(new { description = "Prime d'ancienneté : 5% (2-5 ans), 10% (5-12 ans), 15% (12-20 ans), 20% (20+ ans)", ruleVersion = "MA_2025" });

    [HttpGet("cnss")]
    public ActionResult GetCnss() => Ok(new { description = "Cotisation CNSS employé/employeur selon plafond", ruleVersion = "MA_2025" });

    [HttpGet("ir")]
    public ActionResult GetIr() => Ok(new { description = "Barème IR progressif (tranches)", ruleVersion = "MA_2025" });

    [HttpGet("professional-expenses")]
    public ActionResult GetProfessionalExpenses() => Ok(new { description = "Frais professionnels déductibles (transport, panier)", ruleVersion = "MA_2025" });
}

// ── Payroll Export ────────────────────────────────────────────────────────────

[ApiController]
[Route("api/payroll-export")]
[Authorize]
public class PayrollExportController : ControllerBase
{
    private readonly IPayrollExportService _svc;
    public PayrollExportController(IPayrollExportService svc) => _svc = svc;

    [HttpGet("journal/{companyId:int}")]
    public async Task<ActionResult> JournalPaie(int companyId,
        [FromQuery] int year, [FromQuery] int month)
    {
        var r = await _svc.GetJournalPaieAsync(companyId, year, month);
        if (!r.Success) return BadRequest(new { Message = r.Error });
        return Ok(r.Data);
    }

    [HttpGet("etat-cnss/{companyId:int}")]
    public async Task<ActionResult> EtatCnss(int companyId,
        [FromQuery] int year, [FromQuery] int month)
    {
        var r = await _svc.GetEtatCnssAsync(companyId, year, month);
        if (!r.Success) return BadRequest(new { Message = r.Error });
        return Ok(r.Data);
    }

    [HttpGet("etat-ir/{companyId:int}")]
    public async Task<ActionResult> EtatIr(int companyId,
        [FromQuery] int year, [FromQuery] int month)
    {
        var r = await _svc.GetEtatIrAsync(companyId, year, month);
        if (!r.Success) return BadRequest(new { Message = r.Error });
        return Ok(r.Data);
    }
}

// ── Payslip PDF ───────────────────────────────────────────────────────────────

[ApiController]
[Route("api/payslip")]
[Authorize]
public class PayslipController : ControllerBase
{
    private readonly IDocumentService _svc;
    public PayslipController(IDocumentService svc) => _svc = svc;

    [HttpGet("{payrollResultId:int}")]
    public async Task<ActionResult> Generate(int payrollResultId)
    {
        var r = await _svc.GeneratePayslipAsync(payrollResultId);
        if (!r.Success) return BadRequest(new { Message = r.Error });
        return File(r.Data!, "application/pdf", $"bulletin-paie-{payrollResultId}.pdf");
    }

    [HttpGet("employee/{employeeId:int}/period/{year:int}/{month:int}")]
    public async Task<ActionResult> GenerateByEmployeePeriod(
        int employeeId, int year, int month,
        [FromQuery] int? half)
    {
        var r = await _svc.GeneratePayslipByEmployeePeriodAsync(employeeId, year, month, half);
        if (!r.Success) return BadRequest(new { Message = r.Error });
        return File(r.Data!, "application/pdf", $"bulletin-paie-{employeeId}-{year}-{month:D2}.pdf");
    }

    [HttpGet("cnss-pdf/{companyId:int}")]
    public async Task<ActionResult> CnssPdf(int companyId,
        [FromQuery] int year, [FromQuery] int month)
    {
        var r = await _svc.GenerateEtatCnssPdfAsync(companyId, year, month);
        if (!r.Success) return BadRequest(new { Message = r.Error });
        return File(r.Data!, "application/pdf", $"etat-cnss-{year}-{month:D2}.pdf");
    }

    [HttpGet("ir-pdf/{companyId:int}")]
    public async Task<ActionResult> IrPdf(int companyId,
        [FromQuery] int year, [FromQuery] int month)
    {
        var r = await _svc.GenerateEtatIrPdfAsync(companyId, year, month);
        if (!r.Success) return BadRequest(new { Message = r.Error });
        return File(r.Data!, "application/pdf", $"etat-ir-{year}-{month:D2}.pdf");
    }

    [HttpGet("journal-csv/{companyId:int}")]
    public async Task<ActionResult> JournalCsv(int companyId,
        [FromQuery] int year, [FromQuery] int month)
    {
        var r = await _svc.GenerateJournalPaieCsvAsync(companyId, year, month);
        if (!r.Success) return BadRequest(new { Message = r.Error });
        return File(r.Data!, "text/csv", $"journal-paie-{year}-{month:D2}.csv");
    }
}

/// <summary>
/// Même contrat que l’ancien <c>payzen_backend</c> : <c>GET api/payroll/exports/journal/{companyId}/{year}/{month}</c>, etc.
/// (le monolith expose aussi <c>api/payroll-export</c> et certains fichiers sous <c>api/payslip</c>).
/// </summary>
[ApiController]
[Route("api/payroll/exports")]
[Authorize]
public class PayrollExportsCompatController : ControllerBase
{
    private readonly IPayrollExportService _export;
    private readonly IDocumentService    _documents;

    public PayrollExportsCompatController(IPayrollExportService export, IDocumentService documents)
    {
        _export    = export;
        _documents = documents;
    }

    private static bool ValidatePeriod(int companyId, int year, int month, out string? error)
    {
        error = null;
        if (companyId <= 0) { error = "companyId invalide."; return false; }
        if (month is < 1 or > 12) { error = "month invalide (1-12)."; return false; }
        if (year is < 2000 or > 2100) { error = "year invalide."; return false; }
        return true;
    }

    [HttpGet("journal/{companyId:int}/{year:int}/{month:int}")]
    public async Task<IActionResult> DownloadJournal(int companyId, int year, int month, CancellationToken ct)
    {
        if (!ValidatePeriod(companyId, year, month, out var err))
            return BadRequest(new { message = err });

        var rows = await _export.GetJournalPaieAsync(companyId, year, month, ct);
        if (!rows.Success || rows.Data is not { Count: > 0 })
            return NotFound(new { message = $"Aucun bulletin validé pour {month:D2}/{year}." });

        var file = await _documents.GenerateJournalPaieCsvAsync(companyId, year, month, ct);
        if (!file.Success)
            return BadRequest(new { message = file.Error });

        return File(file.Data!, "text/csv; charset=utf-8", $"JournalPaie_{year}_{month:D2}.csv");
    }

    [HttpGet("cnss/{companyId:int}/{year:int}/{month:int}")]
    public async Task<IActionResult> DownloadCnss(int companyId, int year, int month, CancellationToken ct)
    {
        if (!ValidatePeriod(companyId, year, month, out var err))
            return BadRequest(new { message = err });

        var rows = await _export.GetEtatCnssAsync(companyId, year, month, ct);
        if (!rows.Success || rows.Data is not { Count: > 0 })
            return NotFound(new { message = $"Aucun salarié CNSS trouvé pour {month:D2}/{year}." });

        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var sb = new StringBuilder();
        sb.AppendLine("Nom et Prénom;Numéro CNSS;Salaire Brut Déclaré;Nombre de Jours");
        foreach (var r in rows.Data)
        {
            sb.Append(r.NomPrenom).Append(';')
                .Append(r.NumeroCnss).Append(';')
                .Append(r.SalaireBrutDeclare.ToString("F2", fr)).Append(';')
                .Append(r.NombreJoursDeclare)
                .AppendLine();
        }

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"EtatCnss_{year}_{month:D2}.csv");
    }

    [HttpGet("cnss-pdf/{companyId:int}/{year:int}/{month:int}")]
    public async Task<IActionResult> DownloadCnssPdf(int companyId, int year, int month, CancellationToken ct)
    {
        if (!ValidatePeriod(companyId, year, month, out var err))
            return BadRequest(new { message = err });

        var r = await _documents.GenerateEtatCnssPdfAsync(companyId, year, month, ct);
        if (!r.Success)
            return NotFound(new { message = r.Error });

        return File(r.Data!, "application/pdf", $"EtatCnss_{year}_{month:D2}.pdf");
    }

    [HttpGet("ir/{companyId:int}/{year:int}/{month:int}")]
    public async Task<IActionResult> DownloadIr(int companyId, int year, int month, CancellationToken ct)
    {
        if (!ValidatePeriod(companyId, year, month, out var err))
            return BadRequest(new { message = err });

        var rows = await _export.GetEtatIrAsync(companyId, year, month, ct);
        if (!rows.Success || rows.Data is not { Count: > 0 })
            return NotFound(new { message = $"Aucun bulletin validé pour {month:D2}/{year}." });

        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var sb = new StringBuilder();
        sb.AppendLine("Nom & Prénom;CIN;N° CNSS;Brut Imposable;IR Retenu");
        foreach (var r in rows.Data)
        {
            sb.Append(r.NomPrenom).Append(';')
                .Append(r.CIN).Append(';')
                .Append(r.CNSS).Append(';')
                .Append(r.BrutImposable.ToString("F2", fr)).Append(';')
                .Append(r.IRRetenu.ToString("F2", fr))
                .AppendLine();
        }

        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", $"EtatIR_{year}_{month:D2}.csv");
    }

    [HttpGet("ir-pdf/{companyId:int}/{year:int}/{month:int}")]
    public async Task<IActionResult> DownloadIrPdf(int companyId, int year, int month, CancellationToken ct)
    {
        if (!ValidatePeriod(companyId, year, month, out var err))
            return BadRequest(new { message = err });

        var r = await _documents.GenerateEtatIrPdfAsync(companyId, year, month, ct);
        if (!r.Success)
            return NotFound(new { message = r.Error });

        return File(r.Data!, "application/pdf", $"EtatIR_{year}_{month:D2}.pdf");
    }
}


// ── Claude Simulation ─────────────────────────────────────────────────────────

[ApiController]
[Route("api/payroll/claude-simulation")]
[Authorize]
public class ClaudeSimulationController : ControllerBase
{
    private readonly ILlmService _llm;
    private readonly IPayrollService _payroll;

    public ClaudeSimulationController(ILlmService llm, IPayrollService payroll)
    {
        _llm     = llm;
        _payroll = payroll;
    }

    [HttpPost]
    public async Task<ActionResult> Simulate([FromBody] PayrollSimulateRequestDto dto,
        [FromQuery] string? instruction)
    {
        var payrollResult = await _payroll.SimulateAsync(dto);
        if (!payrollResult.Success) return BadRequest(new { Message = payrollResult.Error });

        var data    = payrollResult.Data!;
        var rules   = await _llm.GetRulesAsync();
        var prompt  = instruction ?? "Analyse ce bulletin et justifie le résultat ligne par ligne.";
        var userMsg = $"{prompt}\n\nDonnées du bulletin (JSON):\n{System.Text.Json.JsonSerializer.Serialize(data)}";
        var analyse = await _llm.SimulationSalaryAsync(
            rules,
            userMsg);

        return Ok(new { simulation = data, analysis = analyse });
    }

    [HttpPost("simulate-quick")]
    public async Task<ActionResult> SimulateQuick([FromBody] PayrollSimulateRequestDto dto, [FromQuery] string? instruction)
    {
        var payrollResult = await _payroll.SimulateAsync(dto);
        if (!payrollResult.Success) return BadRequest(new { Message = payrollResult.Error });
        var rules = await _llm.GetRulesAsync();
        var json = System.Text.Json.JsonSerializer.Serialize(payrollResult.Data);
        var userMsg = $"{instruction ?? "Résumé court et justification."}\n\nDonnées du bulletin (JSON):\n{json}";
        var analysis = await _llm.SimulateQuickAsync(rules, userMsg);
        return Ok(new { simulation = payrollResult.Data, analysis });
    }

    [HttpPost("simulate-stream")]
    public async Task<ActionResult> SimulateStream([FromBody] PayrollSimulateRequestDto dto, [FromQuery] string? instruction)
    {
        var payrollResult = await _payroll.SimulateAsync(dto);
        if (!payrollResult.Success) return BadRequest(new { Message = payrollResult.Error });
        var rules = await _llm.GetRulesAsync();
        var json = System.Text.Json.JsonSerializer.Serialize(payrollResult.Data);
        var userMsg = $"{instruction ?? "Analyse."}\n\nDonnées du bulletin (JSON):\n{json}";
        var analysis = await _llm.SimulationSalaryStreamAsync(rules, userMsg);
        return Ok(new { simulation = payrollResult.Data, analysis });
    }

    [HttpGet("rules")]
    public async Task<ActionResult> GetRules()
    {
        var rules = await _llm.GetRulesAsync();
        return Ok(new { rules });
    }
}
