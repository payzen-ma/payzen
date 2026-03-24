using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Company;
using Payzen.Application.DTOs.Payroll;
using Payzen.Application.Interfaces;
using Payzen.Api.Extensions;

namespace Payzen.Api.Controllers.SystemData;

// ── SalaryPackages ────────────────────────────────────────────────────────────

[ApiController]
[Route("api/salary-packages")]
[Authorize]
public class SalaryPackagesController : ControllerBase
{
    private readonly ISalaryPackageService _svc;
    public SalaryPackagesController(ISalaryPackageService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? companyId)
    {
        var r = await _svc.GetAllAsync(companyId, null, null);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] SalaryPackageCreateDto dto)
    {
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] SalaryPackageUpdateDto dto)
    {
        var r = await _svc.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }

    [HttpGet("templates")]
    public async Task<ActionResult> GetTemplates()
    {
        var r = await _svc.GetTemplatesAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/clone")]
    public async Task<ActionResult> Clone(int id, [FromBody] SalaryPackageCloneDto dto)
    {
        var r = await _svc.CloneAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/new-version")]
    public async Task<ActionResult> NewVersion(int id)
    {
        var r = await _svc.NewVersionAsync(id, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/publish")]
    public async Task<ActionResult> Publish(int id)
    {
        var r = await _svc.PublishAsync(id, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/deprecate")]
    public async Task<ActionResult> Deprecate(int id)
    {
        var r = await _svc.DeprecateAsync(id, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/duplicate")]
    public async Task<ActionResult> Duplicate(int id, [FromBody] SalaryPackageDuplicateDto? dto = null)
    {
        var dup = dto ?? new SalaryPackageDuplicateDto();
        var r = await _svc.DuplicateAsync(id, dup, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{packageId:int}/items")]
    public async Task<ActionResult> GetItems(int packageId)
    {
        var r = await _svc.GetItemsAsync(packageId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{packageId:int}/items")]
    public async Task<ActionResult> AddItem(int packageId, [FromBody] SalaryPackageItemWriteDto dto)
    {
        var r = await _svc.AddItemAsync(packageId, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("items/{itemId:int}")]
    public async Task<ActionResult> UpdateItem(int itemId, [FromBody] SalaryPackageItemWriteDto dto)
    {
        var r = await _svc.UpdateItemAsync(itemId, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("items/{itemId:int}")]
    public async Task<ActionResult> DeleteItem(int itemId)
    {
        var r = await _svc.DeleteItemAsync(itemId, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── PayComponents ─────────────────────────────────────────────────────────────

[ApiController]
[Route("api/pay-components")]
[Authorize]
public class PayComponentsController : ControllerBase
{
    private readonly IPayComponentService _svc;
    public PayComponentsController(IPayComponentService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] bool? isActive)
    {
        var r = await _svc.GetAllAsync(isActive);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("effective")]
    public async Task<ActionResult> GetEffective([FromQuery] DateTime? asOf)
    {
        var r = await _svc.GetEffectiveAsync(asOf);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult> GetByCode(string code)
    {
        var r = await _svc.GetByCodeAsync(code);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] PayComponentWriteDto dto)
    {
        var r = await _svc.CreateAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] PayComponentWriteDto dto)
    {
        var r = await _svc.UpdateAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/new-version")]
    public async Task<ActionResult> NewVersion(int id)
    {
        var r = await _svc.NewVersionAsync(id, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("{id:int}/deactivate")]
    public async Task<ActionResult> Deactivate(int id)
    {
        var r = await _svc.DeactivateAsync(id, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── ContractTypes (par entreprise) ───────────────────────────────────────────

[ApiController]
[Route("api/contract-types")]
[Authorize]
public class ContractTypesController : ControllerBase
{
    private readonly ICompanyService _svc;
    public ContractTypesController(ICompanyService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? companyId)
    {
        if (!companyId.HasValue) return BadRequest(new { Message = "companyId requis." });
        var r = await _svc.GetContractTypesAsync(companyId.Value);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("by-company/{companyId:int}")]
    public async Task<ActionResult> GetByCompany(int companyId)
    {
        var r = await _svc.GetContractTypesAsync(companyId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetContractTypeByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] ContractTypeCreateDto dto)
    {
        var r = await _svc.CreateContractTypeAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] ContractTypeUpdateDto dto)
    {
        var r = await _svc.UpdateContractTypeAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteContractTypeAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── Holidays ──────────────────────────────────────────────────────────────────

[ApiController]
[Route("api/holidays")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly ICompanyService _svc;
    public HolidaysController(ICompanyService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? companyId, [FromQuery] int? year)
    {
        var r = await _svc.GetHolidaysAsync(companyId, year);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetHolidayByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("check")]
    public async Task<ActionResult> Check([FromQuery] int? companyId, [FromQuery] DateOnly date)
    {
        var r = await _svc.CheckHolidayAsync(companyId, date);
        return r.Success ? Ok(new { isHoliday = r.Data }) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("types")]
    public async Task<ActionResult> GetTypes()
    {
        var r = await _svc.GetHolidayTypesAsync();
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] HolidayCreateDto dto)
    {
        var r = await _svc.CreateHolidayAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] HolidayUpdateDto dto)
    {
        var r = await _svc.UpdateHolidayAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteHolidayAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── WorkingCalendars ──────────────────────────────────────────────────────────

[ApiController]
[Route("api/working-calendars")]
[Route("api/working-calendar")]
[Authorize]
public class WorkingCalendarsController : ControllerBase
{
    private readonly ICompanyService _svc;
    public WorkingCalendarsController(ICompanyService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? companyId)
    {
        if (companyId.HasValue)
        {
            var r = await _svc.GetWorkingCalendarAsync(companyId.Value);
            if (!r.Success)
            {
                if (r.Error == "Société non trouvée") return NotFound(new { Message = r.Error });
                return BadRequest(new { Message = r.Error });
            }
            return Ok(r.Data);
        }

        var all = await _svc.GetAllWorkingCalendarsAsync();
        return all.Success ? Ok(all.Data) : BadRequest(new { Message = all.Error });
    }

    [HttpGet("company/{companyId:int}")]
    public async Task<ActionResult> GetByCompany(int companyId)
    {
        var r = await _svc.GetWorkingCalendarAsync(companyId);
        if (!r.Success)
        {
            if (r.Error == "Société non trouvée") return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetWorkingCalendarDayByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] WorkingCalendarCreateDto dto)
    {
        var r = await _svc.UpsertWorkingCalendarDayAsync(dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Société non trouvée") return NotFound(new { Message = r.Error });
            if (r.Error == "Un calendrier existe déjà pour ce jour de la semaine dans cette société")
                return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return CreatedAtAction(nameof(GetById), new { id = r.Data!.Id }, r.Data);
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult> Put(int id, [FromBody] WorkingCalendarUpdateDto dto)
        => UpdateWorkingCalendar(id, dto);

    [HttpPatch("{id:int}")]
    public Task<ActionResult> Patch(int id, [FromBody] WorkingCalendarUpdateDto dto)
        => UpdateWorkingCalendar(id, dto);

    private async Task<ActionResult> UpdateWorkingCalendar(int id, WorkingCalendarUpdateDto dto)
    {
        var r = await _svc.UpdateWorkingCalendarDayAsync(id, dto, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Calendrier de travail non trouvé") return NotFound(new { Message = r.Error });
            if (r.Error == "Société non trouvée") return NotFound(new { Message = r.Error });
            if (r.Error == "Un calendrier existe déjà pour ce jour de la semaine dans cette société")
                return Conflict(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return Ok(r.Data);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteWorkingCalendarDayAsync(id, User.GetUserId());
        if (!r.Success)
        {
            if (r.Error == "Calendrier de travail non trouvé") return NotFound(new { Message = r.Error });
            return BadRequest(new { Message = r.Error });
        }
        return NoContent();
    }
}
