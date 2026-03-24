using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payzen.Application.DTOs.Employee;
using Payzen.Application.Interfaces;
using Payzen.Api.Extensions;
using Payzen.Domain.Enums;

namespace Payzen.Api.Controllers.Employee;

// ── EmployeeCategory ──────────────────────────────────────────────────────────

[ApiController]
[Route("api/employee-categories")]
[Authorize]
public class EmployeeCategoryController : ControllerBase
{
    private readonly IEmployeeService _svc;
    public EmployeeCategoryController(IEmployeeService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] int? companyId)
    {
        if (!companyId.HasValue)
            return BadRequest(new { Message = "companyId est requis." });
        var r = await _svc.GetCategoriesAsync(companyId.Value);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetCategoryByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("company/{companyId:int}")]
    public async Task<ActionResult> GetByCompany(int companyId)
    {
        var r = await _svc.GetCategoriesAsync(companyId);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("by-mode/{mode}")]
    public async Task<ActionResult> GetByMode(EmployeeCategoryMode mode, [FromQuery] int? companyId = null)
    {
        var r = await _svc.GetCategoriesByModeAsync(mode, companyId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] EmployeeCategoryCreateDto dto)
    {
        var r = await _svc.CreateCategoryAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPatch("{id:int}")]
    public async Task<ActionResult> Patch(int id, [FromBody] EmployeeCategoryUpdateDto dto)
    {
        var r = await _svc.UpdateCategoryAsync(id, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var r = await _svc.DeleteCategoryAsync(id, User.GetUserId());
        return r.Success ? Ok() : BadRequest(new { Message = r.Error });
    }
}

// ── AttendanceBreak ───────────────────────────────────────────────────────────

[ApiController]
[Route("api/employee-attendance-break")]
[Authorize]
public class EmployeeAttendanceBreakController : ControllerBase
{
    private readonly IEmployeeAttendanceBreakService _svc;
    public EmployeeAttendanceBreakController(IEmployeeAttendanceBreakService svc) => _svc = svc;

    [HttpPost("start")]
    public async Task<ActionResult> StartBreak([FromBody] StartBreakDto dto)
    {
        var r = await _svc.StartBreakAsync(dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPost("end/{attendanceId:int}")]
    public async Task<ActionResult> EndBreak(int attendanceId, [FromBody] EndBreakDto dto)
    {
        var r = await _svc.EndBreakAsync(attendanceId, dto, User.GetUserId());
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var r = await _svc.GetByIdAsync(id);
        return r.Success ? Ok(r.Data) : NotFound(new { Message = r.Error });
    }

    [HttpGet("attendance/{attendanceId:int}")]
    public async Task<ActionResult> GetByAttendance(int attendanceId)
    {
        var r = await _svc.GetByAttendanceAsync(attendanceId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpGet("attendance/{attendanceId:int}/total-break-time")]
    public async Task<ActionResult> GetTotalBreakTime(int attendanceId)
    {
        var r = await _svc.GetTotalBreakTimeAsync(attendanceId);
        return r.Success ? Ok(r.Data) : BadRequest(new { Message = r.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] EmployeeAttendanceBreakUpdateDto dto)
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
}

