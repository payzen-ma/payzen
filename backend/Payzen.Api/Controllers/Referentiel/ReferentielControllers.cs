using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Api.Extensions;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.DTOs.Referentiel;
using Payzen.Application.Interfaces;

namespace Payzen.Api.Controllers.Referentiel;

// Form data partagé (Genders, Statuses, etc.) fourni par IEmployeeService.GetFormDataAsync

// ── Countries ─────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/countries")]
[Authorize]
public class CountriesController : ControllerBase
{
    private readonly IReferentielService _svc;

    public CountriesController(IReferentielService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetCountriesAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetCountryByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CountryCreateDto dto)
    {
        var r = await _svc.CreateCountryAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] CountryUpdateDto dto)
    {
        var r = await _svc.UpdateCountryAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteCountryAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── Cities ────────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/cities")]
[Authorize]
public class CitiesController : ControllerBase
{
    private readonly IReferentielService _svc;

    public CitiesController(IReferentielService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? countryId)
    {
        var r = await _svc.GetCitiesAsync(countryId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("country/{countryId:int}")]
    public async Task<ActionResult> GetByCountry(int countryId)
    {
        var r = await _svc.GetCitiesAsync(countryId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetCityByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CityCreateDto dto)
    {
        var r = await _svc.CreateCityAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] CityUpdateDto dto)
    {
        var r = await _svc.UpdateCityAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteCityAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── Nationalities ─────────────────────────────────────────────────────────────

[ApiController]
[Route("api/nationalities")]
[Authorize]
public class NationalitiesController : ControllerBase
{
    private readonly IReferentielService _svc;

    public NationalitiesController(IReferentielService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetNationalitiesAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] NationalityCreateDto dto)
    {
        var r = await _svc.CreateNationalityAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteNationalityAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── Genders ───────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/genders")]
[Authorize]
public class GendersController : ControllerBase
{
    private readonly IReferentielService _svc;

    public GendersController(IReferentielService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetGendersAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetGenderByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] GenderCreateDto dto)
    {
        var r = await _svc.CreateGenderAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] GenderUpdateDto dto)
    {
        var r = await _svc.UpdateGenderAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteGenderAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── Statuses ──────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/statuses")]
[Authorize]
public class StatusesController : ControllerBase
{
    private readonly IReferentielService _svc;

    public StatusesController(IReferentielService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetStatusesAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetStatusByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] StatusCreateDto dto)
    {
        var r = await _svc.CreateStatusAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] StatusUpdateDto dto)
    {
        var r = await _svc.UpdateStatusAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteStatusAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── EducationLevels ───────────────────────────────────────────────────────────

[ApiController]
[Route("api/education-levels")]
[Authorize]
public class EducationLevelsController : ControllerBase
{
    private readonly IReferentielService _svc;

    public EducationLevelsController(IReferentielService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetEducationLevelsAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetEducationLevelByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EducationLevelCreateDto dto)
    {
        var r = await _svc.CreateEducationLevelAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] EducationLevelUpdateDto dto)
    {
        var r = await _svc.UpdateEducationLevelAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteEducationLevelAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── MaritalStatuses ───────────────────────────────────────────────────────────

[ApiController]
[Route("api/marital-statuses")]
[Authorize]
public class MaritalStatusesController : ControllerBase
{
    private readonly IReferentielService _svc;

    public MaritalStatusesController(IReferentielService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetMaritalStatusesAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetMaritalStatusByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] MaritalStatusCreateDto dto)
    {
        var r = await _svc.CreateMaritalStatusAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] MaritalStatusUpdateDto dto)
    {
        var r = await _svc.UpdateMaritalStatusAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteMaritalStatusAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── LegalContractTypes (referentiel global) ────────────────────────────────────

[ApiController]
[Route("api/legal-contract-types")]
[Authorize]
public class LegalContractTypeController : ControllerBase
{
    private readonly IReferentielService _svc;

    public LegalContractTypeController(IReferentielService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetLegalContractTypesAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetLegalContractTypeByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] LegalContractTypeCreateDtos dto)
    {
        var r = await _svc.CreateLegalContractTypeAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] LegalContractTypeUpdateDtos dto)
    {
        var r = await _svc.UpdateLegalContractTypeAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteLegalContractTypeAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── StateEmploymentPrograms ───────────────────────────────────────────────────

[ApiController]
[Route("api/state-employment-programs")]
[Authorize]
public class StateEmploymentProgramController : ControllerBase
{
    private readonly IReferentielService _svc;

    public StateEmploymentProgramController(IReferentielService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var r = await _svc.GetStateEmploymentProgramsAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetStateEmploymentProgramByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] StateEmploymentProgramCreateDto dto)
    {
        var r = await _svc.CreateStateEmploymentProgramAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] StateEmploymentProgramUpdateDto dto)
    {
        var r = await _svc.UpdateStateEmploymentProgramAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteStateEmploymentProgramAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── OvertimeRateRules ─────────────────────────────────────────────────────────

[ApiController]
[Route("api/overtime-rate-rules")]
[Authorize]
public class OvertimeRateRuleController : ControllerBase
{
    private readonly IReferentielService _svc;

    public OvertimeRateRuleController(IReferentielService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] bool? isActive)
    {
        var r = await _svc.GetOvertimeRateRulesAsync(isActive);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("categories")]
    public async Task<ActionResult> GetCategories()
    {
        var r = await _svc.GetOvertimeRateRuleCategoriesAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetOvertimeRateRuleByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] OvertimeRateRuleCreateDto dto)
    {
        var r = await _svc.CreateOvertimeRateRuleAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] OvertimeRateRuleUpdateDto dto)
    {
        var r = await _svc.UpdateOvertimeRateRuleAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteOvertimeRateRuleAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}
