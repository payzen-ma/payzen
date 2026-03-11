using payzen_backend.Models.Leave;
using payzen_backend.Models.Leave.Dtos;
using payzen_backend.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using payzen_backend.Data;
using payzen_backend.Services;

namespace payzen_backend.Controllers.v1.Leave
{
    [Route("api/v{version:apiVersion}/leave-type-legal-rules")]
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    public class LeaveTypeLegalRuleController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly LeaveEventLogService _leaveEventLogService;

        public LeaveTypeLegalRuleController(AppDbContext db, LeaveEventLogService leaveEventLogService)
        {
            _db = db;
            _leaveEventLogService = leaveEventLogService;
        }

        /// <summary>
        /// Récupère toutes les règles légales
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveTypeLegalRuleReadDto>>> GetAll([FromQuery] int? leaveTypeId = null)
        {
            var query = _db.LeaveTypeLegalRules
                .AsNoTracking();

            if (leaveTypeId.HasValue)
            {
                query = query.Where(r => r.LeaveTypeId == leaveTypeId.Value);
            }

            var rules = await query
                .OrderBy(r => r.LeaveTypeId)
                .ThenBy(r => r.EventCaseCode)
                .Select(r => new LeaveTypeLegalRuleReadDto
                {
                    Id = r.Id,
                    LeaveTypeId = r.LeaveTypeId,
                    EventCaseCode = r.EventCaseCode,
                    Description = r.Description,
                    DaysGranted = r.DaysGranted,
                    LegalArticle = r.LegalArticle,
                    CanBeDiscontinuous = r.CanBeDiscountinuous,
                    MustBeUsedWithinDays = r.MustBeUsedWithinDays
                })
                .ToListAsync();

            return Ok(rules);
        }

        /// <summary>
        /// Récupère une règle légale par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LeaveTypeLegalRuleReadDto>> GetById(int id)
        {
            var rule = await _db.LeaveTypeLegalRules
                .AsNoTracking()
                .Where(r => r.Id == id)
                .Select(r => new LeaveTypeLegalRuleReadDto
                {
                    Id = r.Id,
                    LeaveTypeId = r.LeaveTypeId,
                    EventCaseCode = r.EventCaseCode,
                    Description = r.Description,
                    DaysGranted = r.DaysGranted,
                    LegalArticle = r.LegalArticle,
                    CanBeDiscontinuous = r.CanBeDiscountinuous,
                    MustBeUsedWithinDays = r.MustBeUsedWithinDays
                })
                .FirstOrDefaultAsync();

            if (rule == null)
                return NotFound(new { Message = "Règle légale non trouvée" });

            return Ok(rule);
        }

        /// <summary>
        /// Récupère les règles légales par type de congé
        /// </summary>
        [HttpGet("by-leave-type/{leaveTypeId}")]
        public async Task<ActionResult<IEnumerable<LeaveTypeLegalRuleReadDto>>> GetByLeaveType(int leaveTypeId)
        {
            var rules = await _db.LeaveTypeLegalRules
                .AsNoTracking()
                .Where(r => r.LeaveTypeId == leaveTypeId)
                .Select(r => new LeaveTypeLegalRuleReadDto
                {
                    Id = r.Id,
                    LeaveTypeId = r.LeaveTypeId,
                    EventCaseCode = r.EventCaseCode,
                    Description = r.Description,
                    DaysGranted = r.DaysGranted,
                    LegalArticle = r.LegalArticle,
                    CanBeDiscontinuous = r.CanBeDiscountinuous,
                    MustBeUsedWithinDays = r.MustBeUsedWithinDays
                })
                .ToListAsync();

            return Ok(rules);
        }

