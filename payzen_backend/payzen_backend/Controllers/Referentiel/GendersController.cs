using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Referentiel;
using static payzen_backend.Models.Permissions.PermissionsConstants;

namespace payzen_backend.Models.Referentiel.Dtos
{
    public class GenderReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameFr { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class GenderCreateDto
    {
        [Required(ErrorMessage = "Le code est requis")]
        [StringLength(50, ErrorMessage = "Le code ne peut pas dépasser 50 caractères")]
        public required string Code { get; set; }

        [Required(ErrorMessage = "Le libellé français est requis")]
        [StringLength(100, ErrorMessage = "Le libellé FR ne peut pas dépasser 100 caractères")]
        public required string NameFr { get; set; }

        [Required(ErrorMessage = "Le libellé arabe est requis")]
        [StringLength(100, ErrorMessage = "Le libellé AR ne peut pas dépasser 100 caractères")]
        public required string NameAr { get; set; }

        [Required(ErrorMessage = "Le libellé anglais est requis")]
        [StringLength(100, ErrorMessage = "Le libellé EN ne peut pas dépasser 100 caractères")]
        public required string NameEn { get; set; }

        public bool? IsActive { get; set; } = true;
    }

    public class GenderUpdateDto
    {
        [StringLength(50, ErrorMessage = "Le code ne peut pas dépasser 50 caractères")]
        public string? Code { get; set; }

        [StringLength(100, ErrorMessage = "Le libellé FR ne peut pas dépasser 100 caractères")]
        public string? NameFr { get; set; }

        [StringLength(100, ErrorMessage = "Le libellé AR ne peut pas dépasser 100 caractères")]
        public string? NameAr { get; set; }

        [StringLength(100, ErrorMessage = "Le libellé EN ne peut pas dépasser 100 caractères")]
        public string? NameEn { get; set; }

        public bool? IsActive { get; set; }
    }
}

namespace payzen_backend.Controllers.Referentiel
{
    [Route("api/genders")]
    [ApiController]
    [Authorize]
    public class GendersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public GendersController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Récupère tous les genres actifs (par défaut)
        /// GET /api/genders
        /// </summary>
        [HttpGet]
        //[HasPermission(READ_GENDERS)]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<Models.Referentiel.Dtos.GenderReadDto>>> GetAll([FromQuery] bool includeInactive = false)
        {
            var query = _db.Genders.AsNoTracking().AsQueryable();

            if (!includeInactive)
                query = query.Where(g => g.IsActive);

            var items = await query
                .OrderBy(g => g.NameFr)
                .Select(g => new Models.Referentiel.Dtos.GenderReadDto
                {
                    Id = g.Id,
                    Code = g.Code,
                    NameFr = g.NameFr,
                    NameAr = g.NameAr,
                    NameEn = g.NameEn,
                    IsActive = g.IsActive,
                    CreatedAt = g.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Récupère un genre par id
        /// </summary>
        [HttpGet("{id}")]
        //[HasPermission(READ_GENDERS)]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.GenderReadDto>> GetById(int id)
        {
            var gender = await _db.Genders
                .AsNoTracking()
                .Where(g => g.Id == id)
                .Select(g => new Models.Referentiel.Dtos.GenderReadDto
                {
                    Id = g.Id,
                    Code = g.Code,
                    NameFr = g.NameFr,
                    NameAr = g.NameAr,
                    NameEn = g.NameEn,
                    IsActive = g.IsActive,
                    CreatedAt = g.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (gender == null)
                return NotFound(new { Message = "Genre non trouvé" });

            return Ok(gender);
        }

        /// <summary>
        /// Crée un genre
        /// </summary>
        [HttpPost]
        //[HasPermission(CREATE_GENDERS)]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.GenderReadDto>> Create([FromBody] Models.Referentiel.Dtos.GenderCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var code = dto.Code.Trim();

            var exists = await _db.Genders
                .AsNoTracking()
                .AnyAsync(g => g.Code.ToLower() == code.ToLower());

            if (exists)
                return Conflict(new { Message = "Un genre avec ce code existe déjà" });

            var userId = User.GetUserId();

            var gender = new Gender
            {
                Code = code,
                NameFr = dto.NameFr.Trim(),
                NameAr = dto.NameAr.Trim(),
                NameEn = dto.NameEn.Trim(),
                IsActive = dto.IsActive ?? true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.Genders.Add(gender);
            await _db.SaveChangesAsync();

            var read = new Models.Referentiel.Dtos.GenderReadDto
            {
                Id = gender.Id,
                Code = gender.Code,
                NameFr = gender.NameFr,
                NameAr = gender.NameAr,
                NameEn = gender.NameEn,
                IsActive = gender.IsActive,
                CreatedAt = gender.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = gender.Id }, read);
        }

        /// <summary>
        /// Met à jour un genre
        /// </summary>
        [HttpPut("{id}")]
        //[HasPermission(UPDATE_GENDERS)]
        [Produces("application/json")]
        public async Task<ActionResult<Models.Referentiel.Dtos.GenderReadDto>> Update(int id, [FromBody] Models.Referentiel.Dtos.GenderUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var gender = await _db.Genders.FirstOrDefaultAsync(g => g.Id == id);
            if (gender == null)
                return NotFound(new { Message = "Genre non trouvé" });

            // Code uniqueness check
            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim() != gender.Code)
            {
                var newCode = dto.Code.Trim();
                var exists = await _db.Genders.AnyAsync(g => g.Code.ToLower() == newCode.ToLower() && g.Id != id);
                if (exists)
                    return Conflict(new { Message = "Un genre avec ce code existe déjà" });

                gender.Code = newCode;
            }

            if (!string.IsNullOrWhiteSpace(dto.NameFr) && dto.NameFr.Trim() != gender.NameFr)
                gender.NameFr = dto.NameFr.Trim();

            if (!string.IsNullOrWhiteSpace(dto.NameAr) && dto.NameAr.Trim() != gender.NameAr)
                gender.NameAr = dto.NameAr.Trim();

            if (!string.IsNullOrWhiteSpace(dto.NameEn) && dto.NameEn.Trim() != gender.NameEn)
                gender.NameEn = dto.NameEn.Trim();

            if (dto.IsActive.HasValue && dto.IsActive.Value != gender.IsActive)
                gender.IsActive = dto.IsActive.Value;

            var userId = User.GetUserId();
            gender.ModifiedAt = DateTimeOffset.UtcNow;
            gender.ModifiedBy = userId;

            await _db.SaveChangesAsync();

            var read = new Models.Referentiel.Dtos.GenderReadDto
            {
                Id = gender.Id,
                Code = gender.Code,
                NameFr = gender.NameFr,
                NameAr = gender.NameAr,
                NameEn = gender.NameEn,
                IsActive = gender.IsActive,
                CreatedAt = gender.CreatedAt
            };

            return Ok(read);
        }

        /// <summary>
        /// Désactive (soft delete) un genre
        /// </summary>
        [HttpDelete("{id}")]
        //[HasPermission(DELETE_GENDERS)]
        public async Task<IActionResult> Delete(int id)
        {
            var gender = await _db.Genders.FirstOrDefaultAsync(g => g.Id == id);
            if (gender == null)
                return NotFound(new { Message = "Genre non trouvé" });

            // Vérifier utilisation par des employés actifs
            var used = await _db.Employees.AnyAsync(e => e.GenderId == id && e.DeletedAt == null);
            if (used)
                return BadRequest(new { Message = "Impossible de supprimer ce genre car il est utilisé par des employés" });

            gender.IsActive = false;
            gender.ModifiedAt = DateTimeOffset.UtcNow;
            gender.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}