using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Company;
using Payzen.Application.Interfaces;
using Payzen.Api.Extensions;

namespace Payzen.Api.Controllers.Company;

// ── Companies ─────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _svc;
    private readonly IValidator<CompanyCreateDto> _createValidator;

    public CompanyController(ICompanyService svc, IValidator<CompanyCreateDto> createValidator)
    {
        _svc = svc;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetAllAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("form-data")]
    [AllowAnonymous]
    public async Task<ActionResult> GetFormData()
    {
        var r = await _svc.GetFormDataAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("search")]
    public async Task<ActionResult> Search([FromQuery] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return BadRequest(new { Message = "Le terme de recherche est requis" });
        var r = await _svc.SearchAsync(searchTerm);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("by-city/{cityId:int}")]
    public async Task<ActionResult> GetByCity(int cityId)
    {
        var r = await _svc.GetByCityAsync(cityId);
        if (!r.Success) return NotFound(new { Message = r.Error });
        return Ok(r.Data);
    }

    [HttpGet("by-country/{countryId:int}")]
    public async Task<ActionResult> GetByCountry(int countryId)
    {
        var r = await _svc.GetByCountryAsync(countryId);
        if (!r.Success) return NotFound(new { Message = r.Error });
        return Ok(r.Data);
    }

    [HttpGet("managedby/{expertCompanyId:int}")]
    public async Task<ActionResult> GetManagedBy(int expertCompanyId)
    {
        var r = await _svc.GetManagedByAsync(expertCompanyId);
        if (!r.Success)
        {
            if (r.Error == "Cabinet gestionnaire introuvable") return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("{companyId:int}/history")]
    public async Task<ActionResult> GetHistory(int companyId)
    {
        var r = await _svc.GetHistoryAsync(companyId);
        if (!r.Success) return NotFound(new { Message = r.Error });
        return Ok(r.Data);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CompanyCreateDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(new { Message = "Données invalides.", Errors = validation.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) });
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error?.Contains("existe déjà") == true) return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Company.Id }, r.Data);
    }

    [HttpPost("create-by-expert")]
    public async Task<ActionResult> CreateByExpert([FromBody] CompanyCreateByExpertDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(new { Message = "Données invalides.", Errors = validation.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()) });
        var r = await _svc.CreateByExpertAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error?.Contains("existe déjà") == true) return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Company.Id }, r.Data);
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] CompanyUpdateDto dto)
    {
        var r = await _svc.PatchAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Entreprise non trouvée") return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("existe déjà") == true) return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }

    // ── Holidays (nested under company) ──────────────────────────────────────

    [HttpGet("{companyId:int}/holidays")]
    public async Task<ActionResult> GetHolidays(int companyId, [FromQuery] int? year)
    {
        var r = await _svc.GetHolidaysAsync(companyId, year);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{companyId:int}/holidays")]
    public async Task<ActionResult> CreateHoliday(int companyId, [FromBody] HolidayCreateDto dto)
    {
        dto.CompanyId = companyId;
        var r = await _svc.CreateHolidayAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("holidays/{id:int}")]
    public async Task<ActionResult> PatchHoliday(int id, [FromBody] HolidayUpdateDto dto)
    {
        var r = await _svc.UpdateHolidayAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("holidays/{id:int}")]
    public async Task<ActionResult> DeleteHoliday(int id)
    {
        var r = await _svc.DeleteHolidayAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }

    // ── WorkingCalendars ──────────────────────────────────────────────────────

    [HttpGet("{companyId:int}/working-calendars")]
    public async Task<ActionResult> GetWorkingCalendars(int companyId)
    {
        var r = await _svc.GetWorkingCalendarAsync(companyId);
        if (!r.Success)
        {
            if (r.Error == "Société non trouvée") return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpPost("{companyId:int}/working-calendars")]
    public async Task<ActionResult> CreateWorkingCalendar(int companyId, [FromBody] WorkingCalendarCreateDto dto)
    {
        dto.CompanyId = companyId;
        var r = await _svc.UpsertWorkingCalendarDayAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Société non trouvée") return NotFound(new { Message = r.Error });
            if (r.Error == "Un calendrier existe déjà pour ce jour de la semaine dans cette société")
                return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction("GetById", "WorkingCalendars", new { id = r.Data!.Id }, r.Data);
    }
}

// ── Departements ──────────────────────────────────────────────────────────────

[ApiController]
[Route("api/departements")]
[Authorize]
public class DepartementsController : ControllerBase
{
    private readonly ICompanyService _svc;
    public DepartementsController(ICompanyService svc) => _svc = svc;

    /// <summary>Récupère tous les départements actifs.</summary>
    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetAllDepartementsAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    /// <summary>Récupère un département par ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetDepartementByIdAsync(id);
        if (!r.Success) return NotFound(new { Message = r.Error });
        return Ok(r.Data);
    }

    /// <summary>Récupère tous les départements d'une société.</summary>
    [HttpGet("company/{companyId:int}")]
    public async Task<ActionResult> GetByCompanyId(int companyId)
    {
        var r = await _svc.GetDepartementsAsync(companyId);
        if (!r.Success) return NotFound(new { Message = r.Error });
        return Ok(r.Data);
    }

    /// <summary>Crée un nouveau département.</summary>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] DepartementCreateDto dto)
    {
        var r = await _svc.CreateDepartementAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error?.Contains("existe déjà") == true) return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data);
    }

    /// <summary>Met à jour un département.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] DepartementUpdateDto dto)
    {
        var r = await _svc.UpdateDepartementAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Département non trouvé") return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("existe déjà") == true) return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    /// <summary>Supprime un département (soft delete).</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteDepartementAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Département non trouvé") return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}