        /// <summary>
        /// Crée une nouvelle règle légale
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<LeaveTypeLegalRuleReadDto>> Create([FromBody] LeaveTypeLegalRuleCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Vérifier que le LeaveType existe
            var leaveType = await _db.LeaveTypes
                .FirstOrDefaultAsync(lt => lt.Id == dto.LeaveTypeId && lt.DeletedAt == null);

            if (leaveType == null)
            {
                return NotFound(new { Message = "Type de congé non trouvé" });
            }

            // Vérifier l'unicité: EventCaseCode unique par LeaveTypeId
            var ruleExists = await _db.LeaveTypeLegalRules
                .AnyAsync(r => r.LeaveTypeId == dto.LeaveTypeId && r.EventCaseCode == dto.EventCaseCode);

            if (ruleExists)
            {
                return Conflict(new { Message = $"Une règle légale avec le code '{dto.EventCaseCode}' existe déjà pour ce type de congé" });
            }

            // Validation métier
            if (dto.DaysGranted <= 0)
            {
                return BadRequest(new { Message = "DaysGranted doit être supérieur à 0" });
            }

            var userId = User.GetUserId();

            var rule = new LeaveTypeLegalRule
            {
                LeaveTypeId = dto.LeaveTypeId,
                EventCaseCode = dto.EventCaseCode.Trim(),
                Description = dto.Description.Trim(),
                DaysGranted = dto.DaysGranted,
                LegalArticle = dto.LegalArticle.Trim(),
                CanBeDiscountinuous = dto.CanBeDiscontinuous,
                MustBeUsedWithinDays = dto.MustBeUsedWithinDays,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            _db.LeaveTypeLegalRules.Add(rule);
            await _db.SaveChangesAsync();

            // Logger l'événement
            var companyId = leaveType.CompanyId ?? 0;
            await _leaveEventLogService.LogSimpleEventAsync(
                companyId,
                LeaveEventLogService.EventNames.LegalRuleCreated,
                null,
                $"Règle légale '{dto.EventCaseCode}' créée pour LeaveType {dto.LeaveTypeId}",
                userId
            );

            var readDto = new LeaveTypeLegalRuleReadDto
            {
                Id = rule.Id,
                LeaveTypeId = rule.LeaveTypeId,
                EventCaseCode = rule.EventCaseCode,
                Description = rule.Description,
                DaysGranted = rule.DaysGranted,
                LegalArticle = rule.LegalArticle,
                CanBeDiscontinuous = rule.CanBeDiscountinuous,
                MustBeUsedWithinDays = rule.MustBeUsedWithinDays
            };

            return CreatedAtAction(nameof(GetById), new { id = rule.Id }, readDto);
        }

        /// <summary>
        /// Met à jour une règle légale
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<LeaveTypeLegalRuleReadDto>> Update(int id, [FromBody] LeaveTypeLegalRulePatchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var rule = await _db.LeaveTypeLegalRules
                .Include(r => r.LeaveType)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rule == null)
                return NotFound(new { Message = "Règle légale non trouvée" });

            var userId = User.GetUserId();
            var changes = new List<string>();

            // Mise à jour de l'EventCaseCode avec vérification d'unicité
            if (!string.IsNullOrWhiteSpace(dto.EventCaseCode) && dto.EventCaseCode != rule.EventCaseCode)
            {
                var codeExists = await _db.LeaveTypeLegalRules
                    .AnyAsync(r => r.Id != id && r.LeaveTypeId == rule.LeaveTypeId && r.EventCaseCode == dto.EventCaseCode);

                if (codeExists)
                {
                    return Conflict(new { Message = $"Une règle légale avec le code '{dto.EventCaseCode}' existe déjà pour ce type de congé" });
                }

                changes.Add($"EventCaseCode: '{rule.EventCaseCode}' → '{dto.EventCaseCode}'");
                rule.EventCaseCode = dto.EventCaseCode.Trim();
            }

            // Mise à jour de la description
            if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description != rule.Description)
            {
                changes.Add($"Description modifiée");
                rule.Description = dto.Description.Trim();
            }

