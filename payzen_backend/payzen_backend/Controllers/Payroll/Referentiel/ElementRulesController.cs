using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.DTOs.Payroll.Referentiel;
using payzen_backend.Extensions;
using payzen_backend.Models.Payroll.Referentiel;
using payzen_backend.Services.Validation;
using payzen_backend.Services.Convergence;

namespace payzen_backend.Controllers.Payroll.Referentiel
{
    /// <summary>
    /// API Controller for managing Element Rules (exemption rules for CNSS, IR, etc.)
    /// </summary>
    [Route("api/payroll/element-rules")]
    [ApiController]
    [Authorize]
    public class ElementRulesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IReferentialValidationService _validationService;
        private readonly IConvergenceAnalysisService _convergenceService;

        public ElementRulesController(
            AppDbContext db,
            IReferentialValidationService validationService,
            IConvergenceAnalysisService convergenceService)
        {
            _db = db;
            _validationService = validationService;
            _convergenceService = convergenceService;
        }

        /// <summary>
        /// Get all element rules (with optional filtering)
        /// GET /api/payroll/element-rules?elementId=1&authorityId=1
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<ElementRuleDto>>> GetAll(
            [FromQuery] int? elementId = null,
            [FromQuery] int? authorityId = null)
        {
            var query = _db.ElementRules
                .AsNoTracking()
                .Include(r => r.Authority)
                .Include(r => r.Cap)
                .Include(r => r.Percentage)
                    .ThenInclude(p => p!.Eligibility)
                .Include(r => r.Formula)
                    .ThenInclude(f => f!.Parameter)
                .Include(r => r.DualCap)
                .Include(r => r.Tiers)
                .Include(r => r.Variants)
                    .ThenInclude(v => v.Eligibility)
                .AsQueryable();

            if (elementId.HasValue)
                query = query.Where(r => r.ElementId == elementId.Value);

            if (authorityId.HasValue)
                query = query.Where(r => r.AuthorityId == authorityId.Value);

            var rules = await query
                .OrderBy(r => r.Authority.Code)
                .ThenBy(r => r.EffectiveFrom)
                .ToListAsync();

            // Phase 2: Resolve formula CurrentCapValue by parameter Label + date (today for display)
            var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var paramValues = await GetParameterValuesEffectiveAt(asOfDate);
            var dtos = rules.Select(r => MapToDto(r, GetResolvedFormulaCap(r, paramValues))).ToList();
            return Ok(dtos);
        }

        /// <summary>
        /// Get an element rule by ID
        /// GET /api/payroll/element-rules/1
        /// </summary>
        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<ElementRuleDto>> GetById(int id)
        {
            var rule = await _db.ElementRules
                .AsNoTracking()
                .Include(r => r.Authority)
                .Include(r => r.Cap)
                .Include(r => r.Percentage)
                    .ThenInclude(p => p!.Eligibility)
                .Include(r => r.Formula)
                    .ThenInclude(f => f!.Parameter)
                .Include(r => r.DualCap)
                .Include(r => r.Tiers)
                .Include(r => r.Variants)
                    .ThenInclude(v => v.Eligibility)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rule == null)
                return NotFound(new { Message = "Element rule not found" });

