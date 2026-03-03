using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.DTOs.Payroll.Referentiel;
using payzen_backend.Extensions;
using payzen_backend.Models.Payroll.Referentiel;
using payzen_backend.Services.Convergence;
using payzen_backend.Services.Validation;

namespace payzen_backend.Controllers.Payroll.Referentiel
{
    /// <summary>
    /// API Controller for managing Referentiel Elements (Transport, Panier, Représentation, etc.)
    /// </summary>
    [Route("api/payroll/referentiel-elements")]
    [ApiController]
    [Authorize]
    public class ReferentielElementsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConvergenceAnalysisService _convergenceService;
        private readonly IReferentialValidationService _validationService;

        public ReferentielElementsController(
            AppDbContext db,
            IConvergenceAnalysisService convergenceService,
            IReferentialValidationService validationService)
        {
            _db = db;
            _convergenceService = convergenceService;
            _validationService = validationService;
        }

        /// <summary>
        /// Get all referentiel elements (summary view with hybrid loading strategy)
        /// GET /api/payroll/referentiel-elements?status=ACTIVE&categoryId=1&search=transport&showHistory=false
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<ReferentielElementListDto>>> GetAll(
            [FromQuery] ElementStatus? status = null,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] bool showHistory = false)
        {
            var query = _db.ReferentielElements
                .AsNoTracking()
                .Include(e => e.Category)
                .Include(e => e.Rules.Where(r => r.Status == ElementStatus.ACTIVE))
                    .ThenInclude(r => r.Authority)
                .Where(e => e.IsActive)
                .AsQueryable();

            // Filter by status
            if (status.HasValue)
            {
                query = query.Where(e => e.Status == status.Value);
            }
            else if (!showHistory)
            {
                // Default: only show ACTIVE and DRAFT, hide ARCHIVED
                query = query.Where(e => e.Status != ElementStatus.ARCHIVED);
            }

            // Filter by category
            if (categoryId.HasValue)
            {
                query = query.Where(e => e.CategoryId == categoryId.Value);
            }

            // Filter by search term (name or description)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(e =>
                    e.Name.ToLower().Contains(searchLower) ||
                    (e.Description != null && e.Description.ToLower().Contains(searchLower)));
            }

            var elements = await query
                .OrderBy(e => e.Category.SortOrder)
                .ThenBy(e => e.Name)
                .ToListAsync();

            var items = elements.Select(e =>
            {
                var activeRules = e.Rules.Where(r => r.Status == ElementStatus.ACTIVE).ToList();
                var hasCnss = activeRules.Any(r => r.Authority.Code == "CNSS");
                var hasDgi = activeRules.Any(r => r.Authority.Code == "DGI");

                return new ReferentielElementListDto
                {
                    Id = e.Id,
                    Code = e.Code,
                    Name = e.Name,
                    CategoryName = e.Category.Name,
                    DefaultFrequency = e.DefaultFrequency,
                    Status = e.Status,
                    IsActive = e.IsActive,
                    HasConvergence = e.HasConvergence,
                    RuleCount = activeRules.Count,
                    HasCnssRule = hasCnss,
                    HasDgiRule = hasDgi
                };
            }).ToList();

            return Ok(items);
        }

        /// <summary>
        /// Get a referentiel element by ID with full details including rules
        /// GET /api/payroll/referentiel-elements/1
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<ReferentielElementDto>> GetById(int id)
        {
            var element = await _db.ReferentielElements
                .AsNoTracking()
                .Include(e => e.Category)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Authority)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Cap)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Percentage)
                        .ThenInclude(p => p!.Eligibility)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Formula)
                        .ThenInclude(f => f!.Parameter)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Tiers)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Variants)
                        .ThenInclude(v => v.Eligibility)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (element == null)
                return NotFound(new { Message = "Element not found" });

            var paramValues = await GetParameterValuesEffectiveAt(DateOnly.FromDateTime(DateTime.UtcNow));
            var dto = MapToDto(element, paramValues);
            return Ok(dto);
        }

        /// <summary>
        /// Create a new referentiel element
        /// POST /api/payroll/referentiel-elements
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<ReferentielElementDto>> Create([FromBody] CreateReferentielElementDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.GetUserId();

            var entity = new ReferentielElement
            {
                Code = dto.Code?.Trim(),
                Name = dto.Name.Trim(),
                CategoryId = dto.CategoryId,
                Description = dto.Description?.Trim(),
                DefaultFrequency = dto.DefaultFrequency,
                Status = dto.Status,
                HasConvergence = false,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            // Validate element
            var validation = await _validationService.ValidateElementAsync(entity);
            if (!validation.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Validation failed",
                    Errors = validation.Errors,
                    Warnings = validation.Warnings
                });
            }

            _db.ReferentielElements.Add(entity);
            await _db.SaveChangesAsync();

            // Reload with navigation properties (include rule details for IsConvergence)
            var created = await _db.ReferentielElements
                .AsNoTracking()
                .Include(e => e.Category)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Authority)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Percentage)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Cap)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Formula)
                .FirstAsync(e => e.Id == entity.Id);

            var paramValuesCreate = await GetParameterValuesEffectiveAt(DateOnly.FromDateTime(DateTime.UtcNow));
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(created, paramValuesCreate));
        }

        /// <summary>
        /// Update a referentiel element
        /// PUT /api/payroll/referentiel-elements/1
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<ReferentielElementDto>> Update(int id, [FromBody] UpdateReferentielElementDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _db.ReferentielElements
                .Include(e => e.Category)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Authority)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Percentage)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Cap)
                .Include(e => e.Rules)
                    .ThenInclude(r => r.Formula)
                        .ThenInclude(f => f!.Parameter)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entity == null)
                return NotFound(new { Message = "Element not found" });

            // Verify category exists and is active (Phase 1.2)
            var category = await _db.ElementCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == dto.CategoryId);
            if (category == null)
                return BadRequest(new { Message = "Category not found" });
            if (!category.IsActive)
                return BadRequest(new { Message = "Cette catégorie est inactive et ne peut pas être utilisée." });

            // Check duplicate (Name, CategoryId) when name or category changes - Phase 1.3
            var nameChanged = entity.Name.Trim().ToLower() != dto.Name.Trim().ToLower() || entity.CategoryId != dto.CategoryId;
            if (nameChanged)
            {
                var duplicateExists = await _db.ReferentielElements
                    .AsNoTracking()
                    .AnyAsync(e => e.DeletedAt == null && e.Id != id &&
                        e.CategoryId == dto.CategoryId &&
                        e.Name.ToLower() == dto.Name.Trim().ToLower());
                if (duplicateExists)
                    return Conflict(new { Message = "Un élément avec ce nom existe déjà dans cette catégorie." });
            }

            entity.Name = dto.Name.Trim();
            entity.CategoryId = dto.CategoryId;
            entity.Description = dto.Description?.Trim();
            entity.DefaultFrequency = dto.DefaultFrequency;
            entity.IsActive = dto.IsActive;
            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedBy = User.GetUserId();

            await _db.SaveChangesAsync();

            var paramValuesUpdate = await GetParameterValuesEffectiveAt(DateOnly.FromDateTime(DateTime.UtcNow));
            return Ok(MapToDto(entity, paramValuesUpdate));
        }

        /// <summary>
        /// Soft-delete a referentiel element
        /// DELETE /api/payroll/referentiel-elements/1
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.ReferentielElements
                .Include(e => e.Rules)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (entity == null)
                return NotFound(new { Message = "Element not found" });

            // Check if element is used in active salary components or assignments
            // TODO: Add validation when salary component feature is implemented

            entity.DeletedAt = DateTimeOffset.UtcNow;
            entity.DeletedBy = User.GetUserId();

            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Get rules for a specific element
        /// GET /api/payroll/referentiel-elements/1/rules
        /// </summary>
        [HttpGet("{id}/rules")]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<ElementRuleDto>>> GetRules(int id)
        {
            var exists = await _db.ReferentielElements.AnyAsync(e => e.Id == id);
            if (!exists)
                return NotFound(new { Message = "Element not found" });

            var rules = await _db.ElementRules
                .AsNoTracking()
                .Include(r => r.Authority)
                .Include(r => r.Cap)
                .Include(r => r.Percentage)
                    .ThenInclude(p => p!.Eligibility)
                .Include(r => r.Formula)
                    .ThenInclude(f => f!.Parameter)
                .Include(r => r.Tiers)
                .Include(r => r.Variants)
                    .ThenInclude(v => v.Eligibility)
                .Where(r => r.ElementId == id)
                .OrderBy(r => r.Authority.Name)
                .ThenBy(r => r.EffectiveFrom)
                .ToListAsync();

            var paramValues = await GetParameterValuesEffectiveAt(DateOnly.FromDateTime(DateTime.UtcNow));
            var dtos = rules.Select(r => MapRuleToDto(r, GetResolvedFormulaCap(r, paramValues))).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Analyze convergence between CNSS and DGI rules for an element
        /// GET /api/payroll/referentiel-elements/1/analyze-convergence
        /// </summary>
        [HttpPost("{id}/analyze-convergence")]
        [Produces("application/json")]
        public async Task<ActionResult<object>> AnalyzeConvergence(int id, [FromQuery] DateOnly? asOfDate = null)
        {
            try
            {
                var result = await _convergenceService.AnalyzeElementAsync(id, asOfDate);

                return Ok(new
                {
                    elementId = id,
                    isConvergent = result.IsConvergent,
                    summary = result.Summary,
                    checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    cnssRuleId = result.CnssRuleId,
                    dgiRuleId = result.DgiRuleId,
                    differences = result.Differences
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Update element status (DRAFT -> ACTIVE -> ARCHIVED)
        /// PATCH /api/payroll/referentiel-elements/1/status
        /// </summary>
        [HttpPatch("{id}/status")]
        [Produces("application/json")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateElementStatusDto dto)
        {
            var element = await _db.ReferentielElements
                .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

            if (element == null)
                return NotFound(new { Message = "Element not found" });

            // If activating, perform stricter validation
            if (dto.Status == ElementStatus.ACTIVE && element.Status == ElementStatus.DRAFT)
            {
                var validation = await _validationService.ValidateElementForActivationAsync(id);
                if (!validation.IsValid)
                {
                    return BadRequest(new
                    {
                        Message = "Cannot activate element: validation failed",
                        Errors = validation.Errors,
                        Warnings = validation.Warnings
                    });
                }

                // If there are warnings but no errors, allow activation but return warnings
                if (validation.Warnings.Any())
                {
                    element.Status = dto.Status;
                    element.ModifiedAt = DateTimeOffset.UtcNow;
                    element.ModifiedBy = User.GetUserId();
                    await _db.SaveChangesAsync();

                    return Ok(new
                    {
                        Message = "Element activated with warnings",
                        Warnings = validation.Warnings
                    });
                }
            }

            element.Status = dto.Status;
            element.ModifiedAt = DateTimeOffset.UtcNow;
            element.ModifiedBy = User.GetUserId();
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Status updated successfully", NewStatus = dto.Status });
        }

        /// <summary>
        /// Recalculate convergence for all active elements
        /// POST /api/payroll/referentiel-elements/recalculate-convergence
        /// </summary>
        [HttpPost("recalculate-convergence")]
        [Produces("application/json")]
        public async Task<ActionResult> RecalculateAllConvergence()
        {
            var updated = await _convergenceService.RecalculateAllConvergenceAsync();

            return Ok(new
            {
                Message = "Convergence recalculated successfully",
                ElementsUpdated = updated
            });
        }

        /// <summary>
        /// Check convergence between CNSS and DGI rules for an element (legacy endpoint)
        /// GET /api/payroll/referentiel-elements/1/convergence
        /// </summary>
        [HttpGet("{id}/convergence")]
        [Produces("application/json")]
        public async Task<ActionResult<object>> CheckConvergence(int id, [FromQuery] DateOnly? asOfDate = null)
        {
            try
            {
                var result = await _convergenceService.AnalyzeElementAsync(id, asOfDate);

                return Ok(new
                {
                    elementId = id,
                    isConvergence = result.IsConvergent,
                    checkDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    cnssRuleId = result.CnssRuleId,
                    dgiRuleId = result.DgiRuleId
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        #region Private Helpers

        /// <summary>
        /// Phase 2: Get parameter values effective at the given date (by Label).
        /// </summary>
        private async Task<Dictionary<string, decimal>> GetParameterValuesEffectiveAt(DateOnly asOfDate)
        {
            var parameters = await _db.LegalParameters
                .AsNoTracking()
                .Where(lp => lp.DeletedAt == null &&
                    lp.EffectiveFrom <= asOfDate &&
                    (lp.EffectiveTo == null || lp.EffectiveTo >= asOfDate))
                .ToListAsync();
            return parameters
                .GroupBy(p => p.Code)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFrom).First().Value);
        }

        private static decimal? GetResolvedFormulaCap(ElementRule rule, Dictionary<string, decimal> paramValues)
        {
            if (rule.Formula == null) return null;
            var code = rule.Formula.Parameter?.Code;
            if (string.IsNullOrEmpty(code)) return rule.Formula.CalculateCurrentCap();
            if (paramValues.TryGetValue(code, out var value))
                return value * rule.Formula.Multiplier;
            return rule.Formula.CalculateCurrentCap();
        }

        private static ReferentielElementDto MapToDto(ReferentielElement entity, Dictionary<string, decimal>? paramValues = null)
        {
            return new ReferentielElementDto
            {
                Id = entity.Id,
                Name = entity.Name,
                CategoryId = entity.CategoryId,
                CategoryName = entity.Category.Name,
                Description = entity.Description,
                DefaultFrequency = entity.DefaultFrequency,
                IsActive = entity.IsActive,
                HasConvergence = entity.HasConvergence,
                Rules = entity.Rules.Select(r => MapRuleToDto(r, paramValues != null ? GetResolvedFormulaCap(r, paramValues) : null)).ToList()
            };
        }

        private static ElementRuleDto MapRuleToDto(ElementRule rule, decimal? formulaCurrentCap = null)
        {
            return new ElementRuleDto
            {
                Id = rule.Id,
                ElementId = rule.ElementId,
                AuthorityId = rule.AuthorityId,
                AuthorityName = rule.Authority.Name,
                ExemptionType = rule.ExemptionType,
                SourceRef = rule.SourceRef,
                EffectiveFrom = rule.EffectiveFrom,
                EffectiveTo = rule.EffectiveTo,
                IsActive = rule.IsActive(null),
                Cap = rule.Cap != null ? new RuleCapDto
                {
                    Id = rule.Cap.Id,
                    CapAmount = rule.Cap.CapAmount,
                    CapUnit = rule.Cap.CapUnit
                } : null,
                Percentage = rule.Percentage != null ? new RulePercentageDto
                {
                    Id = rule.Percentage.Id,
                    Percentage = rule.Percentage.Percentage,
                    BaseReference = rule.Percentage.BaseReference,
                    EligibilityId = rule.Percentage.EligibilityId,
                    EligibilityName = rule.Percentage.Eligibility?.Name
                } : null,
                Formula = rule.Formula != null ? new RuleFormulaDto
                {
                    Id = rule.Formula.Id,
                    Multiplier = rule.Formula.Multiplier,
                    ParameterId = rule.Formula.ParameterId,
                    ParameterName = rule.Formula.Parameter.Label,
                    ResultUnit = rule.Formula.ResultUnit,
                    CurrentCapValue = formulaCurrentCap ?? rule.Formula.CalculateCurrentCap()
                } : null,
                Tiers = rule.Tiers.Select(t => new RuleTierDto
                {
                    Id = t.Id,
                    TierOrder = t.TierOrder,
                    MinAmount = t.FromAmount,
                    MaxAmount = t.ToAmount,
                    ExemptionRate = t.ExemptPercent
                }).ToList(),
                Variants = rule.Variants.OrderBy(v => v.SortOrder).Select(v => new RuleVariantDto
                {
                    Id = v.Id,
                    VariantType = v.VariantType,
                    VariantKey = v.VariantKey,
                    VariantLabel = v.VariantLabel,
                    OverrideCap = v.OverrideCap,
                    OverrideEligibilityId = v.EligibilityId,
                    OverrideEligibilityName = v.Eligibility?.Name
                }).ToList()
            };
        }

        #endregion
    }
}
