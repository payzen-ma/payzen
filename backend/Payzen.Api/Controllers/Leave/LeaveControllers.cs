using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Api.Extensions;
using Payzen.Application.DTOs.Leave;
using Payzen.Application.Interfaces;
using Payzen.Domain.Enums;

namespace Payzen.Api.Controllers.Leave;

// ── LeaveTypes ────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/leave-types")]
[Authorize]
public class LeaveTypeController : ControllerBase
{
    private readonly ILeaveService _svc;

    public LeaveTypeController(ILeaveService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? companyId, [FromQuery] bool? includeGlobal)
    {
        _ = includeGlobal;
        var r = await _svc.GetLeaveTypesAsync(companyId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetLeaveTypeByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] LeaveTypeCreateDto dto)
    {
        var r = await _svc.CreateLeaveTypeAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error?.Contains("existe déjà") == true)
                return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] LeaveTypePatchDto dto)
    {
        var r = await _svc.PatchLeaveTypeAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Type de congé introuvable.")
                return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("existe déjà") == true)
                return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] LeaveTypePatchDto dto) => Put(id, dto);

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteLeaveTypeAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Type de congé introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}

// ── LeaveTypePolicies ─────────────────────────────────────────────────────────

[ApiController]
[Route("api/leave-type-policies")]
[Authorize]
public class LeaveTypePolicyController : ControllerBase
{
    private readonly ILeaveService _svc;

    public LeaveTypePolicyController(ILeaveService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? companyId, [FromQuery] int? leaveTypeId)
    {
        var r = await _svc.GetPoliciesAsync(companyId, leaveTypeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("by-leave-type/{leaveTypeId:int}")]
    public async Task<ActionResult> GetByLeaveType(int leaveTypeId)
    {
        var r = await _svc.GetPoliciesAsync(null, leaveTypeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetPolicyByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] LeaveTypePolicyCreateDto dto)
    {
        var r = await _svc.CreatePolicyAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Type de congé non trouvé.")
                return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("existe déjà") == true)
                return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] LeaveTypePolicyPatchDto dto)
    {
        var r = await _svc.PatchPolicyAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Politique introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] LeaveTypePolicyPatchDto dto) => Put(id, dto);

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeletePolicyAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Politique introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}

// ── LeaveTypeLegalRules ───────────────────────────────────────────────────────

[ApiController]
[Route("api/leave-type-legal-rules")]
[Authorize]
public class LeaveTypeLegalRuleController : ControllerBase
{
    private readonly ILeaveService _svc;

    public LeaveTypeLegalRuleController(ILeaveService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? leaveTypeId)
    {
        var r = await _svc.GetLegalRulesAsync(leaveTypeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("by-leave-type/{leaveTypeId:int}")]
    public async Task<ActionResult> GetByLeaveType(int leaveTypeId)
    {
        var r = await _svc.GetLegalRulesAsync(leaveTypeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetLegalRuleByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] LeaveTypeLegalRuleCreateDto dto)
    {
        var r = await _svc.CreateLegalRuleAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Type de congé non trouvé.")
                return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("existe déjà") == true)
                return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] LeaveTypeLegalRulePatchDto dto)
    {
        var r = await _svc.PatchLegalRuleAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Règle légale introuvable.")
                return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("existe déjà") == true)
                return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] LeaveTypeLegalRulePatchDto dto) => Put(id, dto);

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteLegalRuleAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Règle légale introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}

