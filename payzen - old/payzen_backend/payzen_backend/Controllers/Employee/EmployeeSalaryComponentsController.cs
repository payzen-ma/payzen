using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Authorization;
using payzen_backend.Extensions;
using payzen_backend.Services;

namespace payzen_backend.Controllers.Employees
{
    [Route("api/employee-salary-components")]
    [ApiController]
    [Authorize]
    public class EmployeeSalaryComponentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmployeeEventLogService _eventLogService;

        public EmployeeSalaryComponentsController(AppDbContext db, EmployeeEventLogService eventLogService)
        {
            _db = db;
            _eventLogService = eventLogService;
        }

        /// <summary>
        /// R�cup�re tous les composants de salaire
        /// </summary>
        [HttpGet]
        //[HasPermission("READ_EMPLOYEE_SALARY_COMPONENTS")]
        public async Task<ActionResult<IEnumerable<EmployeeSalaryComponentReadDto>>> GetAll()
        {
            var components = await _db.EmployeeSalaryComponents
                .AsNoTracking()
                .Where(esc => esc.DeletedAt == null)
                .Include(esc => esc.EmployeeSalary)
                .OrderByDescending(esc => esc.EffectiveDate)
                .ToListAsync();

            var result = components.Select(esc => new EmployeeSalaryComponentReadDto
            {
                Id = esc.Id,
                EmployeeSalaryId = esc.EmployeeSalaryId,
                ComponentType = esc.ComponentType,
                Amount = esc.Amount,
                IsTaxable = esc.IsTaxable == true,
                EffectiveDate = esc.EffectiveDate,
                EndDate = esc.EndDate,
                CreatedAt = esc.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>        /// Liste hardcodée des primes / indemités non imposables reconnues par le moteur de paie.
        /// Utilisée par le frontend pour proposer une liste déroulante lors de l’ajout d’un élément de salaire.
        /// </summary>
        [HttpGet("non-imposables")]
        [AllowAnonymous]
        public IActionResult GetNonImposables()
        {
            var list = new[]
            {
                new { code = "TRANSPORT",       label = "Prime de transport" },
                new { code = "KILOMETRIQUE",    label = "Indemité kilométrique" },
                new { code = "TOURNEE",         label = "Indemité de tournée" },
                new { code = "REPRESENTATION",  label = "Indemité de représentation" },
                new { code = "PANIER",          label = "Prime de panier" },
                new { code = "CAISSE",          label = "Indemité de caisse" },
                new { code = "SALISSURE",       label = "Indemité de salissure" },
                new { code = "LAIT",            label = "Indemité de lait" },
                new { code = "OUTILLAGE",       label = "Prime d\u2019outillage" },
                new { code = "AIDE_MEDICALE",   label = "Aide médicale" },
                new { code = "GRATIF_SOCIALE",  label = "Gratification sociale" },
            };
            return Ok(list);
        }

        /// <summary>        /// R�cup�re un composant par ID
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission("VIEW_EMPLOYEE_SALARY_COMPONENT")]
        public async Task<ActionResult<EmployeeSalaryComponentReadDto>> GetById(int id)
        {
            var component = await _db.EmployeeSalaryComponents
                .AsNoTracking()
                .Where(esc => esc.DeletedAt == null)
                .Include(esc => esc.EmployeeSalary)
                .FirstOrDefaultAsync(esc => esc.Id == id);

            if (component == null)
                return NotFound(new { Message = "Composant non trouv�" });

            var result = new EmployeeSalaryComponentReadDto
            {
                Id = component.Id,
                EmployeeSalaryId = component.EmployeeSalaryId,
                ComponentType = component.ComponentType,
                Amount = component.Amount,
                IsTaxable = component.IsTaxable == true,
                EffectiveDate = component.EffectiveDate,
                EndDate = component.EndDate,
                CreatedAt = component.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// R�cup�re tous les composants d'un salaire
        /// </summary>
        [HttpGet("salary/{salaryId}")]
        //[HasPermission("VIEW_EMPLOYEE_SALARY_COMPONENT")]
        public async Task<ActionResult<IEnumerable<EmployeeSalaryComponentReadDto>>> GetBySalaryId(int salaryId)
        {
            var salaryExists = await _db.EmployeeSalaries.AnyAsync(es => es.Id == salaryId && es.DeletedAt == null);
            if (!salaryExists)
                return NotFound(new { Message = "Salaire non trouv�" });

            var components = await _db.EmployeeSalaryComponents
                .AsNoTracking()
                .Where(esc => esc.EmployeeSalaryId == salaryId && esc.DeletedAt == null)
                .Include(esc => esc.EmployeeSalary)
                .OrderByDescending(esc => esc.EffectiveDate)
                .ToListAsync();

            var result = components.Select(esc => new EmployeeSalaryComponentReadDto
            {
                Id = esc.Id,
                EmployeeSalaryId = esc.EmployeeSalaryId,
                ComponentType = esc.ComponentType,
                Amount = esc.Amount,
                IsTaxable = esc.IsTaxable == true,
                EffectiveDate = esc.EffectiveDate,
                EndDate = esc.EndDate,
                CreatedAt = esc.CreatedAt.DateTime
            });

            return Ok(result);
        }

        /// <summary>
        /// Cr�e un nouveau composant de salaire
        /// </summary>
        [HttpPost]
        //[HasPermission("CREATE_EMPLOYEE_SALARY_COMPONENT")]
        public async Task<ActionResult<EmployeeSalaryComponentReadDto>> Create([FromBody] EmployeeSalaryComponentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var salary = await _db.EmployeeSalaries
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(es => es.Id == dto.EmployeeSalaryId && es.DeletedAt == null);

            if (salary == null)
                return NotFound(new { Message = "Salaire non trouv�" });

            // Validation des dates
            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.EffectiveDate)
                return BadRequest(new { Message = "La date de fin doit �tre apr�s la date d'effet" });

            var component = new EmployeeSalaryComponent
            {
                EmployeeSalaryId = dto.EmployeeSalaryId,
                ComponentType = dto.ComponentType,
                Amount = dto.Amount,
                IsTaxable = dto.IsTaxable,
                IsSocial = true,
                IsCIMR = false,
                EffectiveDate = dto.EffectiveDate,
                EndDate = dto.EndDate,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.EmployeeSalaryComponents.Add(component);
            await _db.SaveChangesAsync();

            // Log event
            var createdComponent = await _db.EmployeeSalaryComponents
                .AsNoTracking()
                .Include(esc => esc.EmployeeSalary)
                .FirstAsync(esc => esc.Id == component.Id);

            var result = new EmployeeSalaryComponentReadDto
            {
                Id = createdComponent.Id,
                EmployeeSalaryId = createdComponent.EmployeeSalaryId,
                ComponentType = createdComponent.ComponentType,
                Amount = createdComponent.Amount,
                IsTaxable = createdComponent.IsTaxable == true,
                EffectiveDate = createdComponent.EffectiveDate,
                EndDate = createdComponent.EndDate,
                CreatedAt = createdComponent.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = component.Id }, result);
        }

        /// <summary>
        /// Met � jour un composant
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission("UPDATE_EMPLOYEE_SALARY_COMPONENT")]
        public async Task<ActionResult<EmployeeSalaryComponentReadDto>> Update(int id, [FromBody] EmployeeSalaryComponentUpdateDto dto)
        {
            Console.WriteLine("============UPDATE SALARY COMPONENET");
            if (!ModelState.IsValid)
                return BadRequest(new 
                { 
                    Message = "Donn�es invalides",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });

            var component = await _db.EmployeeSalaryComponents.FirstOrDefaultAsync(esc => esc.Id == id && esc.DeletedAt == null);
            if (component == null)
                return NotFound(new { Message = "Composant non trouv�" });

            if (dto.ComponentType != null)
                component.ComponentType = dto.ComponentType;

            if (dto.Amount.HasValue)
                component.Amount = dto.Amount.Value;

            if (dto.IsTaxable.HasValue)
                component.IsTaxable = dto.IsTaxable.Value;

            if (dto.EffectiveDate.HasValue)
                component.EffectiveDate = dto.EffectiveDate.Value;

            if (dto.EndDate.HasValue)
            {
                if (dto.EndDate.Value < component.EffectiveDate)
                    return BadRequest(new { Message = "La date de fin doit �tre apr�s la date d'effet" });
                
                component.EndDate = dto.EndDate;
            }

            component.ModifiedAt = DateTimeOffset.UtcNow;
            component.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var updatedComponent = await _db.EmployeeSalaryComponents
                .AsNoTracking()
                .Include(esc => esc.EmployeeSalary)
                .FirstAsync(esc => esc.Id == id);

            var result = new EmployeeSalaryComponentReadDto
            {
                Id = updatedComponent.Id,
                EmployeeSalaryId = updatedComponent.EmployeeSalaryId,
                ComponentType = updatedComponent.ComponentType,
                Amount = updatedComponent.Amount,
                IsTaxable = updatedComponent.IsTaxable == true,
                EffectiveDate = updatedComponent.EffectiveDate,
                EndDate = updatedComponent.EndDate,
                CreatedAt = updatedComponent.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Cl�ture un composant et cr�e une nouvelle version (nouvel objet) avec dates r�vis�es.
        /// Ancien composant : EndDate mis � jour + ModifiedAt
        /// Nouveau composant : reprend les infos du pr�c�dent sauf EffectiveDate modifi�e.
        /// </summary>
        [HttpPost("revise/{id}")]
        public async Task<IActionResult> Revise(int id, [FromBody] EmployeeSalaryComponentUpdateDto dto)
        {
            // 1) Recuperation de l'ancien composant
            var oldComponent = await _db.EmployeeSalaryComponents
                .FirstOrDefaultAsync(esc => esc.Id == id && esc.DeletedAt == null);

            if (oldComponent == null)
                return NotFound(new { Message = "Composant non trouv�" });

            // Validation Date de fin
            if (dto.EffectiveDate.HasValue && dto.EffectiveDate.Value <= oldComponent.EffectiveDate)
                return BadRequest(new { Message = "La nouvelle date effective doit �tre sup�rieure � l'ancienne" });

            // 2) Fermeture de l'ancien composant
            oldComponent.EndDate = dto.EffectiveDate ?? DateTimeOffset.UtcNow.DateTime;
            oldComponent.ModifiedAt = DateTimeOffset.UtcNow;
            oldComponent.ModifiedBy = User.GetUserId();

            oldComponent.DeletedAt = DateTimeOffset.UtcNow;
            oldComponent.DeletedBy = User.GetUserId();

            // 3) Cr�ation du nouveau composant (r�vision)
            var newComponent = new EmployeeSalaryComponent
            {
                EmployeeSalaryId = oldComponent.EmployeeSalaryId,
                ComponentType = dto.ComponentType ?? oldComponent.ComponentType,
                Amount = dto.Amount ?? oldComponent.Amount,
                IsTaxable = oldComponent.IsTaxable,
                IsSocial = oldComponent.IsSocial,
                IsCIMR = oldComponent.IsCIMR,
                EffectiveDate = dto.EffectiveDate ?? DateTimeOffset.UtcNow.DateTime,
                EndDate = dto.EndDate, // facultatif
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = User.GetUserId()
            };

            await _db.EmployeeSalaryComponents.AddAsync(newComponent);
            await _db.SaveChangesAsync();

            // 4) Retour du nouveau composant
            return Ok(new
            {
                Message = "Composant r�vis� avec succ�s",
                OldVersion = new
                {
                    oldComponent.Id,
                    oldComponent.EndDate,
                    oldComponent.ModifiedAt
                },
                NewVersion = new
                {
                    newComponent.Id,
                    newComponent.Amount,
                    newComponent.EffectiveDate,
                    newComponent.EndDate
                }
            });
        }

        /// <summary>
        /// Supprime un composant (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission("DELETE_EMPLOYEE_SALARY_COMPONENT")]
        public async Task<IActionResult> Delete(int id)
        {
            var component = await _db.EmployeeSalaryComponents.FirstOrDefaultAsync(esc => esc.Id == id && esc.DeletedAt == null);
            if (component == null)
                return NotFound(new { Message = "Composant non trouv�" });

            component.DeletedAt = DateTimeOffset.UtcNow;
            component.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