            // Mise à jour de DaysGranted
            if (dto.DaysGranted.HasValue && dto.DaysGranted != rule.DaysGranted)
            {
                if (dto.DaysGranted <= 0)
                {
                    return BadRequest(new { Message = "DaysGranted doit être supérieur à 0" });
                }
                changes.Add($"DaysGranted: {rule.DaysGranted} → {dto.DaysGranted}");
                rule.DaysGranted = dto.DaysGranted.Value;
            }

            // Mise à jour de LegalArticle
            if (!string.IsNullOrWhiteSpace(dto.LegalArticle) && dto.LegalArticle != rule.LegalArticle)
            {
                changes.Add($"LegalArticle: '{rule.LegalArticle}' → '{dto.LegalArticle}'");
                rule.LegalArticle = dto.LegalArticle.Trim();
            }

            // Mise à jour de CanBeDiscontinuous
            if (dto.CanBeDiscontinuous.HasValue && dto.CanBeDiscontinuous != rule.CanBeDiscountinuous)
            {
                changes.Add($"CanBeDiscontinuous: {rule.CanBeDiscountinuous} → {dto.CanBeDiscontinuous}");
                rule.CanBeDiscountinuous = dto.CanBeDiscontinuous.Value;
            }

            // Mise à jour de MustBeUsedWithinDays
            if (dto.MustBeUsedWithinDays.HasValue && dto.MustBeUsedWithinDays != rule.MustBeUsedWithinDays)
            {
                changes.Add($"MustBeUsedWithinDays: {rule.MustBeUsedWithinDays} → {dto.MustBeUsedWithinDays}");
                rule.MustBeUsedWithinDays = dto.MustBeUsedWithinDays;
            }

            if (changes.Any())
            {
                rule.ModifiedAt = DateTimeOffset.UtcNow;
                rule.ModifiedBy = userId;
                await _db.SaveChangesAsync();

                // Logger l'événement
                var companyId = rule.LeaveType.CompanyId ?? 0;
                await _leaveEventLogService.LogSimpleEventAsync(
                    companyId,
                    LeaveEventLogService.EventNames.LegalRuleUpdated,
                    null,
                    string.Join(", ", changes),
                    userId
                );
            }

            var readDto = new LeaveTypeLegalRuleReadDto
            {
                Id = rule.Id,
                LeaveTypeId = rule.LeaveTypeId,
                EventCaseCode = rule.EventCaseCode,
                Description = rule.Description,
                DaysGranted = rule.DaysGranted,
                LegalArticle = rule.LegalArticle,
                CanBeDiscontinuous = rule.CanBeDiscountinuous,
                MustBeUsedWithinDays = rule.MustBeUsedWithinDays
            };

            return Ok(readDto);
        }

        /// <summary>
        /// Supprime une règle légale (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _db.LeaveTypeLegalRules
                .Include(r => r.LeaveType)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rule == null)
                return NotFound(new { Message = "Règle légale non trouvée" });

            // Vérifier les dépendances: LeaveRequest utilisant cette règle
            var hasLeaveRequests = await _db.LeaveRequests
                .AnyAsync(lr => lr.LegalRuleId == id && lr.DeletedAt == null);

            if (hasLeaveRequests)
            {
                return BadRequest(new { Message = "Impossible de supprimer cette règle car elle est utilisée dans des demandes de congés" });
            }

            var userId = User.GetUserId();

            // Note: Le modèle n'a pas de DeletedAt, on supprime directement
            _db.LeaveTypeLegalRules.Remove(rule);
            await _db.SaveChangesAsync();

            // Logger l'événement
            var companyId = rule.LeaveType.CompanyId ?? 0;
            await _leaveEventLogService.LogSimpleEventAsync(
                companyId,
                LeaveEventLogService.EventNames.LegalRuleDeleted,
                $"Règle légale '{rule.EventCaseCode}' supprimée",
                null,
                userId
            );

            return NoContent();
        }
    }
}