// ── LeaveRequests ─────────────────────────────────────────────────────────────

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestController : ControllerBase
{
    private readonly ILeaveService _svc;

    public LeaveRequestController(ILeaveService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] int? companyId,
        [FromQuery] int? employeeId,
        [FromQuery] LeaveRequestStatus? status
    )
    {
        var r = await _svc.GetLeaveRequestsAsync(companyId, employeeId, status);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult> GetByEmployee(int employeeId)
    {
        var r = await _svc.GetLeaveRequestsByEmployeeIdAsync(employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("pending")]
    public async Task<ActionResult> GetPending([FromQuery] int? companyId)
    {
        var r = await _svc.GetPendingApprovalAsync(companyId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("pending-approval")]
    public async Task<ActionResult> GetPendingApproval([FromQuery] int? companyId)
    {
        var r = await _svc.GetPendingApprovalAsync(companyId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetLeaveRequestByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] LeaveRequestCreateDto dto)
    {
        var r = await _svc.CreateLeaveRequestForSelfAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("create-for-employee/{employeeId:int}")]
    public async Task<ActionResult> CreateForEmployee(int employeeId, [FromBody] LeaveRequestCreateDto dto)
    {
        var r = await _svc.CreateLeaveRequestForOtherEmployeeAsync(employeeId, dto, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] LeaveRequestPatchDto dto)
    {
        var r = await _svc.PutLeaveRequestAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Demande introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] LeaveRequestPatchDto dto)
    {
        var r = await _svc.PatchLeaveRequestAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Demande introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult> Submit(int id)
    {
        var r = await _svc.SubmitLeaveRequestAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Demande introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult> Approve(int id, [FromBody] LeaveRequestDecisionDto? dto = null)
    {
        var comment = dto?.Comment?.Trim() ?? dto?.ApproverNotes?.Trim();
        var r = await _svc.ApproveLeaveRequestAsync(id, comment, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Demande introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult> Reject(int id, [FromBody] LeaveRequestDecisionDto? dto = null)
    {
        var comment = dto?.Comment?.Trim() ?? dto?.ApproverNotes?.Trim();
        var r = await _svc.RejectLeaveRequestAsync(id, comment, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Demande introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult> Cancel(int id, [FromBody] LeaveRequestDecisionDto? dto = null)
    {
        var comment = dto?.Comment?.Trim() ?? dto?.ApproverNotes?.Trim();
        var r = await _svc.CancelLeaveRequestAsync(id, comment, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Demande introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPost("{id:int}/renounce")]
    public async Task<ActionResult> Renounce(int id)
    {
        var r = await _svc.RenounceLeaveRequestAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Demande introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteLeaveRequestAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Demande introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}

// ── LeaveBalances ─────────────────────────────────────────────────────────────

[ApiController]
[Route("api/leave-balances")]
[Authorize]
public class LeaveBalanceController : ControllerBase
{
    private readonly ILeaveService _svc;

    public LeaveBalanceController(ILeaveService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] int? companyId,
        [FromQuery] int? employeeId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int? leaveTypeId
    )
    {
        var r = await _svc.GetBalancesFilteredAsync(companyId, employeeId, year, month, leaveTypeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetBalanceByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("employee/{employeeId:int}/year/{year:int}")]
    public async Task<ActionResult> GetByEmployeeAndYear(int employeeId, int year, [FromQuery] int? month)
    {
        if (month.HasValue)
        {
            var r = await _svc.GetBalancesByYearMonthAsync(employeeId, year, month.Value);
            return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
        }
        else
        {
            var r = await _svc.GetBalancesByYearAsync(employeeId, year);
            return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
        }
    }

    [HttpGet("employee/{employeeId:int}/year/{year:int}/month/{month:int}")]
    public async Task<ActionResult> GetByEmployeeYearMonth(int employeeId, int year, int month)
    {
        var r = await _svc.GetBalancesByYearMonthAsync(employeeId, year, month);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("summary/{employeeId:int}")]
    public async Task<ActionResult> GetSummary(int employeeId, [FromQuery] int? companyId)
    {
        var r = await _svc.GetBalanceSummaryAsync(employeeId, companyId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] LeaveBalanceCreateDto dto)
    {
        var r = await _svc.CreateBalanceAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Employé non trouvé." || r.Error == "Type de congé non trouvé.")
                return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("existe déjà") == true)
                return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] LeaveBalancePatchDto dto)
    {
        var r = await _svc.PatchBalanceAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Solde introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] LeaveBalancePatchDto dto) => Put(id, dto);

    [HttpPost("recalculate/{employeeId:int}/{year:int}/{month:int}")]
    public async Task<ActionResult> Recalculate(
        int employeeId,
        int year,
        int month,
        [FromQuery] int? companyId,
        [FromQuery] int? leaveTypeId
    )
    {
        if (month < 1 || month > 12)
            return BadRequest(new { Message = "Le mois doit être entre 1 et 12." });
        var r = await _svc.RecalculateBalancesForMonthAsync(
            employeeId,
            year,
            month,
            companyId,
            leaveTypeId,
            User.GetUserId()
        );
        if (!r.Success)
        {
            if (r.Error?.Contains("Aucun solde trouvé") == true)
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }

        var d = r.Data!;
        return Ok(
            new
            {
                Message = d.Message,
                RecalculatedCount = d.RecalculatedCount,
                TotalBalances = d.TotalBalances,
            }
        );
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteBalanceAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Solde introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}

// ── LeaveCarryOverAgreements ──────────────────────────────────────────────────

[ApiController]
[Route("api/leave-carryover-agreements")]
[Authorize]
public class LeaveCarryOverAgreementController : ControllerBase
{
    private readonly ILeaveService _svc;

    public LeaveCarryOverAgreementController(ILeaveService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? employeeId, [FromQuery] int? companyId)
    {
        if (!employeeId.HasValue)
            return BadRequest(new { Message = "employeeId est requis." });
        var r = await _svc.GetCarryOverAgreementsAsync(employeeId.Value);
        if (!r.Success)
            return BadRequest(new { Message = r.Error });
        var data = r.Data!.AsQueryable();
        if (companyId.HasValue)
            data = data.Where(a => a.CompanyId == companyId.Value);
        return Ok(data.ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetCarryOverByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult> GetByEmployee(int employeeId)
    {
        var r = await _svc.GetCarryOverAgreementsAsync(employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] LeaveCarryOverAgreementCreateDto dto)
    {
        var r = await _svc.CreateCarryOverAgreementAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Employé non trouvé." || r.Error == "Type de congé non trouvé.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] LeaveCarryOverAgreementPatchDto dto)
    {
        var r = await _svc.PatchCarryOverAgreementAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Accord introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] LeaveCarryOverAgreementPatchDto dto) => Put(id, dto);

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteCarryOverAgreementAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Accord introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}

// ── LeaveRequestAttachments ───────────────────────────────────────────────────

[ApiController]
[Route("api/leave-request-attachments")]
[Authorize]
public class LeaveRequestAttachmentController : ControllerBase
{
    private readonly ILeaveService _svc;
    private readonly IWebHostEnvironment _env;

    public LeaveRequestAttachmentController(ILeaveService svc, IWebHostEnvironment env)
    {
        _svc = svc;
        _env = env;
    }

    [HttpGet("by-request/{leaveRequestId:int}")]
    public async Task<ActionResult> GetByRequest(int leaveRequestId)
    {
        var r = await _svc.GetAttachmentsAsync(leaveRequestId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetAttachmentByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<ActionResult> Upload([FromForm] int leaveRequestId, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "Aucun fichier fourni." });

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(
                new
                {
                    Message = $"Type de fichier non autorisé. Types autorisés: {string.Join(", ", allowedExtensions)}",
                }
            );

        if (file.Length > 10_485_760)
            return BadRequest(new { Message = "Le fichier ne doit pas dépasser 10 MB." });

        var uploadFolder = Path.Combine(_env.ContentRootPath, "uploads", "leave-attachments");
        if (!Directory.Exists(uploadFolder))
            Directory.CreateDirectory(uploadFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var fullPath = Path.Combine(uploadFolder, uniqueFileName);
        var relativePath = $"uploads/leave-attachments/{uniqueFileName}";

        using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream);

        var dto = new LeaveRequestAttachmentCreateDto
        {
            LeaveRequestId = leaveRequestId,
            FileName = Path.GetFileName(file.FileName),
            FilePath = fullPath, // stocker le chemin absolu pour GetAttachmentDownloadAsync
            FileType = file.ContentType,
        };

        var r = await _svc.CreateAttachmentAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
            if (r.Error == "Demande de congé non trouvée.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var r = await _svc.GetAttachmentDownloadAsync(id);
        if (!r.Success)
            return NotFound(new { Message = r.Error });
        var (content, fileName, contentType) = r.Data;
        if (content.Length == 0)
            return NotFound(new { Message = "Fichier physique non trouvé sur le serveur." });
        return File(content, contentType, fileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAttachmentAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Pièce jointe introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}

// ── LeaveRequestExemptions ────────────────────────────────────────────────────

[ApiController]
[Route("api/leave-request-exemptions")]
[Authorize]
public class LeaveRequestExemptionController : ControllerBase
{
    private readonly ILeaveService _svc;

    public LeaveRequestExemptionController(ILeaveService svc) => _svc = svc;

    [HttpGet("by-request/{leaveRequestId:int}")]
    public async Task<ActionResult> GetByRequest(int leaveRequestId)
    {
        var r = await _svc.GetExemptionsAsync(leaveRequestId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetExemptionByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] LeaveRequestExemptionCreateDto dto)
    {
        var r = await _svc.CreateExemptionAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Demande de congé non trouvée.")
                return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("existe déjà") == true)
                return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] LeaveRequestExemptionPatchDto dto)
    {
        var r = await _svc.PatchExemptionAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Exemption introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] LeaveRequestExemptionPatchDto dto) => Put(id, dto);

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteExemptionAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Exemption introuvable.")
                return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}

// ── LeaveAuditLogs ────────────────────────────────────────────────────────────

[ApiController]
[Route("api/leave-audit-logs")]
[Authorize]
public class LeaveAuditLogController : ControllerBase
{
    private readonly ILeaveService _svc;

    public LeaveAuditLogController(ILeaveService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] int? companyId,
        [FromQuery] int? employeeId,
        [FromQuery] int? leaveRequestId
    )
    {
        if (employeeId.HasValue)
        {
            var r = await _svc.GetAuditLogsByEmployeeAsync(employeeId.Value);
            return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
        }
        else
        {
            var r = await _svc.GetAuditLogsAsync(companyId, leaveRequestId);
            return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetAuditLogByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("by-request/{leaveRequestId:int}")]
    public async Task<ActionResult> GetByRequest(int leaveRequestId)
    {
        var r = await _svc.GetAuditLogsAsync(null, leaveRequestId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("by-employee/{employeeId:int}")]
    public async Task<ActionResult> GetByEmployee(int employeeId)
    {
        var r = await _svc.GetAuditLogsByEmployeeAsync(employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }
}

// ── LeaveRequestApprovalHistory ───────────────────────────────────────────────

[ApiController]
[Route("api/leave-request-approval-history")]
[Authorize]
public class LeaveRequestApprovalHistoryController : ControllerBase
{
    private readonly ILeaveService _svc;

    public LeaveRequestApprovalHistoryController(ILeaveService svc) => _svc = svc;

    /// <summary>
    /// Récupère l'historique d'approbation d'une demande de congé.
    /// Note : la lecture des histories est exposée via les demandes elles-mêmes.
    /// Cet endpoint délègue au service pour rester cohérent avec l'architecture.
    /// </summary>
    [HttpGet("by-request/{leaveRequestId:int}")]
    public async Task<ActionResult> GetByRequest(int leaveRequestId)
    {
        // L'historique est retourné via les audit logs filtrés sur la LeaveRequest
        var r = await _svc.GetAuditLogsAsync(null, leaveRequestId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetAuditLogByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }
}