// ── JobPositions ──────────────────────────────────────────────────────────────

[ApiController]
[Route("api/job-positions")]
[Authorize]
public class JobPositionsController : ControllerBase
{
    private readonly ICompanyService _svc;
    public JobPositionsController(ICompanyService svc) => _svc = svc;

    /// <summary>Récupère tous les postes actifs.</summary>
    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetAllJobPositionsAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    /// <summary>Récupère un poste par ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetJobPositionByIdAsync(id);
        if (!r.Success) return NotFound(new { Message = r.Error });
        return Ok(r.Data);
    }

    /// <summary>Récupère tous les postes d'une société.</summary>
    [HttpGet("by-company/{companyId:int}")]
    public async Task<ActionResult> GetByCompany(int companyId)
    {
        var r = await _svc.GetJobPositionsAsync(companyId);
        if (!r.Success) return NotFound(new { Message = r.Error });
        return Ok(r.Data);
    }

    /// <summary>Crée un nouveau poste.</summary>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] JobPositionCreateDto dto)
    {
        var r = await _svc.CreateJobPositionAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Société non trouvée") return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("existe déjà") == true) return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data);
    }

    /// <summary>Met à jour un poste.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] JobPositionUpdateDto dto)
    {
        var r = await _svc.UpdateJobPositionAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Poste non trouvé") return NotFound(new { Message = r.Error });
            if (r.Error?.Contains("existe déjà") == true) return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    /// <summary>Supprime un poste (soft delete).</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteJobPositionAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Poste non trouvé") return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}


// ── Company Documents ─────────────────────────────────────────────────────────

[ApiController]
[Route("api/company-documents")]
[Route("api/CompanyDocuments")]
[Authorize]
public class CompanyDocumentsController : ControllerBase
{
    private readonly ICompanyDocumentService _files;
    private readonly ICompanyService _companies;
    public CompanyDocumentsController(ICompanyDocumentService files, ICompanyService companies)
    {
        _files = files;
        _companies = companies;
    }

    // ── Resource endpoints (parité monolithe) ───────────────────────────────

    [HttpGet("company/{companyId:int}")]
    public async Task<ActionResult> GetByCompanyId(int companyId)
    {
        var r = await _companies.GetDocumentsAsync(companyId);
        if (!r.Success)
        {
            if (r.Error == "Entreprise non trouvée") return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _companies.GetDocumentByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _companies.GetAllDocumentsAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> Upload([FromForm] CompanyDocumentUploadDto dto)
    {
        var saved = await _files.SaveFileAsync(dto.File, dto.CompanyId, dto.DocumentType);
        if (!saved.Success) return BadRequest(new { Message = saved.Error });

        var created = await _companies.CreateDocumentAsync(
            new CompanyDocumentCreateDto
            {
                CompanyId = dto.CompanyId,
                Name = dto.File.FileName,
                FilePath = saved.Data!,
                DocumentType = dto.DocumentType
            },
            User.GetUserId());

        if (!created.Success)
        {
            if (created.Error == "Entreprise non trouvée") return NotFound(new { Message = created.Error });
            return BadRequest(new { Message = created.Error });
        }

        return CreatedAtAction(nameof(GetById), new { id = created.Data!.Id }, created.Data);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CompanyDocumentCreateDto dto)
    {
        var r = await _companies.CreateDocumentAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Entreprise non trouvée") return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] CompanyDocumentUpdateDto dto)
    {
        var r = await _companies.UpdateDocumentAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteById(int id)
    {
        var r = await _companies.DeleteDocumentAsync(id, User.GetUserId());
        return r.Success ? NoContent() : NotFound(new { Message = r.Error });
    }

    [HttpGet("{id:int}/download")]
    public async Task<ActionResult> DownloadById(int id)
    {
        var doc = await _companies.GetDocumentByIdAsync(id);
        if (!doc.Success) return NotFound(new { Message = doc.Error });

        var file = await _files.GetFileAsync(doc.Data!.FilePath);
        if (!file.Success) return NotFound(new { Message = file.Error });

        var (bytes, contentType, fileName) = file.Data;
        return File(bytes, contentType, fileName);
    }

    // ── Fichier brut (endpoints existants) ──────────────────────────────────

    [HttpPost("{companyId:int}")]
    public async Task<ActionResult> Upload(int companyId, IFormFile file, [FromQuery] string? documentType)
    {
        var r = await _files.SaveFileAsync(file, companyId, documentType);
        return r.Success ? Ok(new { path = r.Data }) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("download")]
    public async Task<ActionResult> Download([FromQuery] string filePath)
    {
        var r = await _files.GetFileAsync(filePath);
        if (!r.Success) return NotFound(new { Message = r.Error });
        var (bytes, contentType, fileName) = r.Data;
        return File(bytes, contentType, fileName);
    }

    [HttpDelete("delete")]
    public async Task<ActionResult> Delete([FromQuery] string filePath)
    {
        var r = await _files.DeleteFileAsync(filePath);
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}