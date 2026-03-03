using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Authorization;
using payzen_backend.Extensions;

namespace payzen_backend.Controllers.Employees
{
    [Route("api/employee-salaries")]
    [ApiController]
    [Authorize]
    public class EmployeeSalariesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EmployeeSalariesController(AppDbContext db) => _db = db;

        /// <summary>
        /// R�cup�re tous les salaires
        /// </summary>
        [HttpGet]
        //[HasPermission("READ_EMPLOYEE_SALARIES")]
        public async Task<ActionResult<IEnumerable<EmployeeSalaryReadDto>>> GetAll()
        {
            var salaries = await _db.EmployeeSalaries
                .AsNoTracking()
                .Where(es => es.DeletedAt == null)
                .Include(es => es.Employee)
                .Include(es => es.Contract)
                .OrderByDescending(es => es.EffectiveDate)
                .ToListAsync();

            var result = salaries.Select(es => new EmployeeSalaryReadDto
            {
                Id = es.Id,
                EmployeeId = es.EmployeeId,
                EmployeeFullName = $"{es.Employee?.FirstName} {es.Employee?.LastName}",
                ContractId = es.ContractId,
                BaseSalary = es.BaseSalary,
                EffectiveDate = es.EffectiveDate,
                EndDate = es.EndDate,
                CreatedAt = es.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re un salaire par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission("VIEW_EMPLOYEE_SALARY")]
        public async Task<ActionResult<EmployeeSalaryReadDto>> GetById(int id)
        {
            var salary = await _db.EmployeeSalaries
                .AsNoTracking()
                .Where(es => es.DeletedAt == null)
                .Include(es => es.Employee)
                .Include(es => es.Contract)
                .FirstOrDefaultAsync(es => es.Id == id);

            if (salary == null)
                return NotFound(new { Message = "Salaire non trouv�" });

            var result = new EmployeeSalaryReadDto
            {
                Id = salary.Id,
                EmployeeId = salary.EmployeeId,
                EmployeeFullName = $"{salary.Employee?.FirstName} {salary.Employee?.LastName}",
                ContractId = salary.ContractId,
                BaseSalary = salary.BaseSalary,
                EffectiveDate = salary.EffectiveDate,
                EndDate = salary.EndDate,
                CreatedAt = salary.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re tous les salaires d'un employ�
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        //[HasPermission("VIEW_EMPLOYEE_SALARY")]
        public async Task<ActionResult<IEnumerable<EmployeeSalaryReadDto>>> GetByEmployeeId(int employeeId)
        {
            var employeeExists = await _db.Employees.AnyAsync(e => e.Id == employeeId && e.DeletedAt == null);
            if (!employeeExists)
                return NotFound(new { Message = "Employ� non trouv�" });

            var salaries = await _db.EmployeeSalaries
                .AsNoTracking()
                .Where(es => es.EmployeeId == employeeId && es.DeletedAt == null)
                .Include(es => es.Employee)
                .Include(es => es.Contract)
                .OrderByDescending(es => es.EffectiveDate)
                .ToListAsync();

            var result = salaries.Select(es => new EmployeeSalaryReadDto
            {
                Id = es.Id,
                EmployeeId = es.EmployeeId,
                EmployeeFullName = $"{es.Employee?.FirstName} {es.Employee?.LastName}",
                ContractId = es.ContractId,
                BaseSalary = es.BaseSalary,
                EffectiveDate = es.EffectiveDate,
                EndDate = es.EndDate,
                CreatedAt = es.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re tous les salaires d'un contrat
        /// </summary>
        [HttpGet("contract/{contractId}")]
        //[HasPermission("VIEW_EMPLOYEE_SALARY")]
        public async Task<ActionResult<IEnumerable<EmployeeSalaryReadDto>>> GetByContractId(int contractId)
        {
            var contractExists = await _db.EmployeeContracts.AnyAsync(ec => ec.Id == contractId && ec.DeletedAt == null);
            if (!contractExists)
                return NotFound(new { Message = "Contrat non trouv�" });

            var salaries = await _db.EmployeeSalaries
                .AsNoTracking()
                .Where(es => es.ContractId == contractId && es.DeletedAt == null)
                .Include(es => es.Employee)
                .Include(es => es.Contract)
                .OrderByDescending(es => es.EffectiveDate)
                .ToListAsync();

            var result = salaries.Select(es => new EmployeeSalaryReadDto
            {
                Id = es.Id,
                EmployeeId = es.EmployeeId,
                EmployeeFullName = $"{es.Employee?.FirstName} {es.Employee?.LastName}",
                ContractId = es.ContractId,
                BaseSalary = es.BaseSalary,
                EffectiveDate = es.EffectiveDate,
                EndDate = es.EndDate,
                CreatedAt = es.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Cr�e un nouveau salaire
        /// </summary>
        [HttpPost]
        //[HasPermission("CREATE_EMPLOYEE_SALARY")]
        public async Task<ActionResult<EmployeeSalaryReadDto>> Create([FromBody] EmployeeSalaryCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            // Validations
            var employeeExists = await _db.Employees.AnyAsync(e => e.Id == dto.EmployeeId && e.DeletedAt == null);
            if (!employeeExists)
                return NotFound(new { Message = "Employ� non trouv�" });

            var contractExists = await _db.EmployeeContracts.AnyAsync(ec => ec.Id == dto.ContractId && ec.DeletedAt == null);
            if (!contractExists)
                return NotFound(new { Message = "Contrat non trouv�" });

            // V�rifier que le contrat appartient � l'employ�
            var contract = await _db.EmployeeContracts.FindAsync(dto.ContractId);
            if (contract?.EmployeeId != dto.EmployeeId)
                return BadRequest(new { Message = "Le contrat ne correspond pas � l'employ� sp�cifi�" });

            // Validation des dates
            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.EffectiveDate)
                return BadRequest(new { Message = "La date de fin doit �tre apr�s la date d'effet" });

            var salary = new EmployeeSalary
            {
                EmployeeId = dto.EmployeeId,
                ContractId = dto.ContractId,
                BaseSalary = dto.BaseSalary,
                EffectiveDate = dto.EffectiveDate,
                EndDate = dto.EndDate,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.EmployeeSalaries.Add(salary);
            await _db.SaveChangesAsync();

            var createdSalary = await _db.EmployeeSalaries
                .AsNoTracking()
                .Include(es => es.Employee)
                .Include(es => es.Contract)
                .FirstAsync(es => es.Id == salary.Id);

            var result = new EmployeeSalaryReadDto
            {
                Id = createdSalary.Id,
                EmployeeId = createdSalary.EmployeeId,
                EmployeeFullName = $"{createdSalary.Employee?.FirstName} {createdSalary.Employee?.LastName}",
                ContractId = createdSalary.ContractId,
                BaseSalary = createdSalary.BaseSalary,
                EffectiveDate = createdSalary.EffectiveDate,
                EndDate = createdSalary.EndDate,
                CreatedAt = createdSalary.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = salary.Id }, result);
        }

        /// <summary>
        /// Met � jour un salaire
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("UPDATE_EMPLOYEE_SALARY")]
        public async Task<ActionResult<EmployeeSalaryReadDto>> Update(int id, [FromBody] EmployeeSalaryUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var salary = await _db.EmployeeSalaries.FirstOrDefaultAsync(es => es.Id == id && es.DeletedAt == null);
            if (salary == null)
                return NotFound(new { Message = "Salaire non trouv�" });

            if (dto.BaseSalary.HasValue)
                salary.BaseSalary = dto.BaseSalary.Value;

            if (dto.EffectiveDate.HasValue)
                salary.EffectiveDate = dto.EffectiveDate.Value;

            if (dto.EndDate.HasValue)
            {
                if (dto.EndDate.Value < salary.EffectiveDate)
                    return BadRequest(new { Message = "La date de fin doit �tre apr�s la date d'effet" });
                
                salary.EndDate = dto.EndDate;
            }

            salary.DeletedAt = DateTimeOffset.UtcNow;
            salary.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var updatedSalary = await _db.EmployeeSalaries
                .AsNoTracking()
                .Include(es => es.Employee)
                .Include(es => es.Contract)
                .FirstAsync(es => es.Id == id);

            var result = new EmployeeSalaryReadDto
            {
                Id = updatedSalary.Id,
                EmployeeId = updatedSalary.EmployeeId,
                EmployeeFullName = $"{updatedSalary.Employee?.FirstName} {updatedSalary.Employee?.LastName}",
                ContractId = updatedSalary.ContractId,
                BaseSalary = updatedSalary.BaseSalary,
                EffectiveDate = updatedSalary.EffectiveDate,
                EndDate = updatedSalary.EndDate,
                CreatedAt = updatedSalary.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Supprime un salaire (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("DELETE_EMPLOYEE_SALARY")]
        public async Task<IActionResult> Delete(int id)
        {
            var salary = await _db.EmployeeSalaries.FirstOrDefaultAsync(es => es.Id == id && es.DeletedAt == null);
            if (salary == null)
                return NotFound(new { Message = "Salaire non trouv�" });

            // V�rifier si le salaire a des composants
            var hasComponents = await _db.EmployeeSalaryComponents.AnyAsync(esc => esc.EmployeeSalaryId == id && esc.DeletedAt == null);
            if (hasComponents)
                return BadRequest(new { Message = "Impossible de supprimer ce salaire car il contient des composants" });

            salary.DeletedAt = DateTimeOffset.UtcNow;
            salary.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}