            // Phase 2: Resolve formula CurrentCapValue by parameter Label + date (today for display)
            var asOfDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var paramValues = await GetParameterValuesEffectiveAt(asOfDate);
            return Ok(MapToDto(rule, GetResolvedFormulaCap(rule, paramValues)));
        }

        /// <summary>
        /// Create a new element rule
        /// POST /api/payroll/element-rules
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        public async Task<ActionResult<ElementRuleDto>> Create([FromBody] CreateElementRuleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate that required rule detail is present for the given ExemptionType (Phase 1.1)
            var (valid, validationMessage) = ValidateRuleDetailsForCreate(dto);
            if (!valid)
                return BadRequest(new { Message = validationMessage });

            // Verify element exists
            var elementExists = await _db.ReferentielElements.AnyAsync(e => e.Id == dto.ElementId);
            if (!elementExists)
                return BadRequest(new { Message = "Element not found" });

            // Resolve authority (ID preferred, fallback to code for hardcoded UI)
            var authorityId = dto.AuthorityId;
            if (authorityId <= 0 && !string.IsNullOrWhiteSpace(dto.AuthorityCode))
            {
                authorityId = await _db.Authorities
                    .AsNoTracking()
                    .Where(a => a.Code.ToLower() == dto.AuthorityCode.ToLower())
                    .Select(a => a.Id)
                    .FirstOrDefaultAsync();
            }

            if (authorityId <= 0)
                return BadRequest(new { Message = "Authority not found" });

            // Reject inactive authority for new rules (Phase 1.2)
            var authorityActive = await _db.Authorities
                .AsNoTracking()
                .Where(a => a.Id == authorityId)
                .Select(a => a.IsActive)
                .FirstOrDefaultAsync();
            if (!authorityActive)
                return BadRequest(new { Message = "Cette autorité est inactive et ne peut pas être utilisée pour une nouvelle règle." });

            var userId = User.GetUserId();

            var entity = new ElementRule
            {
                ElementId = dto.ElementId,
                AuthorityId = authorityId,
                ExemptionType = dto.ExemptionType,
                SourceRef = dto.SourceRef?.Trim(),
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                Status = dto.Status,
                RuleDetails = dto.RuleDetails,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId
            };

            // Validate using new validation service
            var validation = await _validationService.ValidateRuleAsync(entity, false);
            if (!validation.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Validation failed",
                    Errors = validation.Errors,
                    Warnings = validation.Warnings
                });
            }

            _db.ElementRules.Add(entity);
            await _db.SaveChangesAsync();

            // Add legacy rule details based on exemption type (for backward compatibility)
            await AddRuleDetails(entity, dto, userId);

            // Trigger convergence recalculation for this element
            await _convergenceService.RecalculateElementConvergenceAsync(dto.ElementId);

            // Reload with all navigation properties
            var created = await _db.ElementRules
                .AsNoTracking()
                .Include(r => r.Authority)
                .Include(r => r.Cap)
                .Include(r => r.Percentage)
                    .ThenInclude(p => p!.Eligibility)
                .Include(r => r.Formula)
                    .ThenInclude(f => f!.Parameter)
                .Include(r => r.DualCap)
                .Include(r => r.Tiers)
                .Include(r => r.Variants)
                    .ThenInclude(v => v.Eligibility)
                .FirstAsync(r => r.Id == entity.Id);

            var paramValues = await GetParameterValuesEffectiveAt(DateOnly.FromDateTime(DateTime.UtcNow));
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, MapToDto(created, GetResolvedFormulaCap(created, paramValues)));
        }

        /// <summary>
        /// Update an element rule
        /// PUT /api/payroll/element-rules/1
        /// </summary>
        [HttpPut("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<ElementRuleDto>> Update(int id, [FromBody] UpdateElementRuleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _db.ElementRules
                .Include(r => r.Authority)
                .Include(r => r.Cap)
                .Include(r => r.Percentage)
                    .ThenInclude(p => p!.Eligibility)
                .Include(r => r.Formula)
                    .ThenInclude(f => f!.Parameter)
                .Include(r => r.DualCap)
                .Include(r => r.Tiers)
                .Include(r => r.Variants)
                    .ThenInclude(v => v.Eligibility)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null)
                return NotFound(new { Message = "Element rule not found" });

            // Validate that required rule detail is present for the effective ExemptionType (Phase 1.1)
            var effectiveType = dto.ExemptionType ?? entity.ExemptionType;
            var (valid, validationMessage) = ValidateRuleDetailsForUpdate(dto, effectiveType);
            if (!valid)
                return BadRequest(new { Message = validationMessage });

            var userId = User.GetUserId();
            var elementId = entity.ElementId;

            // Update basic properties (only if provided)
            if (dto.ExemptionType.HasValue)
                entity.ExemptionType = dto.ExemptionType.Value;

            // Allow clearing SourceRef by setting to null when not provided
            entity.SourceRef = dto.SourceRef?.Trim();

            if (dto.EffectiveFrom.HasValue)
                entity.EffectiveFrom = dto.EffectiveFrom.Value;

            if (dto.EffectiveTo.HasValue)
                entity.EffectiveTo = dto.EffectiveTo.Value;

            if (dto.Status.HasValue)
                entity.Status = dto.Status.Value;

            if (dto.RuleDetails != null)
                entity.RuleDetails = dto.RuleDetails;

            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedBy = userId;

            // Validate using new validation service
            var validation = await _validationService.ValidateRuleAsync(entity, true);
            if (!validation.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Validation failed",
                    Errors = validation.Errors,
                    Warnings = validation.Warnings
                });
            }

            // Clear existing rule details
            if (entity.Cap != null)
            {
                _db.RuleCaps.Remove(entity.Cap);
                entity.Cap = null;
            }

            if (entity.Percentage != null)
            {
                _db.RulePercentages.Remove(entity.Percentage);
                entity.Percentage = null;
            }

            if (entity.Formula != null)
            {
                _db.RuleFormulas.Remove(entity.Formula);
                entity.Formula = null;
            }

            if (entity.DualCap != null)
            {
                _db.RuleDualCaps.Remove(entity.DualCap);
                entity.DualCap = null;
            }

            foreach (var tier in entity.Tiers.ToList())
            {
                _db.RuleTiers.Remove(tier);
            }
            entity.Tiers.Clear();

            foreach (var variant in entity.Variants.ToList())
            {
                _db.RuleVariants.Remove(variant);
            }
            entity.Variants.Clear();

            await _db.SaveChangesAsync();

            // Add new rule details based on exemption type (legacy compatibility)
            if (dto.ExemptionType.HasValue)
                await AddRuleDetails(entity, dto, userId);

            // Trigger convergence recalculation for this element
            await _convergenceService.RecalculateElementConvergenceAsync(elementId);

            // Reload to get all updated details
            var updated = await _db.ElementRules
                .AsNoTracking()
                .Include(r => r.Authority)
                .Include(r => r.Cap)
                .Include(r => r.Percentage)
                    .ThenInclude(p => p!.Eligibility)
                .Include(r => r.Formula)
                    .ThenInclude(f => f!.Parameter)
                .Include(r => r.DualCap)
                .Include(r => r.Tiers)
                .Include(r => r.Variants)
                    .ThenInclude(v => v.Eligibility)
                .FirstAsync(r => r.Id == id);

            var paramValuesUpdate = await GetParameterValuesEffectiveAt(DateOnly.FromDateTime(DateTime.UtcNow));
            return Ok(MapToDto(updated, GetResolvedFormulaCap(updated, paramValuesUpdate)));
        }

        /// <summary>
        /// Validate rule details JSON against exemption type schema
        /// POST /api/payroll/element-rules/validate
        /// </summary>
        [HttpPost("validate")]
        [Produces("application/json")]
        public ActionResult<ValidationResult> ValidateRule([FromBody] ValidateRuleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var validation = _validationService.ValidateRuleDetailsJson(dto.ExemptionType, dto.RuleDetails);

            // Also check date range overlap if element and authority are provided
            if (dto.ElementId.HasValue && dto.AuthorityId.HasValue)
            {
                var overlapCheck = _validationService.CheckDateRangeOverlapAsync(
                    dto.ElementId.Value,
                    dto.AuthorityId.Value,
                    dto.EffectiveFrom,
                    dto.EffectiveTo,
                    null).Result;

                if (!overlapCheck.IsValid)
                {
                    validation.Errors.AddRange(overlapCheck.Errors);
                    validation.IsValid = false;
                }
            }

            return Ok(validation);
        }

        /// <summary>
        /// Update rule status (DRAFT → ACTIVE → ARCHIVED)
        /// PATCH /api/payroll/element-rules/{id}/status
        /// </summary>
        [HttpPatch("{id}/status")]
        [Produces("application/json")]
        public async Task<ActionResult<ElementRuleDto>> UpdateStatus(int id, [FromBody] UpdateElementStatusDto dto)
        {
            var rule = await _db.ElementRules
                .Include(r => r.Authority)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rule == null)
                return NotFound(new { Message = "Element rule not found" });

            // Validate the rule before activation
            if (dto.Status == ElementStatus.ACTIVE && rule.Status != ElementStatus.ACTIVE)
            {
                var validation = await _validationService.ValidateRuleAsync(rule, true);
                if (!validation.IsValid)
                {
                    return BadRequest(new
                    {
                        Message = "Cannot activate rule with validation errors",
                        Errors = validation.Errors,
                        Warnings = validation.Warnings
                    });
                }
            }

            var userId = User.GetUserId();
            rule.Status = dto.Status;
            rule.ModifiedAt = DateTimeOffset.UtcNow;
            rule.ModifiedBy = userId;

            await _db.SaveChangesAsync();

            // Trigger convergence recalculation for this element
            await _convergenceService.RecalculateElementConvergenceAsync(rule.ElementId);

            // Reload with navigation properties
            var updated = await _db.ElementRules
                .AsNoTracking()
                .Include(r => r.Authority)
                .Include(r => r.Cap)
                .Include(r => r.Percentage)
                    .ThenInclude(p => p!.Eligibility)
                .Include(r => r.Formula)
                    .ThenInclude(f => f!.Parameter)
                .Include(r => r.DualCap)
                .Include(r => r.Tiers)
                .Include(r => r.Variants)
                    .ThenInclude(v => v.Eligibility)
                .FirstAsync(r => r.Id == id);

            var paramValues = await GetParameterValuesEffectiveAt(DateOnly.FromDateTime(DateTime.UtcNow));
            return Ok(MapToDto(updated, GetResolvedFormulaCap(updated, paramValues)));
        }

        /// <summary>
        /// Delete an element rule
        /// DELETE /api/payroll/element-rules/1
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.ElementRules
                .Include(r => r.Cap)
                .Include(r => r.Percentage)
                .Include(r => r.Formula)
                .Include(r => r.DualCap)
                .Include(r => r.Tiers)
                .Include(r => r.Variants)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (entity == null)
                return NotFound(new { Message = "Element rule not found" });

            var elementId = entity.ElementId;

            // Remove all rule details
            if (entity.Cap != null)
                _db.RuleCaps.Remove(entity.Cap);

            if (entity.Percentage != null)
                _db.RulePercentages.Remove(entity.Percentage);

            if (entity.Formula != null)
                _db.RuleFormulas.Remove(entity.Formula);

            if (entity.DualCap != null)
                _db.RuleDualCaps.Remove(entity.DualCap);

            foreach (var tier in entity.Tiers)
                _db.RuleTiers.Remove(tier);

            foreach (var variant in entity.Variants)
                _db.RuleVariants.Remove(variant);

            _db.ElementRules.Remove(entity);
            await _db.SaveChangesAsync();

            // Trigger convergence recalculation for this element
            await _convergenceService.RecalculateElementConvergenceAsync(elementId);

            return NoContent();
        }

        #region Private Helpers

        /// <summary>
        /// Validates that the required rule detail is present for the given ExemptionType (Phase 1.1 - DB integrity).
        /// </summary>
        private static (bool Valid, string? ErrorMessage) ValidateRuleDetailsForCreate(CreateElementRuleDto dto)
        {
            return ValidateRuleDetails(
                dto.ExemptionType,
                dto.Cap != null,
                dto.Percentage != null,
                dto.Formula != null,
                dto.DualCap != null,
                dto.Tiers?.Count > 0);
        }

        /// <summary>
        /// Validates that the provided details match the effective ExemptionType on update (Phase 1.1).
        /// </summary>
        private static (bool Valid, string? ErrorMessage) ValidateRuleDetailsForUpdate(UpdateElementRuleDto dto, ExemptionType effectiveType)
        {
            return ValidateRuleDetails(
                effectiveType,
                dto.Cap != null,
                dto.Percentage != null,
                dto.Formula != null,
                dto.DualCap != null,
                dto.Tiers?.Count > 0);
        }

        private static (bool Valid, string? ErrorMessage) ValidateRuleDetails(
            ExemptionType exemptionType,
            bool hasCap,
            bool hasPercentage,
            bool hasFormula,
            bool hasDualCap,
            bool hasTiers)
        {
            switch (exemptionType)
            {
                case ExemptionType.FULLY_EXEMPT:
                case ExemptionType.FULLY_SUBJECT:
                    return (true, null);
                case ExemptionType.CAPPED:
                    return hasCap ? (true, null) : (false, "Le type CAPPED requiert un plafond (Cap).");
                case ExemptionType.PERCENTAGE:
                    return hasPercentage ? (true, null) : (false, "Le type PERCENTAGE requiert un pourcentage (Percentage).");
                case ExemptionType.PERCENTAGE_CAPPED:
                    return hasPercentage && hasCap
                        ? (true, null)
                        : (false, "Le type PERCENTAGE_CAPPED requiert un pourcentage et un plafond.");
                case ExemptionType.FORMULA:
                    return hasFormula ? (true, null) : (false, "Le type FORMULA requiert une formule (Formula).");
                case ExemptionType.FORMULA_CAPPED:
                    return hasFormula && hasCap
                        ? (true, null)
                        : (false, "Le type FORMULA_CAPPED requiert une formule et un plafond.");
                case ExemptionType.TIERED:
                    return hasTiers ? (true, null) : (false, "Le type TIERED requiert au moins une tranche (Tiers).");
                case ExemptionType.DUAL_CAP:
                    return hasDualCap ? (true, null) : (false, "Le type DUAL_CAP requiert un double plafond (DualCap).");
                default:
                    return (false, $"Type d'exonération non reconnu: {exemptionType}");
            }
        }

        private async Task AddRuleDetails(ElementRule rule, dynamic dto, int userId)
        {
            switch (rule.ExemptionType)
            {
                case ExemptionType.CAPPED:
                    if (dto.Cap != null)
                    {
                        rule.Cap = new RuleCap
                        {
                            RuleId = rule.Id,
                            CapAmount = dto.Cap.CapAmount,
                            CapUnit = dto.Cap.CapUnit,
                            MinAmount = dto.Cap.MinAmount
                        };
                    }
                    break;

                case ExemptionType.PERCENTAGE:
                    if (dto.Percentage != null)
                    {
                        rule.Percentage = new RulePercentage
                        {
                            RuleId = rule.Id,
                            Percentage = dto.Percentage.Percentage,
                            BaseReference = dto.Percentage.BaseReference,
                            EligibilityId = dto.Percentage.EligibilityId
                        };
                    }
                    break;

                case ExemptionType.PERCENTAGE_CAPPED:
                    if (dto.Percentage != null)
                    {
                        rule.Percentage = new RulePercentage
                        {
                            RuleId = rule.Id,
                            Percentage = dto.Percentage.Percentage,
                            BaseReference = dto.Percentage.BaseReference,
                            EligibilityId = dto.Percentage.EligibilityId
                        };
                    }
                    if (dto.Cap != null)
                    {
                        rule.Cap = new RuleCap
                        {
                            RuleId = rule.Id,
                            CapAmount = dto.Cap.CapAmount,
                            CapUnit = dto.Cap.CapUnit,
                            MinAmount = dto.Cap.MinAmount
                        };
                    }
                    break;

                case ExemptionType.FORMULA:
                    if (dto.Formula != null)
                    {
                        rule.Formula = new RuleFormula
                        {
                            RuleId = rule.Id,
                            Multiplier = dto.Formula.Multiplier,
                            ParameterId = dto.Formula.ParameterId,
                            ResultUnit = dto.Formula.ResultUnit
                        };
                    }
                    break;

                case ExemptionType.FORMULA_CAPPED:
                    if (dto.Formula != null)
                    {
                        rule.Formula = new RuleFormula
                        {
                            RuleId = rule.Id,
                            Multiplier = dto.Formula.Multiplier,
                            ParameterId = dto.Formula.ParameterId,
                            ResultUnit = dto.Formula.ResultUnit
                        };
                    }
                    if (dto.Cap != null)
                    {
                        rule.Cap = new RuleCap
                        {
                            RuleId = rule.Id,
                            CapAmount = dto.Cap.CapAmount,
                            CapUnit = dto.Cap.CapUnit,
                            MinAmount = dto.Cap.MinAmount
                        };
                    }
                    break;

                case ExemptionType.TIERED:
                    if (dto.Tiers != null && dto.Tiers.Count > 0)
                    {
                        foreach (var tierDto in dto.Tiers)
                        {
                            var tier = new RuleTier
                            {
                                RuleId = rule.Id,
                                TierOrder = tierDto.TierOrder,
                                FromAmount = tierDto.MinAmount ?? 0,
                                ToAmount = tierDto.MaxAmount,
                                ExemptPercent = tierDto.ExemptionRate
                            };
                            rule.Tiers.Add(tier);
                        }
                    }
                    break;

                case ExemptionType.DUAL_CAP:
                    if (dto.DualCap != null)
                    {
                        rule.DualCap = new RuleDualCap
                        {
                            RuleId = rule.Id,
                            FixedCapAmount = dto.DualCap.FixedCapAmount,
                            FixedCapUnit = dto.DualCap.FixedCapUnit,
                            PercentageCap = dto.DualCap.PercentageCap,
                            BaseReference = dto.DualCap.BaseReference,
                            Logic = dto.DualCap.Logic
                        };
                    }
                    break;
            }

            // Add variants (can be used with CAPPED, PERCENTAGE, etc.)
            // For zone-based rules like transport allowance, variants define the caps directly
            if (dto.Variants != null && dto.Variants.Count > 0)
            {
                int sortOrder = 0;
                foreach (var variantDto in dto.Variants)
                {
                    var variant = new RuleVariant
                    {
                        RuleId = rule.Id,
                        VariantType = variantDto.VariantType ?? "ZONE",
                        VariantKey = variantDto.VariantKey,
                        VariantLabel = variantDto.VariantLabel,
                        OverrideCap = variantDto.OverrideCap,
                        EligibilityId = variantDto.OverrideEligibilityId,
                        SortOrder = sortOrder++
                    };
                    rule.Variants.Add(variant);
                }
            }

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Phase 2: Get parameter values effective at the given date (by Label).
        /// Returns Label -> Value so formula rules can use "current" SMIG etc. at display/payroll date.
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

        /// <summary>
        /// Phase 2: Resolve formula cap using parameter value by Label + date (not the stored ParameterId row).
        /// </summary>
        private static decimal? GetResolvedFormulaCap(ElementRule rule, Dictionary<string, decimal> paramValues)
        {
            if (rule.Formula == null) return null;
            var code = rule.Formula.Parameter?.Code;
            if (string.IsNullOrEmpty(code)) return rule.Formula.CalculateCurrentCap();
            if (paramValues.TryGetValue(code, out var value))
                return value * rule.Formula.Multiplier;
            return rule.Formula.CalculateCurrentCap(); // fallback to stored row value
        }

        private static ElementRuleDto MapToDto(ElementRule rule, decimal? formulaCurrentCap = null)
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
                Status = rule.Status,
                RuleDetails = rule.RuleDetails,
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
                DualCap = rule.DualCap != null ? new RuleDualCapDto
                {
                    Id = rule.DualCap.Id,
                    FixedCapAmount = rule.DualCap.FixedCapAmount,
                    FixedCapUnit = rule.DualCap.FixedCapUnit,
                    PercentageCap = rule.DualCap.PercentageCap,
                    BaseReference = rule.DualCap.BaseReference,
                    Logic = rule.DualCap.Logic
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
