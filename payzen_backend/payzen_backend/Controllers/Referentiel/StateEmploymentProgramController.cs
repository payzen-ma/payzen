using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Referentiel;
using payzen_backend.Models.Referentiel.Dtos;

namespace payzen_backend.Controllers.Referentiel
{
    [Route("api/state-employment-programs")]
    [ApiController]
    [Authorize]
    public class StateEmploymentProgramController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StateEmploymentProgramController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// R�cup�re tous les programmes d'emploi �tatiques actifs
        /// GET /api/state-employment-programs
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<StateEmploymentProgramReadDto>>> GetAll()
        {
            var programs = await _db.StateEmploymentPrograms
                .AsNoTracking()
                .Where(s => s.DeletedAt == null)
                .OrderBy(s => s.Code)
                .Select(s => new StateEmploymentProgramReadDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    IsCnssEmployeeExempt = s.IsCnssEmployeeExempt,
                    IsCnssEmployerExempt = s.IsCnssEmployerExempt,
                    IsIrExempt = s.IsIrExempt,
                    MaxDurationMonths = s.MaxDurationMonths,
                    SalaryCeiling = s.SalaryCeiling,
                    CreatedAt = s.CreatedAt.DateTime
                })
                .ToListAsync();

            return Ok(programs);
        }

        /// <summary>
        /// R�cup�re un programme d'emploi �tatique par ID
        /// GET /api/state-employment-programs/{id}
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<StateEmploymentProgramReadDto>> GetById(int id)
        {
            var program = await _db.StateEmploymentPrograms
                .AsNoTracking()
                .Where(s => s.Id == id && s.DeletedAt == null)
                .Select(s => new StateEmploymentProgramReadDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    IsCnssEmployeeExempt = s.IsCnssEmployeeExempt,
                    IsCnssEmployerExempt = s.IsCnssEmployerExempt,
                    IsIrExempt = s.IsIrExempt,
                    MaxDurationMonths = s.MaxDurationMonths,
                    SalaryCeiling = s.SalaryCeiling,
                    CreatedAt = s.CreatedAt.DateTime
                })
                .FirstOrDefaultAsync();

            if (program == null)
                return NotFound(new { Message = "Programme d'emploi �tatique non trouv�" });

            return Ok(program);
        }

        /// <summary>
        /// Cr�e un nouveau programme d'emploi �tatique
        /// POST /api/state-employment-programs
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<StateEmploymentProgramReadDto>> Create([FromBody] StateEmploymentProgramCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // V�rifier l'unicit� du code
            if (await _db.StateEmploymentPrograms.AnyAsync(s => s.Code == dto.Code && s.DeletedAt == null))
                return Conflict(new { Message = "Un programme avec ce code existe d�j�" });

            var program = new StateEmploymentProgram
            {
                Code = dto.Code.Trim().ToUpper(),
                Name = dto.Name.Trim(),
                IsCnssEmployeeExempt = dto.IsCnssEmployeeExempt,
                IsCnssEmployerExempt = dto.IsCnssEmployerExempt,
                IsIrExempt = dto.IsIrExempt,
                MaxDurationMonths = dto.MaxDurationMonths,
                SalaryCeiling = dto.SalaryCeiling,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.StateEmploymentPrograms.Add(program);
            await _db.SaveChangesAsync();

            var result = new StateEmploymentProgramReadDto
            {
                Id = program.Id,
                Code = program.Code,
                Name = program.Name,
                IsCnssEmployeeExempt = program.IsCnssEmployeeExempt,
                IsCnssEmployerExempt = program.IsCnssEmployerExempt,
                IsIrExempt = program.IsIrExempt,
                MaxDurationMonths = program.MaxDurationMonths,
                SalaryCeiling = program.SalaryCeiling,
                CreatedAt = program.CreatedAt.DateTime
            };

            return CreatedAtAction(nameof(GetById), new { id = program.Id }, result);
        }

        /// <summary>
        /// Met � jour un programme d'emploi �tatique
        /// PUT /api/state-employment-programs/{id}
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<StateEmploymentProgramReadDto>> Update(int id, [FromBody] StateEmploymentProgramUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var program = await _db.StateEmploymentPrograms
                .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (program == null)
                return NotFound(new { Message = "Programme d'emploi �tatique non trouv�" });

            bool hasChanges = false;

            // Mise � jour du code
            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim().ToUpper() != program.Code)
            {
                var newCode = dto.Code.Trim().ToUpper();
                if (await _db.StateEmploymentPrograms.AnyAsync(s => s.Code == newCode && s.Id != id && s.DeletedAt == null))
                    return Conflict(new { Message = "Un programme avec ce code existe d�j�" });

                program.Code = newCode;
                hasChanges = true;
            }

            // Mise � jour du nom
            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Trim() != program.Name)
            {
                program.Name = dto.Name.Trim();
                hasChanges = true;
            }

            // Mise � jour des exemptions
            if (dto.IsCnssEmployeeExempt.HasValue && dto.IsCnssEmployeeExempt != program.IsCnssEmployeeExempt)
            {
                program.IsCnssEmployeeExempt = dto.IsCnssEmployeeExempt.Value;
                hasChanges = true;
            }

            if (dto.IsCnssEmployerExempt.HasValue && dto.IsCnssEmployerExempt != program.IsCnssEmployerExempt)
            {
                program.IsCnssEmployerExempt = dto.IsCnssEmployerExempt.Value;
                hasChanges = true;
            }

            if (dto.IsIrExempt.HasValue && dto.IsIrExempt != program.IsIrExempt)
            {
                program.IsIrExempt = dto.IsIrExempt.Value;
                hasChanges = true;
            }

            // Mise � jour de la dur�e maximale
            if (dto.MaxDurationMonths.HasValue && dto.MaxDurationMonths != program.MaxDurationMonths)
            {
                program.MaxDurationMonths = dto.MaxDurationMonths.Value;
                hasChanges = true;
            }

            // Mise � jour du plafond salarial
            if (dto.SalaryCeiling.HasValue && dto.SalaryCeiling != program.SalaryCeiling)
            {
                program.SalaryCeiling = dto.SalaryCeiling.Value;
                hasChanges = true;
            }

            if (hasChanges)
            {
                program.UpdatedAt = DateTimeOffset.UtcNow;
                program.UpdatedBy = userId;
                await _db.SaveChangesAsync();
            }

            var result = new StateEmploymentProgramReadDto
            {
                Id = program.Id,
                Code = program.Code,
                Name = program.Name,
                IsCnssEmployeeExempt = program.IsCnssEmployeeExempt,
                IsCnssEmployerExempt = program.IsCnssEmployerExempt,
                IsIrExempt = program.IsIrExempt,
                MaxDurationMonths = program.MaxDurationMonths,
                SalaryCeiling = program.SalaryCeiling,
                CreatedAt = program.CreatedAt.DateTime
            };

            return Ok(result);
        }

        /// <summary>
        /// Supprime un programme d'emploi �tatique (soft delete)
        /// DELETE /api/state-employment-programs/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();

            var program = await _db.StateEmploymentPrograms
                .FirstOrDefaultAsync(s => s.Id == id && s.DeletedAt == null);

            if (program == null)
                return NotFound(new { Message = "Programme d'emploi �tatique non trouv�" });

            // V�rifier si le programme est utilis� par des ContractTypes
            var isUsed = await _db.ContractTypes
                .AnyAsync(c => c.StateEmploymentProgramId == id && c.DeletedAt == null);

            if (isUsed)
                return BadRequest(new { Message = "Impossible de supprimer ce programme car il est utilis� par des types de contrats" });

            program.DeletedAt = DateTimeOffset.UtcNow;
            program.DeletedBy = userId;

            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}