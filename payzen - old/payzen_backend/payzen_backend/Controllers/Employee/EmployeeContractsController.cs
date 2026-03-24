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
    [Route("api/employee-contracts")]
    [ApiController]
    [Authorize]
    public class EmployeeContractsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EmployeeContractsController(AppDbContext db) => _db = db;

        /// <summary>
        /// R�cup�re tous les contrats d'employ�s
        /// </summary>
        [HttpGet]
        //[HasPermission("READ_EMPLOYEE_CONTRACTS")]
        public async Task<ActionResult<IEnumerable<EmployeeContractReadDto>>> GetAll()
        {
            var contracts = await _db.EmployeeContracts
                .AsNoTracking()
                .Where(ec => ec.DeletedAt == null)
                .Include(ec => ec.Employee)
                .Include(ec => ec.Company)
                .Include(ec => ec.JobPosition)
                .Include(ec => ec.ContractType)
                .OrderByDescending(ec => ec.StartDate)
                .ToListAsync();

            var result = contracts.Select(ec => new EmployeeContractReadDto
            {
                Id = ec.Id,
                EmployeeId = ec.EmployeeId,
                EmployeeFullName = $"{ec.Employee?.FirstName} {ec.Employee?.LastName}",
                CompanyId = ec.CompanyId,
                CompanyName = ec.Company?.CompanyName ?? "",
                JobPositionId = ec.JobPositionId,
                JobPositionName = ec.JobPosition?.Name ?? "",
                ContractTypeId = ec.ContractTypeId,
                ContractTypeName = ec.ContractType?.ContractTypeName ?? "",
                StartDate = ec.StartDate,
                EndDate = ec.EndDate,
                CreatedAt = ec.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re un contrat par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission("VIEW_EMPLOYEE_CONTRACT")]
        public async Task<ActionResult<EmployeeContractReadDto>> GetById(int id)
        {
            var contract = await _db.EmployeeContracts
                .AsNoTracking()
                .Where(ec => ec.DeletedAt == null)
                .Include(ec => ec.Employee)
                .Include(ec => ec.Company)
                .Include(ec => ec.JobPosition)
                .Include(ec => ec.ContractType)
                .FirstOrDefaultAsync(ec => ec.Id == id);

            if (contract == null)
                return NotFound(new { Message = "Contrat non trouv�" });

            var result = new EmployeeContractReadDto
            {
                Id = contract.Id,
                EmployeeId = contract.EmployeeId,
                EmployeeFullName = $"{contract.Employee?.FirstName} {contract.Employee?.LastName}",
                CompanyId = contract.CompanyId,
                CompanyName = contract.Company?.CompanyName ?? "",
                JobPositionId = contract.JobPositionId,
                JobPositionName = contract.JobPosition?.Name ?? "",
                ContractTypeId = contract.ContractTypeId,
                ContractTypeName = contract.ContractType?.ContractTypeName ?? "",
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                CreatedAt = contract.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re tous les contrats d'un employ�
        /// </summary>
        [HttpGet("employee/{employeeId}")]
        //[HasPermission("VIEW_EMPLOYEE_CONTRACT")]
        public async Task<ActionResult<IEnumerable<EmployeeContractReadDto>>> GetByEmployeeId(int employeeId)
        {
            var employeeExists = await _db.Employees.AnyAsync(e => e.Id == employeeId && e.DeletedAt == null);
            if (!employeeExists)
                return NotFound(new { Message = "Employ� non trouv�" });

            var contracts = await _db.EmployeeContracts
                .AsNoTracking()
                .Where(ec => ec.EmployeeId == employeeId && ec.DeletedAt == null)
                .Include(ec => ec.Employee)
                .Include(ec => ec.Company)
                .Include(ec => ec.JobPosition)
                .Include(ec => ec.ContractType)
                .OrderByDescending(ec => ec.StartDate)
                .ToListAsync();

            var result = contracts.Select(ec => new EmployeeContractReadDto
            {
                Id = ec.Id,
                EmployeeId = ec.EmployeeId,
                EmployeeFullName = $"{ec.Employee?.FirstName} {ec.Employee?.LastName}",
                CompanyId = ec.CompanyId,
                CompanyName = ec.Company?.CompanyName ?? "",
                JobPositionId = ec.JobPositionId,
                JobPositionName = ec.JobPosition?.Name ?? "",
                ContractTypeId = ec.ContractTypeId,
                ContractTypeName = ec.ContractType?.ContractTypeName ?? "",
                StartDate = ec.StartDate,
                EndDate = ec.EndDate,
                CreatedAt = ec.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Cr�e un nouveau contrat
        /// </summary>
        [HttpPost]
        //[HasPermission("CREATE_EMPLOYEE_CONTRACT")]
        public async Task<ActionResult<EmployeeContractReadDto>> Create([FromBody] EmployeeContractCreateDto dto)
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

            var companyExists = await _db.Companies.AnyAsync(c => c.Id == dto.CompanyId && c.DeletedAt == null);
            if (!companyExists)
                return NotFound(new { Message = "Soci�t� non trouv�e" });

            var jobPositionExists = await _db.JobPositions.AnyAsync(jp => jp.Id == dto.JobPositionId && jp.DeletedAt == null);
            if (!jobPositionExists)
                return NotFound(new { Message = "Poste non trouv�" });

            var contractTypeExists = await _db.ContractTypes.AnyAsync(ct => ct.Id == dto.ContractTypeId && ct.DeletedAt == null);
            if (!contractTypeExists)
                return NotFound(new { Message = "Type de contrat non trouv�" });

            // Validation des dates
            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
                return BadRequest(new { Message = "La date de fin doit �tre apr�s la date de d�but" });

            var contract = new EmployeeContract
            {
                EmployeeId = dto.EmployeeId,
                CompanyId = dto.CompanyId,
                JobPositionId = dto.JobPositionId,
                ContractTypeId = dto.ContractTypeId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.EmployeeContracts.Add(contract);
            await _db.SaveChangesAsync();

            var createdContract = await _db.EmployeeContracts
                .AsNoTracking()
                .Include(ec => ec.Employee)
                .Include(ec => ec.Company)
                .Include(ec => ec.JobPosition)
                .Include(ec => ec.ContractType)
                .FirstAsync(ec => ec.Id == contract.Id);

            var result = new EmployeeContractReadDto
            {
                Id = createdContract.Id,
                EmployeeId = createdContract.EmployeeId,
                EmployeeFullName = $"{createdContract.Employee?.FirstName} {createdContract.Employee?.LastName}",
                CompanyId = createdContract.CompanyId,
                CompanyName = createdContract.Company?.CompanyName ?? "",
                JobPositionId = createdContract.JobPositionId,
                JobPositionName = createdContract.JobPosition?.Name ?? "",
                ContractTypeId = createdContract.ContractTypeId,
                ContractTypeName = createdContract.ContractType?.ContractTypeName ?? "",
                StartDate = createdContract.StartDate,
                EndDate = createdContract.EndDate,
                CreatedAt = createdContract.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = contract.Id }, result);
        }

        /// <summary>
        /// Met � jour un contrat
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("UPDATE_EMPLOYEE_CONTRACT")]
        public async Task<ActionResult<EmployeeContractReadDto>> Update(int id, [FromBody] EmployeeContractUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var contract = await _db.EmployeeContracts.FirstOrDefaultAsync(ec => ec.Id == id && ec.DeletedAt == null);
            if (contract == null)
                return NotFound(new { Message = "Contrat non trouv�" });

            if (dto.JobPositionId.HasValue)
            {
                var jobPositionExists = await _db.JobPositions.AnyAsync(jp => jp.Id == dto.JobPositionId.Value && jp.DeletedAt == null);
                if (!jobPositionExists)
                    return NotFound(new { Message = "Poste non trouv�" });
                
                contract.JobPositionId = dto.JobPositionId.Value;
            }

            if (dto.ContractTypeId.HasValue)
            {
                var contractTypeExists = await _db.ContractTypes.AnyAsync(ct => ct.Id == dto.ContractTypeId.Value && ct.DeletedAt == null);
                if (!contractTypeExists)
                    return NotFound(new { Message = "Type de contrat non trouv�" });
                
                contract.ContractTypeId = dto.ContractTypeId.Value;
            }

            if (dto.StartDate.HasValue)
                contract.StartDate = dto.StartDate.Value;

            if (dto.EndDate.HasValue)
            {
                if (dto.EndDate.Value < contract.StartDate)
                    return BadRequest(new { Message = "La date de fin doit �tre apr�s la date de d�but" });
                
                contract.EndDate = dto.EndDate;
            }

            contract.ModifiedAt = DateTimeOffset.UtcNow;
            contract.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var updatedContract = await _db.EmployeeContracts
                .AsNoTracking()
                .Include(ec => ec.Employee)
                .Include(ec => ec.Company)
                .Include(ec => ec.JobPosition)
                .Include(ec => ec.ContractType)
                .FirstAsync(ec => ec.Id == id);

            var result = new EmployeeContractReadDto
            {
                Id = updatedContract.Id,
                EmployeeId = updatedContract.EmployeeId,
                EmployeeFullName = $"{updatedContract.Employee?.FirstName} {updatedContract.Employee?.LastName}",
                CompanyId = updatedContract.CompanyId,
                CompanyName = updatedContract.Company?.CompanyName ?? "",
                JobPositionId = updatedContract.JobPositionId,
                JobPositionName = updatedContract.JobPosition?.Name ?? "",
                ContractTypeId = updatedContract.ContractTypeId,
                ContractTypeName = updatedContract.ContractType?.ContractTypeName ?? "",
                StartDate = updatedContract.StartDate,
                EndDate = updatedContract.EndDate,
                CreatedAt = updatedContract.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Supprime un contrat (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("DELETE_EMPLOYEE_CONTRACT")]
        public async Task<IActionResult> Delete(int id)
        {
            var contract = await _db.EmployeeContracts.FirstOrDefaultAsync(ec => ec.Id == id && ec.DeletedAt == null);
            if (contract == null)
                return NotFound(new { Message = "Contrat non trouv�" });

            // V�rifier si le contrat a des salaires associ�s
            var hasSalaries = await _db.EmployeeSalaries.AnyAsync(es => es.ContractId == id && es.DeletedAt == null);
            if (hasSalaries)
                return BadRequest(new { Message = "Impossible de supprimer ce contrat car il contient des salaires" });

            contract.DeletedAt = DateTimeOffset.UtcNow;
            contract.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
