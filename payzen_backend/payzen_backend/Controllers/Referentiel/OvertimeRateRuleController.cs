using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Extensions;
using payzen_backend.Models.Referentiel;
using payzen_backend.Models.Referentiel.Dtos;
using payzen_backend.Models.Common.OvertimeEnums;

namespace payzen_backend.Controllers.Referentiel
{
    [Route("api/overtime-rate-rules")]
    [ApiController]
    [Authorize]
    public class OvertimeRateRuleController : ControllerBase
    {
        private readonly AppDbContext _db;

        public OvertimeRateRuleController(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// R�cup�re toutes les r�gles de majoration actives
        /// GET /api/overtime-rate-rules?isActive=true&category=FERIE
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<OvertimeRateRuleReadDto>>> GetAll(
            [FromQuery] bool? isActive = null,
            [FromQuery] string? category = null,
            [FromQuery] OvertimeType? appliesTo = null,
            [FromQuery] DateOnly? effectiveOn = null)
        {
            var query = _db.OvertimeRateRules
                .AsNoTracking()
                .Where(r => r.DeletedAt == null);

            // Filtres
            if (isActive.HasValue)
                query = query.Where(r => r.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(r => r.Category == category.Trim());

            if (appliesTo.HasValue)
                query = query.Where(r => (r.AppliesTo & appliesTo.Value) == appliesTo.Value);

            if (effectiveOn.HasValue)
                query = query.Where(r => r.EffectiveFrom <= effectiveOn.Value &&
                                        (r.EffectiveTo == null || r.EffectiveTo >= effectiveOn.Value));

            var rules = await query
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Code)
                .Select(r => new OvertimeRateRuleReadDto
                {
                    Id = r.Id,
                    Code = r.Code,
                    NameFr = r.NameFr,
                    NameAr = r.NameAr,
                    NameEn = r.NameEn,
                    Description = r.Description,
                    AppliesTo = r.AppliesTo,
                    TimeRangeType = r.TimeRangeType,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    ApplicableDaysOfWeek = r.ApplicableDaysOfWeek,
                    Multiplier = r.Multiplier,
                    CumulationStrategy = r.CumulationStrategy,
                    Priority = r.Priority,
                    Category = r.Category,
                    IsActive = r.IsActive,
                    EffectiveFrom = r.EffectiveFrom,
                    EffectiveTo = r.EffectiveTo,
                    MinimumDurationHours = r.MinimumDurationHours,
                    MaximumDurationHours = r.MaximumDurationHours,
                    RequiresSuperiorApproval = r.RequiresSuperiorApproval,
                    LegalReference = r.LegalReference,
                    DocumentationUrl = r.DocumentationUrl,
                    CreatedAt = r.CreatedAt.DateTime
                })
                .ToListAsync();

            return Ok(rules);
        }

        /// <summary>
        /// R�cup�re une r�gle par ID
        /// GET /api/overtime-rate-rules/5
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<OvertimeRateRuleReadDto>> GetById(int id)
        {
            var rule = await _db.OvertimeRateRules
                .AsNoTracking()
                .Where(r => r.Id == id && r.DeletedAt == null)
                .Select(r => new OvertimeRateRuleReadDto
                {
                    Id = r.Id,
                    Code = r.Code,
                    NameFr = r.NameFr,
                    NameAr = r.NameAr,
                    NameEn = r.NameEn,
                    Description = r.Description,
                    AppliesTo = r.AppliesTo,
                    TimeRangeType = r.TimeRangeType,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    ApplicableDaysOfWeek = r.ApplicableDaysOfWeek,
                    Multiplier = r.Multiplier,
                    CumulationStrategy = r.CumulationStrategy,
                    Priority = r.Priority,
                    Category = r.Category,
                    IsActive = r.IsActive,
                    EffectiveFrom = r.EffectiveFrom,
                    EffectiveTo = r.EffectiveTo,
                    MinimumDurationHours = r.MinimumDurationHours,
                    MaximumDurationHours = r.MaximumDurationHours,
                    RequiresSuperiorApproval = r.RequiresSuperiorApproval,
                    LegalReference = r.LegalReference,
                    DocumentationUrl = r.DocumentationUrl,
                    CreatedAt = r.CreatedAt.DateTime
                })
                .FirstOrDefaultAsync();

            if (rule == null)
                return NotFound(new { Message = "R�gle de majoration non trouv�e" });

            return Ok(rule);
        }

        /// <summary>
        /// Cr�e une nouvelle r�gle de majoration
        /// POST /api/overtime-rate-rules
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<OvertimeRateRuleReadDto>> Create([FromBody] OvertimeRateRuleCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            // V�rifier unicit� Code + EffectiveFrom
            if (await _db.OvertimeRateRules.AnyAsync(r =>
                r.Code == dto.Code &&
                r.EffectiveFrom == dto.EffectiveFrom &&
                r.DeletedAt == null))
            {
                return Conflict(new { Message = "Une r�gle avec ce code existe d�j� pour cette p�riode" });
            }

            // Validation coh�rence TimeRange
            if (dto.TimeRangeType != TimeRangeType.AllDay)
            {
                if (dto.StartTime == null || dto.EndTime == null)
                    return BadRequest(new { Message = "StartTime et EndTime sont requis pour TimeRangeType != AllDay" });
            }

            var rule = new OvertimeRateRule
            {
                Code = dto.Code.Trim().ToUpper(),
                NameFr = dto.NameFr.Trim(),
                NameAr = dto.NameAr.Trim(),
                NameEn = dto.NameEn.Trim(),
                Description = dto.Description?.Trim(),
                AppliesTo = dto.AppliesTo,
                TimeRangeType = dto.TimeRangeType,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                ApplicableDaysOfWeek = dto.ApplicableDaysOfWeek,
                Multiplier = dto.Multiplier,
                CumulationStrategy = dto.CumulationStrategy,
                Priority = dto.Priority,
                Category = dto.Category?.Trim(),
                IsActive = dto.IsActive,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                MinimumDurationHours = dto.MinimumDurationHours,
                MaximumDurationHours = dto.MaximumDurationHours,
                RequiresSuperiorApproval = dto.RequiresSuperiorApproval,
                LegalReference = dto.LegalReference?.Trim(),
                DocumentationUrl = dto.DocumentationUrl?.Trim(),
                CreatedBy = userId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.OvertimeRateRules.Add(rule);
            await _db.SaveChangesAsync();

            var result = await GetById(rule.Id);
            return CreatedAtAction(nameof(GetById), new { id = rule.Id }, result.Value);
        }

        /// <summary>
        /// Met � jour une r�gle de majoration
        /// PUT /api/overtime-rate-rules/5
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<OvertimeRateRuleReadDto>> Update(int id, [FromBody] OvertimeRateRuleUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var rule = await _db.OvertimeRateRules
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (rule == null)
                return NotFound(new { Message = "R�gle de majoration non trouv�e" });

            // V�rifier si utilis�e dans des overtimes approuv�s
            var isUsedInApprovedOvertimes = await _db.EmployeeOvertimes
                .AnyAsync(o => o.RateRuleId == id &&
                              o.Status == OvertimeStatus.Approved &&
                              o.DeletedAt == null);

            if (isUsedInApprovedOvertimes)
            {
                return BadRequest(new
                {
                    Message = "Cette r�gle est utilis�e dans des overtimes approuv�s. " +
                             "Cr�ez une nouvelle version avec une date EffectiveFrom diff�rente."
                });
            }

            bool hasChanges = false;

            // Mise � jour des champs
            if (dto.NameFr != null && dto.NameFr.Trim() != rule.NameFr)
            {
                rule.NameFr = dto.NameFr.Trim();
                hasChanges = true;
            }

            if (dto.NameAr != null && dto.NameAr.Trim() != rule.NameAr)
            {
                rule.NameAr = dto.NameAr.Trim();
                hasChanges = true;
            }

            if (dto.NameEn != null && dto.NameEn.Trim() != rule.NameEn)
            {
                rule.NameEn = dto.NameEn.Trim();
                hasChanges = true;
            }

            if (dto.Description != null)
            {
                rule.Description = dto.Description.Trim();
                hasChanges = true;
            }

            if (dto.AppliesTo.HasValue && dto.AppliesTo != rule.AppliesTo)
            {
                rule.AppliesTo = dto.AppliesTo.Value;
                hasChanges = true;
            }

            if (dto.Multiplier.HasValue && dto.Multiplier != rule.Multiplier)
            {
                rule.Multiplier = dto.Multiplier.Value;
                hasChanges = true;
            }

            if (dto.Priority.HasValue && dto.Priority != rule.Priority)
            {
                rule.Priority = dto.Priority.Value;
                hasChanges = true;
            }

            if (dto.IsActive.HasValue && dto.IsActive != rule.IsActive)
            {
                rule.IsActive = dto.IsActive.Value;
                hasChanges = true;
            }

            // ... Autres champs selon besoin

            if (hasChanges)
            {
                rule.ModifiedBy = userId;
                rule.ModifiedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();
            }

            var result = await GetById(id);
            return Ok(result.Value);
        }

        /// <summary>
        /// D�sactive une r�gle (soft delete)
        /// DELETE /api/overtime-rate-rules/5
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();

            var rule = await _db.OvertimeRateRules
                .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);

            if (rule == null)
                return NotFound(new { Message = "R�gle de majoration non trouv�e" });

            // V�rifier si utilis�e
            var isUsed = await _db.EmployeeOvertimes
                .AnyAsync(o => o.RateRuleId == id && o.DeletedAt == null);

            if (isUsed)
            {
                return BadRequest(new
                {
                    Message = "Cette r�gle est utilis�e dans des overtimes. " +
                             "Vous pouvez la d�sactiver (IsActive=false) au lieu de la supprimer."
                });
            }

            rule.DeletedBy = userId;
            rule.DeletedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// R�cup�re les cat�gories distinctes de r�gles
        /// GET /api/overtime-rate-rules/categories
        /// </summary>
        [HttpGet("categories")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            var categories = await _db.OvertimeRateRules
                .AsNoTracking()
                .Where(r => r.DeletedAt == null && r.Category != null)
                .Select(r => r.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }
    }
}
