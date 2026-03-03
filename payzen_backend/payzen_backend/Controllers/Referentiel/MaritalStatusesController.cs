using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Referentiel;

namespace payzen_backend.Models.Referentiel.Dtos
{
    public class MaritalStatusReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class MaritalStatusCreateDto
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
    }

    public class MaritalStatusUpdateDto
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
    }
}

namespace payzen_backend.Controllers.Referentiel
{
    [Route("api/marital-statuses")]
    [ApiController]
    [Authorize]
    public class MaritalStatusesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MaritalStatusesController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET /api/marital-statuses?includeInactive=true
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Models.Referentiel.Dtos.MaritalStatusReadDto>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var q = _db.MaritalStatuses.AsNoTracking().AsQueryable();
            if (!includeInactive)
                q = q.Where(m => m.IsActive);

            var items = await q
                .OrderBy(m => m.NameFr)
                .Select(m => new Models.Referentiel.Dtos.MaritalStatusReadDto
                {
                    Id = m.Id,
                    Code = m.Code,
                    NameFr = m.NameFr,
                    NameAr = m.NameAr,
                    NameEn = m.NameEn,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// GET /api/marital-statuses/{id}
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.MaritalStatusReadDto>> GetById(int id)
        {
            var item = await _db.MaritalStatuses
                .AsNoTracking()
                .Where(m => m.Id == id)
                .Select(m => new Models.Referentiel.Dtos.MaritalStatusReadDto
                {
                    Id = m.Id,
                    Code = m.Code,
                    NameFr = m.NameFr,
                    NameAr = m.NameAr,
                    NameEn = m.NameEn,
                    IsActive = m.IsActive,
                    CreatedAt = m.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { Message = "Statut marital non trouv�" });

            return Ok(item);
        }

        /// <summary>
        /// POST /api/marital-statuses
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.MaritalStatusReadDto>> Create([FromBody] Models.Referentiel.Dtos.MaritalStatusCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var code = dto.Code.Trim();

            var exists = await _db.MaritalStatuses
                .AsNoTracking()
                .AnyAsync(m => m.Code.ToLower() == code.ToLower());

            if (exists)
                return Conflict(new { Message = "Un statut marital avec ce code existe d�j�" });

            var userId = User.GetUserId();

            var entity = new MaritalStatus
            {
                Code = code,
                NameFr = dto.NameFr.Trim(),
                NameAr = dto.NameAr.Trim(),
                NameEn = dto.NameEn.Trim(),
                IsActive = dto.IsActive ?? true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.MaritalStatuses.Add(entity);
            await _db.SaveChangesAsync();

            var read = new Models.Referentiel.Dtos.MaritalStatusReadDto
            {
                Id = entity.Id,
                Code = entity.Code,
                NameFr = entity.NameFr,
                NameAr = entity.NameAr,
                NameEn = entity.NameEn,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, read);
        }

        /// <summary>
        /// PUT /api/marital-statuses/{id}
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.MaritalStatusReadDto>> Update(int id, [FromBody] Models.Referentiel.Dtos.MaritalStatusUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _db.MaritalStatuses.FirstOrDefaultAsync(m => m.Id == id);
            if (entity == null)
                return NotFound(new { Message = "Statut marital non trouv�" });

            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim() != entity.Code)
            {
                var newCode = dto.Code.Trim();
                var exists = await _db.MaritalStatuses.AnyAsync(m => m.Code.ToLower() == newCode.ToLower() && m.Id != id);
                if (exists)
                    return Conflict(new { Message = "Un statut marital avec ce code existe d�j�" });

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

            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var read = new Models.Referentiel.Dtos.MaritalStatusReadDto
            {
                Id = entity.Id,
                Code = entity.Code,
                NameFr = entity.NameFr,
                NameAr = entity.NameAr,
                NameEn = entity.NameEn,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };

            return Ok(read);
        }

        /// <summary>
        /// DELETE /api/marital-statuses/{id}  (d�sactive)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.MaritalStatuses.FirstOrDefaultAsync(m => m.Id == id);
            if (entity == null)
                return NotFound(new { Message = "Statut marital non trouv�" });

            // Emp�cher la suppression si utilis� par des employ�s actifs
            var used = await _db.Employees.AnyAsync(e => e.MaritalStatusId == id && e.DeletedAt == null);
            if (used)
                return BadRequest(new { Message = "Impossible de supprimer ce statut car il est utilis� par des employ�s" });

            entity.IsActive = false;
            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}