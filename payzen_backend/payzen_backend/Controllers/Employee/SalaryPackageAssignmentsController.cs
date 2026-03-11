using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Payroll;
using payzen_backend.Models.Payroll.Dtos;
using payzen_backend.Services;

namespace payzen_backend.Controllers.Employees
{
    [Route("api/salary-package-assignments")]
    [ApiController]
    [Authorize]
    public class SalaryPackageAssignmentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmployeeEventLogService _eventLogService;

        public SalaryPackageAssignmentsController(AppDbContext db, EmployeeEventLogService eventLogService)
        {
            _db = db;
            _eventLogService = eventLogService;
        }

        /// <summary>
        /// Get salary package assignments, optionally filtered by package or employee.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalaryPackageAssignmentReadDto>>> GetAll(
            [FromQuery] int? salaryPackageId = null,
            [FromQuery] int? employeeId = null)
        {
            if (salaryPackageId.HasValue && salaryPackageId.Value <= 0)
                return BadRequest(new { Message = "salaryPackageId must be a positive number" });

            if (employeeId.HasValue && employeeId.Value <= 0)
                return BadRequest(new { Message = "employeeId must be a positive number" });

            var query = _db.SalaryPackageAssignments
                .AsNoTracking()
                .Where(a => a.DeletedAt == null);

            if (salaryPackageId.HasValue)
            {
                query = query.Where(a => a.SalaryPackageId == salaryPackageId.Value);
            }

            if (employeeId.HasValue)
            {
                query = query.Where(a => a.EmployeeId == employeeId.Value);
            }

            var assignments = await query
                .Include(a => a.SalaryPackage)
                .Include(a => a.Employee)
                .Include(a => a.Contract)
                .Include(a => a.EmployeeSalary)
                .OrderByDescending(a => a.EffectiveDate)
                .ToListAsync();

            var result = assignments.Select(MapToReadDto);

            return Ok(result);
        }

        /// <summary>
        /// Get a salary package assignment by id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SalaryPackageAssignmentReadDto>> GetById(int id)
        {
            var assignment = await _db.SalaryPackageAssignments
                .AsNoTracking()
                .Where(a => a.DeletedAt == null && a.Id == id)
                .Include(a => a.SalaryPackage)
                .Include(a => a.Employee)
                .Include(a => a.Contract)
                .Include(a => a.EmployeeSalary)
                .FirstOrDefaultAsync();

            if (assignment == null)
                return NotFound(new { Message = "Assignment not found" });

            return Ok(MapToReadDto(assignment));
        }

        /// <summary>
        /// Get salary package assignments for an employee
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<IEnumerable<SalaryPackageAssignmentReadDto>>> GetByEmployeeId(int employeeId)
        {
            var employeeExists = await _db.Employees
                .AnyAsync(e => e.Id == employeeId && e.DeletedAt == null);

            if (!employeeExists)
                return NotFound(new { Message = "Employee not found" });

            var assignments = await _db.SalaryPackageAssignments
                .AsNoTracking()
                .Where(a => a.EmployeeId == employeeId && a.DeletedAt == null)
                .Include(a => a.SalaryPackage)
                .Include(a => a.Employee)
                .Include(a => a.Contract)
                .Include(a => a.EmployeeSalary)
                .OrderByDescending(a => a.EffectiveDate)
                .ToListAsync();

            return Ok(assignments.Select(MapToReadDto));
        }

        /// <summary>
        /// Apply a salary package to an employee contract
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SalaryPackageAssignmentReadDto>> Create([FromBody] SalaryPackageAssignmentCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            var package = await _db.SalaryPackages
                .Include(sp => sp.Items)
                .FirstOrDefaultAsync(sp => sp.Id == dto.SalaryPackageId && sp.DeletedAt == null);

            if (package == null)
                return NotFound(new { Message = "Salary package not found" });

            if (!string.Equals(package.Status, "published", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { Message = "Salary package must be published" });

            var employee = await _db.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null);

            if (employee == null)
                return NotFound(new { Message = "Employee not found" });

            var contract = await _db.EmployeeContracts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.ContractId && c.DeletedAt == null);

            if (contract == null)
                return NotFound(new { Message = "Contract not found" });

            if (contract.EmployeeId != dto.EmployeeId)
                return BadRequest(new { Message = "Contract does not belong to the employee" });

            if (contract.EndDate.HasValue && contract.EndDate.Value < dto.EffectiveDate)
                return BadRequest(new { Message = "Contract ends before the effective date" });

            if (package.CompanyId.HasValue && package.CompanyId.Value != employee.CompanyId)
                return BadRequest(new { Message = "Salary package does not match employee company" });

            var activeSalary = await _db.EmployeeSalaries
                .FirstOrDefaultAsync(es => es.ContractId == dto.ContractId && es.EmployeeId == dto.EmployeeId && es.DeletedAt == null && es.EndDate == null);

            var activeAssignment = await _db.SalaryPackageAssignments
                .FirstOrDefaultAsync(a => a.ContractId == dto.ContractId && a.EmployeeId == dto.EmployeeId && a.DeletedAt == null && a.EndDate == null);

            if (activeSalary != null)
            {
                if (dto.EffectiveDate <= activeSalary.EffectiveDate)
                    return BadRequest(new { Message = "Effective date must be after current salary effective date" });

                activeSalary.EndDate = dto.EffectiveDate;
                activeSalary.ModifiedAt = now;
                activeSalary.ModifiedBy = userId;

                if (activeAssignment != null)
                {
                    activeAssignment.EndDate = dto.EffectiveDate;
                    activeAssignment.ModifiedAt = now;
                    activeAssignment.ModifiedBy = userId;
                }
            }
            else if (activeAssignment != null)
            {
                if (dto.EffectiveDate <= activeAssignment.EffectiveDate)
                    return BadRequest(new { Message = "Effective date must be after current assignment effective date" });

                activeAssignment.EndDate = dto.EffectiveDate;
                activeAssignment.ModifiedAt = now;
                activeAssignment.ModifiedBy = userId;
            }

            var salary = new EmployeeSalary
            {
                EmployeeId = dto.EmployeeId,
                ContractId = dto.ContractId,
                BaseSalary = package.BaseSalary,
                EffectiveDate = dto.EffectiveDate,
                CreatedAt = now,
                CreatedBy = userId
            };

            var components = package.Items?
                .Where(i => i.DeletedAt == null)
                .Select(i => new EmployeeSalaryComponent
                {
                    ComponentType = i.Label,
                    Amount = i.DefaultValue,
                    IsTaxable = i.IsTaxable,
                    IsSocial = i.IsSocial,
                    IsCIMR = i.IsCIMR,
                    EffectiveDate = dto.EffectiveDate,
                    CreatedAt = now,
                    CreatedBy = userId
                })
                .ToList() ?? new List<EmployeeSalaryComponent>();

            salary.Components = components;

            var assignment = new SalaryPackageAssignment
            {
                SalaryPackageId = package.Id,
                EmployeeId = dto.EmployeeId,
                ContractId = dto.ContractId,
                EmployeeSalary = salary,
                EffectiveDate = dto.EffectiveDate,
                PackageVersion = package.Version, // Snapshot for audit/reproducibility
                CreatedAt = now,
                CreatedBy = userId
            };

            _db.EmployeeSalaries.Add(salary);
            _db.SalaryPackageAssignments.Add(assignment);

            // Lock the package to prevent modifications (immutability rule)
            if (!package.IsLocked)
            {
                package.IsLocked = true;
                package.ModifiedAt = now;
                package.ModifiedBy = userId;
            }

            await _db.SaveChangesAsync();

            if (activeSalary == null)
            {
                await _eventLogService.LogSimpleEventAsync(
                    dto.EmployeeId,
                    EmployeeEventLogService.EventNames.SalaryCreated,
                    null,
                    salary.BaseSalary.ToString("N2"),
                    userId);
            }
            else
            {
                await _eventLogService.LogSimpleEventAsync(
                    dto.EmployeeId,
                    EmployeeEventLogService.EventNames.SalaryUpdated,
                    activeSalary.BaseSalary.ToString("N2"),
                    salary.BaseSalary.ToString("N2"),
                    userId);
            }

            var created = await _db.SalaryPackageAssignments
                .AsNoTracking()
                .Where(a => a.Id == assignment.Id)
                .Include(a => a.SalaryPackage)
                .Include(a => a.Employee)
                .Include(a => a.Contract)
                .Include(a => a.EmployeeSalary)
                .FirstAsync();

            return CreatedAtAction(nameof(GetById), new { id = assignment.Id }, MapToReadDto(created));
        }

        /// <summary>
        /// Close an assignment (set end date)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<SalaryPackageAssignmentReadDto>> Update(int id, [FromBody] SalaryPackageAssignmentUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var assignment = await _db.SalaryPackageAssignments
                .Include(a => a.SalaryPackage)
                .Include(a => a.Employee)
                .Include(a => a.Contract)
                .Include(a => a.EmployeeSalary)
                .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

            if (assignment == null)
                return NotFound(new { Message = "Assignment not found" });

            if (!dto.EndDate.HasValue)
                return BadRequest(new { Message = "End date is required" });

            if (dto.EndDate.Value < assignment.EffectiveDate)
                return BadRequest(new { Message = "End date must be after effective date" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            assignment.EndDate = dto.EndDate;
            assignment.ModifiedAt = now;
            assignment.ModifiedBy = userId;

            if (assignment.EmployeeSalary != null)
            {
                assignment.EmployeeSalary.EndDate = dto.EndDate;
                assignment.EmployeeSalary.ModifiedAt = now;
                assignment.EmployeeSalary.ModifiedBy = userId;
            }

            await _db.SaveChangesAsync();

            var updated = await _db.SalaryPackageAssignments
                .AsNoTracking()
                .Where(a => a.Id == id)
                .Include(a => a.SalaryPackage)
                .Include(a => a.Employee)
                .Include(a => a.Contract)
                .Include(a => a.EmployeeSalary)
                .FirstAsync();

            return Ok(MapToReadDto(updated));
        }

        /// <summary>
        /// Soft delete an assignment
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var assignment = await _db.SalaryPackageAssignments
                .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

            if (assignment == null)
                return NotFound(new { Message = "Assignment not found" });

            var userId = User.GetUserId();
            var now = DateTimeOffset.UtcNow;

            assignment.DeletedAt = now;
            assignment.DeletedBy = userId;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        private static SalaryPackageAssignmentReadDto MapToReadDto(SalaryPackageAssignment assignment)
        {
            return new SalaryPackageAssignmentReadDto
            {
                Id = assignment.Id,
                SalaryPackageId = assignment.SalaryPackageId,
                SalaryPackageName = assignment.SalaryPackage?.Name ?? string.Empty,
                EmployeeId = assignment.EmployeeId,
                EmployeeFullName = assignment.Employee != null
                    ? $"{assignment.Employee.FirstName} {assignment.Employee.LastName}".Trim()
                    : string.Empty,
                ContractId = assignment.ContractId,
                EmployeeSalaryId = assignment.EmployeeSalaryId,
                EffectiveDate = assignment.EffectiveDate,
                EndDate = assignment.EndDate,
                CreatedAt = assignment.CreatedAt.DateTime,
                PackageVersion = assignment.PackageVersion
            };
        }
    }
}
