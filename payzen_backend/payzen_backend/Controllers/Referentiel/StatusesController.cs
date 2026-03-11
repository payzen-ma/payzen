using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Referentiel;

namespace payzen_backend.Models.Referentiel.Dtos
{
    public class StatusReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool AffectsAccess { get; set; }
        public bool AffectsPayroll { get; set; }
        public bool AffectsAttendance { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class StatusCreateDto
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

        public bool? IsActive { get; set; } = true;
        public bool? AffectsAccess { get; set; } = false;
        public bool? AffectsPayroll { get; set; } = false;
        public bool? AffectsAttendance { get; set; } = false;
    }

    public class StatusUpdateDto
    {
        [StringLength(50)]
        public string? Code { get; set; }

        [StringLength(100)]
        public string? NameFr { get; set; }

        [StringLength(100)]
        public string? NameAr { get; set; }

        [StringLength(100)]
        public string? NameEn { get; set; }

        public bool? IsActive { get; set; }
        public bool? AffectsAccess { get; set; }
        public bool? AffectsPayroll { get; set; }
        public bool? AffectsAttendance { get; set; }
    }
}

namespace payzen_backend.Controllers.Referentiel
{
    [Route("api/statuses")]
    [ApiController]
    [Authorize]
    public class StatusesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StatusesController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET /api/statuses?includeInactive=true
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Models.Referentiel.Dtos.StatusReadDto>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var q = _db.Statuses.AsNoTracking().AsQueryable();
            if (!includeInactive)
                q = q.Where(s => s.IsActive);

            var items = await q
                .OrderBy(s => s.NameFr)
                .Select(s => new Models.Referentiel.Dtos.StatusReadDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    NameFr = s.NameFr,
                    NameAr = s.NameAr,
                    NameEn = s.NameEn,
                    IsActive = s.IsActive,
                    AffectsAccess = s.AffectsAccess,
                    AffectsPayroll = s.AffectsPayroll,
                    AffectsAttendance = s.AffectsAttendance,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// GET /api/statuses/{id}
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.StatusReadDto>> GetById(int id)
        {
            var item = await _db.Statuses
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new Models.Referentiel.Dtos.StatusReadDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    NameFr = s.NameFr,
                    NameAr = s.NameAr,
                    NameEn = s.NameEn,
                    IsActive = s.IsActive,
                    AffectsAccess = s.AffectsAccess,
                    AffectsPayroll = s.AffectsPayroll,
                    AffectsAttendance = s.AffectsAttendance,
                    CreatedAt = s.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { Message = "Statut non trouvé" });

            return Ok(item);
        }

        /// <summary>
        /// POST /api/statuses
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.StatusReadDto>> Create([FromBody] Models.Referentiel.Dtos.StatusCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var code = dto.Code.Trim();

            var exists = await _db.Statuses
                .AsNoTracking()
                .AnyAsync(s => s.Code.ToLower() == code.ToLower());

            if (exists)
                return Conflict(new { Message = "Un statut avec ce code existe déjà" });

            var userId = User.GetUserId();

            var entity = new Status
            {
                Code = code,
                NameFr = dto.NameFr.Trim(),
                NameAr = dto.NameAr.Trim(),
                NameEn = dto.NameEn.Trim(),
                IsActive = dto.IsActive ?? true,
                AffectsAccess = dto.AffectsAccess ?? false,
                AffectsPayroll = dto.AffectsPayroll ?? false,
                AffectsAttendance = dto.AffectsAttendance ?? false,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.Statuses.Add(entity);
            await _db.SaveChangesAsync();

            var read = new Models.Referentiel.Dtos.StatusReadDto
            {
                Id = entity.Id,
                Code = entity.Code,
                NameFr = entity.NameFr,
                NameAr = entity.NameAr,
                NameEn = entity.NameEn,
                IsActive = entity.IsActive,
                AffectsAccess = entity.AffectsAccess,
                AffectsPayroll = entity.AffectsPayroll,
                AffectsAttendance = entity.AffectsAttendance,
                CreatedAt = entity.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, read);
        }

        /// <summary>
        /// PUT /api/statuses/{id}
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.StatusReadDto>> Update(int id, [FromBody] Models.Referentiel.Dtos.StatusUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _db.Statuses.FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null)
                return NotFound(new { Message = "Statut non trouvé" });

            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim() != entity.Code)
            {
                var newCode = dto.Code.Trim();
                var exists = await _db.Statuses.AnyAsync(s => s.Code.ToLower() == newCode.ToLower() && s.Id != id);
                if (exists)
                    return Conflict(new { Message = "Un statut avec ce code existe déjà" });

                entity.Code = newCode;
            }

            if (!string.IsNullOrWhiteSpace(dto.NameFr))
                entity.NameFr = dto.NameFr.Trim();

            if (!string.IsNullOrWhiteSpace(dto.NameAr))
                entity.NameAr = dto.NameAr.Trim();

            if (!string.IsNullOrWhiteSpace(dto.NameEn))
                entity.NameEn = dto.NameEn.Trim();

            if (dto.IsActive.HasValue)
                entity.IsActive = dto.IsActive.Value;

            if (dto.AffectsAccess.HasValue)
                entity.AffectsAccess = dto.AffectsAccess.Value;

            if (dto.AffectsPayroll.HasValue)
                entity.AffectsPayroll = dto.AffectsPayroll.Value;

            if (dto.AffectsAttendance.HasValue)
                entity.AffectsAttendance = dto.AffectsAttendance.Value;

            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var read = new Models.Referentiel.Dtos.StatusReadDto
            {
                Id = entity.Id,
                Code = entity.Code,
                NameFr = entity.NameFr,
                NameAr = entity.NameAr,
                NameEn = entity.NameEn,
                IsActive = entity.IsActive,
                AffectsAccess = entity.AffectsAccess,
                AffectsPayroll = entity.AffectsPayroll,
                AffectsAttendance = entity.AffectsAttendance,
                CreatedAt = entity.CreatedAt
            };

            return Ok(read);
        }

        /// <summary>
        /// DELETE /api/statuses/{id}  (désactive)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Statuses.FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null)
                return NotFound(new { Message = "Statut non trouvé" });

            // Empêcher désactivation si utilisé par des employés actifs
            var used = await _db.Employees.AnyAsync(e => e.StatusId == id && e.DeletedAt == null);
            if (used)
                return BadRequest(new { Message = "Impossible de supprimer ce statut car il est utilisé par des employés" });

            entity.IsActive = false;
            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
