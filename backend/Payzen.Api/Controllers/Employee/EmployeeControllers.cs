using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;
using Payzen.Api.Extensions;
using Payzen.Domain.Enums;

namespace Payzen.Api.Controllers.Employee;

// ── Employee ──────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/employee")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _svc;
    public EmployeeController(IEmployeeService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? companyId)
    {
        var r = await _svc.GetAllAsync(companyId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("me")]
    public async Task<ActionResult> GetMe()
    {
        var userId = User.GetUserId();
        if (userId <= 0) return Unauthorized();
        var r = await _svc.GetCurrentAsync(userId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("summary")]
    public async Task<ActionResult> GetSummary([FromQuery] int? companyId)
    {
        var r = await _svc.GetSummaryAsync(companyId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("company/{companyId:int}")]
    public async Task<ActionResult> GetByCompany(int companyId)
    {
        var r = await _svc.GetByCompanyAsync(companyId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("departement/{departementId:int}")]
    public async Task<ActionResult> GetByDepartement(int departementId)
    {
        var r = await _svc.GetByDepartementAsync(departementId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("manager/{managerId:int}/subordinates")]
    public async Task<ActionResult> GetSubordinates(int managerId)
    {
        var r = await _svc.GetSubordinatesAsync(managerId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult> GetHistory(int id)
    {
        var r = await _svc.GetHistoryAsync(id);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("current")]
    public async Task<ActionResult> GetCurrent()
    {
        var userId = User.GetUserId();
        if (userId <= 0) return Unauthorized();
        var r = await _svc.GetCurrentAsync(userId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost("import-sage")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> ImportSage(
        IFormFile? file,
        [FromQuery] int? companyId = null,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null,
        [FromQuery] bool preview = false)
    {
        var userId = User.GetUserId();
        if (userId <= 0)
            return Unauthorized();
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "Fichier CSV invalide ou vide" });
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".csv")
            return BadRequest(new { Message = "Le fichier doit être au format CSV (.csv)" });

        await using var stream = file.OpenReadStream();
        var r = await _svc.ImportFromSageAsync(stream, companyId, userId, month, year, preview);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("{id:int}/details")]
    public async Task<ActionResult> GetDetail(int id)
    {
        var userId = User.GetUserId();
        if (userId <= 0) return Unauthorized();
        var r = await _svc.GetDetailAsync(id, userId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("form-data")]
    public async Task<ActionResult> GetFormData([FromQuery] int? companyId)
    {
        var userId = User.GetUserId();
        if (userId <= 0) return Unauthorized();
        var r = await _svc.GetFormDataAsync(companyId, userId);
        if (!r.Success)
        {
            if (r.Error == "Société cible non trouvée.") return NotFound(new { Message = r.Error });
            if (r.Error == "Accès refusé à cette société.") return Forbid();
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EmployeeCreateDto dto)
    {
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] EmployeeUpdateDto dto)
    {
        var r = await _svc.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult> Put(int id, [FromBody] EmployeeUpdateDto dto) => Patch(id, dto);

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── Absences (Angular : /api/absences/...) — parité ancien EmployeeAbsenceController ─

[ApiController]
[Route("api/absences")]
[Authorize]
public class AbsencesController : ControllerBase
{
    private readonly IEmployeeAbsenceService _absences;

    public AbsencesController(IEmployeeAbsenceService absences) => _absences = absences;

    [HttpGet("stats")]
    public async Task<ActionResult> GetStats([FromQuery] int companyId, [FromQuery] int? employeeId = null)
    {
        var r = await _absences.GetStatsAsync(companyId, employeeId);
        if (!r.Success)
        {
            return r.Error switch
            {
                "Société non trouvée" => NotFound(new { Message = r.Error }),
                "Employé non trouvé" => NotFound(new { Message = r.Error }),
                _ => BadRequest(new { Message = r.Error })
            };
        }
        return Ok(r.Data);
    }

    [HttpGet("types")]
    public async Task<ActionResult> GetTypes([FromQuery] int companyId)
    {
        var r = await _absences.GetDistinctTypesAsync(companyId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet]
    public async Task<ActionResult> GetList(
        [FromQuery] int companyId,
        [FromQuery] int? employeeId = null,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] AbsenceDurationType? durationType = null,
        [FromQuery] AbsenceStatus? status = null,
        [FromQuery] string? absenceType = null,
        [FromQuery] int limit = 100)
    {
        var r = await _absences.GetByCompanyAsync(companyId, employeeId, startDate, endDate, durationType, status, absenceType, limit);
        if (!r.Success)
        {
            if (r.Error == "Société non trouvée") return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("Employé") == true) return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _absences.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EmployeeAbsenceCreateDto dto)
    {
        var r = await _absences.CreateAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error?.Contains("chevauche") == true || r.Error?.Contains("déjà") == true)
                return Conflict(new { Message = r.Error });
            if (r.Error == "Employé non trouvé") return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] EmployeeAbsenceUpdateDto dto)
    {
        var r = await _absences.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}/decision")]
    public async Task<ActionResult> Decision(int id, [FromBody] EmployeeAbsenceDecisionDto dto)
    {
        var r = await _absences.DecideAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/submit")]
    public async Task<ActionResult> Submit(int id)
    {
        var r = await _absences.SubmitAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Accès refusé.") return Forbid();
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult> Approve(int id, [FromBody] EmployeeAbsenceApprovalDto? dto = null)
    {
        var r = await _absences.ApproveAsync(id, User.GetUserId(), dto?.Comment);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult> Reject(int id, [FromBody] EmployeeAbsenceRejectionDto dto)
    {
        var r = await _absences.RejectAsync(id, User.GetUserId(), dto.Reason);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult> Cancel(int id, [FromBody] EmployeeAbsenceCancellationDto? dto = null)
    {
        var r = await _absences.CancelAsync(id, dto ?? new EmployeeAbsenceCancellationDto(), User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Accès refusé.") return Forbid();
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _absences.DeleteAsync(id, User.GetUserId());
        return r.Success ? NoContent() : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/upload")]
    public ActionResult UploadNotImplemented(int id)
        => StatusCode(501, new { Message = "Upload de justification non implémenté sur cette API." });
}

// ── Famille (Angular : /api/employees/{id}/children|spouse) ───────────────────

[ApiController]
[Route("api/employees/{employeeId:int}/children")]
[Authorize]
public class EmployeesChildrenController : ControllerBase
{
    private readonly IEmployeeFamilyService _svc;
    public EmployeesChildrenController(IEmployeeFamilyService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetChildren(int employeeId)
    {
        var r = await _svc.GetChildrenAsync(employeeId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> CreateChild(int employeeId, [FromBody] EmployeeChildCreateDto dto)
    {
        dto.EmployeeId = employeeId;
        var r = await _svc.CreateChildAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{childId:int}")]
    public async Task<ActionResult> PutChild(int employeeId, int childId, [FromBody] EmployeeChildUpdateDto dto)
    {
        var r = await _svc.UpdateChildAsync(childId, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{childId:int}")]
    public async Task<ActionResult> DeleteChild(int employeeId, int childId)
    {
        var r = await _svc.DeleteChildAsync(childId, User.GetUserId());
        return r.Success ? NoContent() : BadRequest(new { Message = r.Error });
    }
}

[ApiController]
[Route("api/employees/{employeeId:int}/spouse")]
[Authorize]
public class EmployeesSpouseController : ControllerBase
{
    private readonly IEmployeeFamilyService _svc;
    public EmployeesSpouseController(IEmployeeFamilyService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetSpouse(int employeeId)
    {
        var r = await _svc.GetSpousesAsync(employeeId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> CreateSpouse(int employeeId, [FromBody] EmployeeSpouseCreateDto dto)
    {
        dto.EmployeeId = employeeId;
        var r = await _svc.CreateSpouseAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut]
    public async Task<ActionResult> UpdateSpouse(int employeeId, [FromBody] EmployeeSpouseUpdateDto dto)
    {
        var r = await _svc.UpdateSpouseByEmployeeAsync(employeeId, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpDelete]
    public async Task<ActionResult> DeleteSpouse(int employeeId)
    {
        var r = await _svc.DeleteSpouseByEmployeeAsync(employeeId, User.GetUserId());
        return r.Success ? NoContent() : NotFound(new { Message = r.Error });
    }
}

// ── Contrats (Angular : /api/employee-contracts/...) ──────────────────────────

[ApiController]
[Route("api/employee-contracts")]
[Authorize]
public class EmployeeContractsListController : ControllerBase
{
    private readonly IEmployeeContractService _contracts;
    private readonly IEmployeeService _employees;

    public EmployeeContractsListController(IEmployeeContractService contracts, IEmployeeService employees)
    {
        _contracts = contracts;
        _employees = employees;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _contracts.GetAllAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult> GetByEmployee(int employeeId)
    {
        var ex = await _employees.GetByIdAsync(employeeId);
        if (!ex.Success) return NotFound(new { Message = "Employé non trouvé" });
        var r = await _contracts.GetByEmployeeAsync(employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _contracts.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EmployeeContractCreateDto dto)
    {
        var r = await _contracts.CreateAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] EmployeeContractUpdateDto dto)
    {
        var r = await _contracts.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] EmployeeContractUpdateDto dto) => Put(id, dto);

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _contracts.DeleteAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── Contracts (sous-ressource employé) ──────────────────────────────────────

[ApiController]
[Route("api/employee/{employeeId:int}/contracts")]
[Authorize]
public class EmployeeContractsController : ControllerBase
{
    private readonly IEmployeeContractService _svc;
    public EmployeeContractsController(IEmployeeContractService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll(int employeeId)
    {
        var r = await _svc.GetByEmployeeAsync(employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int employeeId, int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create(int employeeId, [FromBody] EmployeeContractCreateDto dto)
    {
        dto.EmployeeId = employeeId;
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int employeeId, int id, [FromBody] EmployeeContractUpdateDto dto)
    {
        var r = await _svc.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int employeeId, int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── Salaires (Angular : /api/employee-salaries/...) ───────────────────────────

[ApiController]
[Route("api/employee-salaries")]
[Authorize]
public class EmployeeSalariesListController : ControllerBase
{
    private readonly IEmployeeSalaryService _svc;
    private readonly IEmployeeService _employees;

    public EmployeeSalariesListController(IEmployeeSalaryService svc, IEmployeeService employees)
    {
        _svc = svc;
        _employees = employees;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetAllSalariesAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult> GetByEmployee(int employeeId)
    {
        var ex = await _employees.GetByIdAsync(employeeId);
        if (!ex.Success) return NotFound(new { Message = "Employé non trouvé" });
        var r = await _svc.GetByEmployeeAsync(employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("contract/{contractId:int}")]
    public async Task<ActionResult> GetByContract(int contractId)
    {
        var r = await _svc.GetByContractAsync(contractId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EmployeeSalaryCreateDto dto)
    {
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        return r.Success
            ? CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data)
            : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] EmployeeSalaryUpdateDto dto)
    {
        var r = await _svc.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] EmployeeSalaryUpdateDto dto) => Put(id, dto);

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? NoContent() : BadRequest(new { Message = r.Error });
    }
}

// ── Composants salaire (Angular : /api/employee-salary-components/...) ───────

[ApiController]
[Route("api/employee-salary-components")]
[Authorize]
public class EmployeeSalaryComponentsListController : ControllerBase
{
    private readonly IEmployeeSalaryService _svc;

    public EmployeeSalaryComponentsListController(IEmployeeSalaryService svc) => _svc = svc;

    [HttpGet("non-imposables")]
    [AllowAnonymous]
    public IActionResult GetNonImposables()
    {
        var list = new[]
        {
            new { code = "TRANSPORT",       label = "Prime de transport" },
            new { code = "KILOMETRIQUE",    label = "Indemnité kilométrique" },
            new { code = "TOURNEE",         label = "Indemnité de tournée" },
            new { code = "REPRESENTATION",  label = "Indemnité de représentation" },
            new { code = "PANIER",          label = "Prime de panier" },
            new { code = "CAISSE",          label = "Indemnité de caisse" },
            new { code = "SALISSURE",       label = "Indemnité de salissure" },
            new { code = "LAIT",            label = "Indemnité de lait" },
            new { code = "OUTILLAGE",       label = "Prime d'outillage" },
            new { code = "AIDE_MEDICALE",   label = "Aide médicale" },
            new { code = "GRATIF_SOCIALE",  label = "Gratification sociale" },
        };
        return Ok(list);
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetAllSalaryComponentsAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("salary/{salaryId:int}")]
    public async Task<ActionResult> GetBySalary(int salaryId)
    {
        var r = await _svc.GetComponentsAsync(salaryId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetComponentByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EmployeeSalaryComponentCreateDto dto)
    {
        var r = await _svc.CreateComponentAsync(dto, User.GetUserId());
        return r.Success
            ? CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data)
            : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] EmployeeSalaryComponentUpdateDto dto)
    {
        var r = await _svc.UpdateComponentAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] EmployeeSalaryComponentUpdateDto dto) => Put(id, dto);

    [HttpPost("revise/{id:int}")]
    public async Task<ActionResult> Revise(int id, [FromBody] EmployeeSalaryComponentUpdateDto dto)
    {
        var r = await _svc.ReviseSalaryComponentAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteComponentAsync(id, User.GetUserId());
        return r.Success ? NoContent() : BadRequest(new { Message = r.Error });
    }
}

// ── Documents (sous-ressource employé) ────────────────────────────────────────

[ApiController]
[Route("api/employee/{employeeId:int}/documents")]
[Authorize]
public class EmployeeDocumentsController : ControllerBase
{
    private readonly IEmployeeDocumentService _svc;
    private readonly IWebHostEnvironment _env;
    public EmployeeDocumentsController(IEmployeeDocumentService svc, IWebHostEnvironment env)
    {
        _svc = svc;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll(int employeeId)
    {
        var r = await _svc.GetByEmployeeAsync(employeeId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create(int employeeId, [FromBody] EmployeeDocumentCreateDto dto)
    {
        dto.EmployeeId = employeeId;
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        return r.Success
            ? CreatedAtAction(nameof(GetAll), new { employeeId }, r.Data)
            : BadRequest(new { Message = r.Error });
    }

    [HttpPost("upload")]
    public async Task<ActionResult> Upload(
        int employeeId,
        [FromForm] IFormFile file,
        [FromForm] string? type,
        [FromForm] DateTime? expirationDate)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "Fichier requis." });

        var originalName = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(originalName);
        var uniqueName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
        var relativePath = Path.Combine("uploads", "employees", employeeId.ToString(), uniqueName).Replace("\\", "/");
        var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var dto = new EmployeeDocumentCreateDto
        {
            EmployeeId = employeeId,
            Name = originalName,
            FilePath = relativePath,
            DocumentType = string.IsNullOrWhiteSpace(type) ? "other" : type.Trim(),
            ExpirationDate = expirationDate
        };

        var r = await _svc.CreateAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
            return BadRequest(new { Message = r.Error });
        }

        return CreatedAtAction(nameof(GetAll), new { employeeId }, r.Data);
    }

    [HttpGet("{id:int}/download")]
    public async Task<ActionResult> Download(int employeeId, int id)
    {
        var r = await _svc.GetByIdAsync(id);
        if (!r.Success || r.Data == null || r.Data.EmployeeId != employeeId)
            return NotFound(new { Message = "Document introuvable." });

        var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var relative = (r.Data.FilePath ?? string.Empty).TrimStart('/', '\\');
        var fullPath = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(fullPath))
            return NotFound(new { Message = "Fichier introuvable sur le serveur." });

        return PhysicalFile(fullPath, "application/octet-stream", r.Data.Name);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int employeeId, int id)
    {
        var byId = await _svc.GetByIdAsync(id);
        if (!byId.Success || byId.Data == null || byId.Data.EmployeeId != employeeId)
            return NotFound(new { Message = "Document introuvable." });

        var root = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var relative = (byId.Data.FilePath ?? string.Empty).TrimStart('/', '\\');
        var fullPath = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

        var r = await _svc.DeleteAsync(id, User.GetUserId());
        if (!r.Success) return BadRequest(new { Message = r.Error });

        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);

        return NoContent();
    }
}

// ── Family (children + spouse) ────────────────────────────────────────────────

[ApiController]
[Route("api/employee/{employeeId:int}/children")]
[Authorize]
public class EmployeeChildController : ControllerBase
{
    private readonly IEmployeeFamilyService _svc;
    public EmployeeChildController(IEmployeeFamilyService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll(int employeeId)
    {
        var r = await _svc.GetChildrenAsync(employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int employeeId, int id)
    {
        var r = await _svc.GetChildByIdAsync(id);
        if (!r.Success) return NotFound(new { Message = r.Error });
        if (r.Data!.EmployeeId != employeeId)
            return NotFound(new { Message = "Enfant introuvable pour cet employé." });
        return Ok(r.Data);
    }

    [HttpPost]
    public async Task<ActionResult> Create(int employeeId, [FromBody] EmployeeChildCreateDto dto)
    {
        dto.EmployeeId = employeeId;
        var r = await _svc.CreateChildAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int employeeId, int id, [FromBody] EmployeeChildUpdateDto dto)
    {
        var r = await _svc.UpdateChildAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int employeeId, int id)
    {
        var r = await _svc.DeleteChildAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

[ApiController]
[Route("api/employee/{employeeId:int}/spouse")]
[Authorize]
public class EmployeeSpouseController : ControllerBase
{
    private readonly IEmployeeFamilyService _svc;
    public EmployeeSpouseController(IEmployeeFamilyService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> Get(int employeeId)
    {
        var r = await _svc.GetSpousesAsync(employeeId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create(int employeeId, [FromBody] EmployeeSpouseCreateDto dto)
    {
        dto.EmployeeId = employeeId;
        var r = await _svc.CreateSpouseAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch]
    public async Task<ActionResult> Patch(int employeeId, [FromBody] EmployeeSpouseUpdateDto dto)
    {
        var r = await _svc.UpdateSpouseByEmployeeAsync(employeeId, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut]
    public Task<ActionResult> Put(int employeeId, [FromBody] EmployeeSpouseUpdateDto dto) => Patch(employeeId, dto);

    [HttpDelete]
    public async Task<ActionResult> DeleteSpouse(int employeeId)
    {
        var r = await _svc.DeleteSpouseByEmployeeAsync(employeeId, User.GetUserId());
        return r.Success ? NoContent() : NotFound(new { Message = r.Error });
    }
}

// ── Présence (Angular : /api/employee-attendance/...) ─────────────────────────

[ApiController]
[Route("api/employee-attendance")]
[Authorize]
public class EmployeeAttendanceListController : ControllerBase
{
    private readonly IEmployeeAttendanceService _svc;
    private readonly IEmployeeService _employees;

    public EmployeeAttendanceListController(IEmployeeAttendanceService svc, IEmployeeService employees)
    {
        _svc = svc;
        _employees = employees;
    }

    [HttpGet]
    public async Task<ActionResult> GetFiltered(
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] int? employeeId,
        [FromQuery] AttendanceStatus? status,
        [FromQuery] bool includeBreaks = false)
    {
        var r = await _svc.GetAllAsync(startDate, endDate, employeeId, status, includeBreaks);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult> GetByEmployee(int employeeId, [FromQuery] DateOnly? startDate, [FromQuery] DateOnly? endDate, [FromQuery] bool includeBreaks = false)
    {
        var ex = await _employees.GetByIdAsync(employeeId);
        if (!ex.Success) return NotFound(new { Message = "Employé non trouvé" });
        var r = await _svc.GetByEmployeeAsync(employeeId, startDate, endDate, includeBreaks);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id, [FromQuery] bool includeBreaks = false)
    {
        var r = await _svc.GetByIdAsync(id, includeBreaks);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EmployeeAttendanceCreateDto dto)
    {
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] EmployeeAttendanceCreateDto dto)
    {
        var r = await _svc.PutAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] EmployeeAttendanceUpdateDto dto)
    {
        var r = await _svc.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? NoContent() : BadRequest(new { Message = r.Error });
    }

    [HttpPost("check-in")]
    public async Task<ActionResult> CheckIn([FromBody] EmployeeAttendanceCheckDto dto)
    {
        var r = await _svc.CheckInAsync(dto.EmployeeId, null, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("check-out")]
    public async Task<ActionResult> CheckOut([FromBody] EmployeeAttendanceCheckDto dto)
    {
        var r = await _svc.CheckOutAsync(dto.EmployeeId, null, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }
}

// ── Attendance (sous-ressource employé) ───────────────────────────────────────

[ApiController]
[Route("api/employee/{employeeId:int}/attendance")]
[Authorize]
public class EmployeeAttendanceController : ControllerBase
{
    private readonly IEmployeeAttendanceService _svc;
    public EmployeeAttendanceController(IEmployeeAttendanceService svc) => _svc = svc;

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int employeeId, int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost("check-in")]
    public async Task<ActionResult> CheckIn(int employeeId, [FromBody] EmployeeAttendanceCreateDto? dto = null)
    {
        var r = await _svc.CheckInAsync(employeeId, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("check-out")]
    public async Task<ActionResult> CheckOut(int employeeId, [FromQuery] int? attendanceId = null)
    {
        var r = await _svc.CheckOutAsync(employeeId, attendanceId, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet]
    public async Task<ActionResult> GetAll(int employeeId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var from = (year.HasValue && month.HasValue) ? new DateOnly?(new DateOnly(year.Value, month.Value, 1)) : null;
        var to = (year.HasValue && month.HasValue) ? new DateOnly?(new DateOnly(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value))) : null;
        var r = await _svc.GetByEmployeeAsync(employeeId, from, to);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create(int employeeId, [FromBody] EmployeeAttendanceCreateDto dto)
    {
        dto.EmployeeId = employeeId;
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int employeeId, int id, [FromBody] EmployeeAttendanceUpdateDto dto)
    {
        var r = await _svc.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int employeeId, int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── Absences ──────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/employee/{employeeId:int}/absences")]
[Authorize]
public class EmployeeAbsenceController : ControllerBase
{
    private readonly IEmployeeAbsenceService _svc;
    public EmployeeAbsenceController(IEmployeeAbsenceService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll(int employeeId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var r = await _svc.GetByEmployeeAsync(employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int employeeId, int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create(int employeeId, [FromBody] EmployeeAbsenceCreateDto dto)
    {
        dto.EmployeeId = employeeId;
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int employeeId, int id, [FromBody] EmployeeAbsenceUpdateDto dto)
    {
        var r = await _svc.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int employeeId, int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── Heures sup. (Angular : /api/employee-overtimes/...) ──────────────────────

[ApiController]
[Route("api/employee-overtimes")]
[Authorize]
public class EmployeeOvertimesListController : ControllerBase
{
    private readonly IEmployeeOvertimeService _svc;
    private readonly IReferentielService _referentiel;

    public EmployeeOvertimesListController(IEmployeeOvertimeService svc, IReferentielService referentiel)
    {
        _svc = svc;
        _referentiel = referentiel;
    }

    /// <summary>Catalogue des types d'HS (flags) — parité ancien GET /api/employee-overtimes/types.</summary>
    [HttpGet("types")]
    public ActionResult GetOvertimeTypeCatalog()
    {
        var types = new List<object>
        {
            new { Value = (int)OvertimeType.None, Code = "None", DescriptionFr = "Aucun", DescriptionEn = "None" },
            new { Value = (int)OvertimeType.Standard, Code = "Standard", DescriptionFr = "Standard", DescriptionEn = "Standard" },
            new { Value = (int)OvertimeType.WeeklyRest, Code = "WeeklyRest", DescriptionFr = "Repos hebdomadaire", DescriptionEn = "Weekly Rest" },
            new { Value = (int)OvertimeType.PublicHoliday, Code = "PublicHoliday", DescriptionFr = "Jour férié", DescriptionEn = "Public Holiday" },
            new { Value = (int)OvertimeType.Night, Code = "Night", DescriptionFr = "Nuit", DescriptionEn = "Night" },
            new { Value = (int)(OvertimeType.Standard | OvertimeType.Night), Code = "Standard_Night", DescriptionFr = "Standard + Nuit", DescriptionEn = "Standard + Night" },
            new { Value = (int)(OvertimeType.WeeklyRest | OvertimeType.Night), Code = "WeeklyRest_Night", DescriptionFr = "Repos hebdomadaire + Nuit", DescriptionEn = "Weekly Rest + Night" },
            new { Value = (int)(OvertimeType.PublicHoliday | OvertimeType.Night), Code = "PublicHoliday_Night", DescriptionFr = "Jour férié + Nuit", DescriptionEn = "Public Holiday + Night" },
            new { Value = (int)OvertimeType.FerieOrRest, Code = "FerieOrRest", DescriptionFr = "Férié ou Repos", DescriptionEn = "Holiday or Rest" }
        };
        return Ok(types);
    }

    [HttpGet("rate-rules")]
    public async Task<ActionResult> GetRateRules([FromQuery] bool? isActive)
    {
        var r = await _referentiel.GetOvertimeRateRulesAsync(isActive);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] int? companyId,
        [FromQuery] int? employeeId,
        [FromQuery] OvertimeStatus? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        [FromQuery] bool? isProcessedInPayroll)
    {
        var from = fromDate ?? startDate;
        var to = toDate ?? endDate;
        var r = await _svc.GetAllAsync(companyId, employeeId, status, from, to, isProcessedInPayroll);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("stats")]
    public async Task<ActionResult> GetStats([FromQuery] int companyId, [FromQuery] int? employeeId = null)
    {
        var r = await _svc.GetStatsAsync(companyId, employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}", Name = "GetEmployeeOvertimeById")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EmployeeOvertimeCreateDto dto)
    {
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        if (!r.Success)
            return BadRequest(new { Message = r.Error });
        var outcome = r.Data!;
        if (outcome.Overtimes.Count == 1)
            return CreatedAtRoute("GetEmployeeOvertimeById", new { id = outcome.Overtimes[0].Id }, outcome.Overtimes[0]);
        return Ok(new
        {
            Message = $"{outcome.Overtimes.Count} segments d'overtime créés avec succès (split automatique)",
            SplitBatchId = outcome.SplitBatchId,
            TotalSegments = outcome.Overtimes.Count,
            Overtimes = outcome.Overtimes
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Put(int id, [FromBody] EmployeeOvertimeUpdateDto dto)
    {
        var r = await _svc.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] EmployeeOvertimeUpdateDto dto) => Put(id, dto);

    [HttpPut("{id:int}/submit")]
    public async Task<ActionResult> Submit(int id, [FromBody] EmployeeOvertimeSubmitDto? dto = null)
    {
        var r = await _svc.SubmitAsync(id, dto ?? new EmployeeOvertimeSubmitDto(), User.GetUserId());
        if (!r.Success && r.Error == "Accès refusé.")
            return Forbid();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult> Cancel(int id)
    {
        var r = await _svc.CancelAsync(id, User.GetUserId());
        if (!r.Success && r.Error == "Accès refusé.")
            return Forbid();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}/approve")]
    public async Task<ActionResult> Approve(int id, [FromBody] EmployeeOvertimeApprovalDto dto)
    {
        var r = await _svc.DecideAsync(id, dto, User.GetUserId());
        if (!r.Success && r.Error == "Accès refusé.")
            return Forbid();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? NoContent() : BadRequest(new { Message = r.Error });
    }
}

// ── Overtime (sous-ressource employé) ────────────────────────────────────────

[ApiController]
[Route("api/employee/{employeeId:int}/overtime")]
[Authorize]
public class EmployeeOvertimeController : ControllerBase
{
    private readonly IEmployeeOvertimeService _svc;
    public EmployeeOvertimeController(IEmployeeOvertimeService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll(int employeeId, [FromQuery] int? year, [FromQuery] int? month)
    {
        var r = await _svc.GetByEmployeeAsync(employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}", Name = "GetEmployeeOvertimeByEmployeeAndId")]
    public async Task<ActionResult> GetById(int employeeId, int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create(int employeeId, [FromBody] EmployeeOvertimeCreateDto dto)
    {
        dto.EmployeeId = employeeId;
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        if (!r.Success)
            return BadRequest(new { Message = r.Error });
        var outcome = r.Data!;
        if (outcome.Overtimes.Count == 1)
            return CreatedAtRoute("GetEmployeeOvertimeByEmployeeAndId", new { employeeId, id = outcome.Overtimes[0].Id }, outcome.Overtimes[0]);
        return Ok(new
        {
            Message = $"{outcome.Overtimes.Count} segments d'overtime créés avec succès (split automatique)",
            SplitBatchId = outcome.SplitBatchId,
            TotalSegments = outcome.Overtimes.Count,
            Overtimes = outcome.Overtimes
        });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int employeeId, int id, [FromBody] EmployeeOvertimeUpdateDto dto)
    {
        var r = await _svc.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}/submit")]
    public async Task<ActionResult> Submit(int employeeId, int id, [FromBody] EmployeeOvertimeSubmitDto? dto = null)
    {
        var r = await _svc.SubmitAsync(id, dto ?? new EmployeeOvertimeSubmitDto(), User.GetUserId());
        if (!r.Success && r.Error == "Accès refusé.")
            return Forbid();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<ActionResult> Cancel(int employeeId, int id)
    {
        var r = await _svc.CancelAsync(id, User.GetUserId());
        if (!r.Success && r.Error == "Accès refusé.")
            return Forbid();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}/approve")]
    public async Task<ActionResult> Approve(int employeeId, int id, [FromBody] EmployeeOvertimeApprovalDto dto)
    {
        var r = await _svc.DecideAsync(id, dto, User.GetUserId());
        if (!r.Success && r.Error == "Accès refusé.")
            return Forbid();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int employeeId, int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? NoContent() : BadRequest(new { Message = r.Error });
    }
}

[ApiController]
[Route("api/overtime-types")]
[Authorize]
public class OvertimeTypesController : ControllerBase
{
    private readonly IReferentielService _svc;
    public OvertimeTypesController(IReferentielService svc) => _svc = svc;
    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] bool? isActive) { var r = await _svc.GetOvertimeRateRulesAsync(isActive); return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error }); }
}

// ── SalaryPackage Assignments ─────────────────────────────────────────────────

[ApiController]
[Route("api/salary-package-assignments")]
[Authorize]
public class SalaryPackageAssignmentsController : ControllerBase
{
    private readonly ISalaryPackageService _svc;
    public SalaryPackageAssignmentsController(ISalaryPackageService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? companyId, [FromQuery] int? employeeId)
    {
        var r = await _svc.GetAllAssignmentsAsync(companyId, employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetAssignmentByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult> GetByEmployee(int employeeId)
    {
        var r = await _svc.GetAssignmentsAsync(employeeId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Assign([FromBody] SalaryPackageAssignmentCreateDto dto)
    {
        var r = await _svc.AssignAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] SalaryPackageAssignmentUpdateDto dto)
    {
        var r = await _svc.UpdateAssignmentAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult> Put(int id, [FromBody] SalaryPackageAssignmentUpdateDto dto) => Update(id, dto);

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.RevokeAssignmentAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}
