using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Referentiel;

namespace payzen_backend.Models.Referentiel.Dtos
{
    public class EducationLevelReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public int LevelOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class EducationLevelCreateDto
    {
        [Required]
        [StringLength(50)]
        public required string Code { get; set; }

        [Required]
        [StringLength(100)]
        public required string NameFr { get; set; }

        [Required]
        [StringLength(100)]
        public required string NameAr { get; set; }

        [Required]
        [StringLength(100)]
        public required string NameEn { get; set; }

        [Range(1, int.MaxValue)]
        public int LevelOrder { get; set; } = 1;

        public bool? IsActive { get; set; } = true;
    }

    public class EducationLevelUpdateDto
    {
        [StringLength(50)]
        public string? Code { get; set; }

        [StringLength(100)]
        public string? NameFr { get; set; }

        [StringLength(100)]
        public string? NameAr { get; set; }

        [StringLength(100)]
        public string? NameEn { get; set; }

        [Range(1, int.MaxValue)]
        public int? LevelOrder { get; set; }

        public bool? IsActive { get; set; }
    }
}

namespace payzen_backend.Controllers.v1.Referentiel
{
    [Route("api/v{version:apiVersion}/education-levels")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class EducationLevelsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EducationLevelsController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Récupère tous les niveaux d'éducation.
        /// GET /api/education-levels?includeInactive=true
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Models.Referentiel.Dtos.EducationLevelReadDto>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var q = _db.EducationLevels.AsNoTracking().AsQueryable();
            if (!includeInactive)
                q = q.Where(e => e.IsActive);

            var items = await q
                .OrderBy(e => e.LevelOrder)
                .ThenBy(e => e.NameFr)
                .Select(e => new Models.Referentiel.Dtos.EducationLevelReadDto
                {
                    Id = e.Id,
                    Code = e.Code,
                    NameFr = e.NameFr,
                    NameAr = e.NameAr,
                    NameEn = e.NameEn,
                    LevelOrder = e.LevelOrder,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Récupère un niveau par id
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.EducationLevelReadDto>> GetById(int id)
        {
            var item = await _db.EducationLevels
                .AsNoTracking()
                .Where(e => e.Id == id)
                .Select(e => new Models.Referentiel.Dtos.EducationLevelReadDto
                {
                    Id = e.Id,
                    Code = e.Code,
                    NameFr = e.NameFr,
                    NameAr = e.NameAr,
                    NameEn = e.NameEn,
                    LevelOrder = e.LevelOrder,
                    IsActive = e.IsActive,
                    CreatedAt = e.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { Message = "Niveau d'éducation non trouvé" });

            return Ok(item);
        }

        /// <summary>
        /// Crée un niveau d'éducation
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.EducationLevelReadDto>> Create([FromBody] Models.Referentiel.Dtos.EducationLevelCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var code = dto.Code.Trim();

            var exists = await _db.EducationLevels
                .AsNoTracking()
                .AnyAsync(e => e.Code.ToLower() == code.ToLower());

            if (exists)
                return Conflict(new { Message = "Un niveau avec ce code existe déjà" });

            var userId = User.GetUserId();

            var entity = new EducationLevel
            {
                Code = code,
                NameFr = dto.NameFr.Trim(),
                NameAr = dto.NameAr.Trim(),
                NameEn = dto.NameEn.Trim(),
                LevelOrder = dto.LevelOrder,
                IsActive = dto.IsActive ?? true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.EducationLevels.Add(entity);
            await _db.SaveChangesAsync();

            var read = new Models.Referentiel.Dtos.EducationLevelReadDto
            {
                Id = entity.Id,
                Code = entity.Code,
                NameFr = entity.NameFr,
                NameAr = entity.NameAr,
                NameEn = entity.NameEn,
                LevelOrder = entity.LevelOrder,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, read);
        }

        /// <summary>
        /// Met à jour un niveau d'éducation
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.EducationLevelReadDto>> Update(int id, [FromBody] Models.Referentiel.Dtos.EducationLevelUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _db.EducationLevels.FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null)
                return NotFound(new { Message = "Niveau d'éducation non trouvé" });

            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim() != entity.Code)
            {
                var newCode = dto.Code.Trim();
                var exists = await _db.EducationLevels.AnyAsync(e => e.Code.ToLower() == newCode.ToLower() && e.Id != id);
                if (exists)
                    return Conflict(new { Message = "Un niveau avec ce code existe déjà" });

                entity.Code = newCode;
            }

            if (!string.IsNullOrWhiteSpace(dto.NameFr))
                entity.NameFr = dto.NameFr.Trim();

            if (!string.IsNullOrWhiteSpace(dto.NameAr))
                entity.NameAr = dto.NameAr.Trim();

            if (!string.IsNullOrWhiteSpace(dto.NameEn))
                entity.NameEn = dto.NameEn.Trim();

            if (dto.LevelOrder.HasValue)
                entity.LevelOrder = dto.LevelOrder.Value;

            if (dto.IsActive.HasValue)
                entity.IsActive = dto.IsActive.Value;

            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var read = new Models.Referentiel.Dtos.EducationLevelReadDto
            {
                Id = entity.Id,
                Code = entity.Code,
                NameFr = entity.NameFr,
                NameAr = entity.NameAr,
                NameEn = entity.NameEn,
                LevelOrder = entity.LevelOrder,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };

            return Ok(read);
        }

        /// <summary>
        /// Désactive un niveau d'éducation (soft)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.EducationLevels.FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null)
                return NotFound(new { Message = "Niveau d'éducation non trouvé" });

            // Vérifier utilisation par des employés actifs
            var used = await _db.Employees.AnyAsync(emp => emp.EducationLevelId == id && emp.DeletedAt == null);
            if (used)
                return BadRequest(new { Message = "Impossible de supprimer ce niveau car il est utilisé par des employés" });

            entity.IsActive = false;
            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}