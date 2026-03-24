using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data;
using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.Services.Validation
{
    /// <summary>
    /// Service for validating referential elements and rules
    /// </summary>
    public class ReferentialValidationService : IReferentialValidationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ReferentialValidationService> _logger;

        public ReferentialValidationService(AppDbContext context, ILogger<ReferentialValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<ValidationResult> ValidateElementAsync(ReferentielElement element, bool isUpdate = false)
        {
            var result = new ValidationResult { IsValid = true };

            // Required fields
            if (string.IsNullOrWhiteSpace(element.Name))
            {
                result.AddError("Element name is required");
            }
            else if (element.Name.Length < 3)
            {
                result.AddError("Element name must be at least 3 characters");
            }
            else if (element.Name.Length > 200)
            {
                result.AddError("Element name cannot exceed 200 characters");
            }

            // Check name uniqueness
            var duplicateName = await _context.ReferentielElements
                .AnyAsync(e => e.Name == element.Name && e.Id != element.Id && e.IsActive);

            if (duplicateName)
            {
                result.AddError($"An element with the name '{element.Name}' already exists");
            }

            // Check code uniqueness if provided
            if (!string.IsNullOrWhiteSpace(element.Code))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(element.Code, @"^[a-z_]+$"))
                {
                    result.AddError("Code must contain only lowercase letters and underscores");
                }

                var duplicateCode = await _context.ReferentielElements
                    .AnyAsync(e => e.Code == element.Code && e.Id != element.Id && e.IsActive);

                if (duplicateCode)
                {
                    result.AddError($"An element with the code '{element.Code}' already exists");
                }
            }

            // Category must exist
            if (element.CategoryId > 0)
            {
                var categoryExists = await _context.ElementCategories
                    .AnyAsync(c => c.Id == element.CategoryId && c.IsActive);

                if (!categoryExists)
                {
                    result.AddError("Selected category does not exist or is inactive");
                }
            }
            else
            {
                result.AddError("Category is required");
            }

            // Description length
            if (element.Description?.Length > 1000)
            {
                result.AddError("Description cannot exceed 1000 characters");
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<ValidationResult> ValidateElementForActivationAsync(int elementId)
        {
            var result = new ValidationResult { IsValid = true };

            var element = await _context.ReferentielElements
                .Include(e => e.Rules)
                .ThenInclude(r => r.Authority)
                .FirstOrDefaultAsync(e => e.Id == elementId && e.IsActive);

            if (element == null)
            {
                result.AddError("Element not found");
                return result;
            }

            // Basic validation
            var basicValidation = await ValidateElementAsync(element, true);
            if (!basicValidation.IsValid)
            {
                result.Errors.AddRange(basicValidation.Errors);
            }

            // Check for active rules
            var activeRules = element.Rules.Where(r => r.Status == ElementStatus.ACTIVE).ToList();

            if (activeRules.Count == 0)
            {
                result.AddWarning("Element has no active rules");
            }
            else
            {
                // Check for CNSS and DGI rules
                var hasCnss = activeRules.Any(r => r.Authority.Code == "CNSS");
                var hasDgi = activeRules.Any(r => r.Authority.Code == "DGI");

                if (!hasCnss)
                {
                    result.AddWarning("Element is missing an active CNSS rule");
                }

                if (!hasDgi)
                {
                    result.AddWarning("Element is missing an active DGI rule");
                }
            }

            // Check if element has been in draft for too long
            if (element.Status == ElementStatus.DRAFT)
            {
                var daysInDraft = (DateTimeOffset.UtcNow - element.CreatedAt).Days;
                if (daysInDraft > 60)
                {
                    result.AddWarning($"Element has been in draft status for {daysInDraft} days");
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<ValidationResult> ValidateRuleAsync(ElementRule rule, bool isUpdate = false)
        {
            var result = new ValidationResult { IsValid = true };

            // Required fields
            if (rule.ElementId <= 0)
            {
                result.AddError("Element ID is required");
            }
            else
            {
                // Element must exist
                var elementExists = await _context.ReferentielElements
                    .AnyAsync(e => e.Id == rule.ElementId && e.IsActive);

                if (!elementExists)
                {
                    result.AddError("Referenced element does not exist or is inactive");
                }
            }

            if (rule.AuthorityId <= 0)
            {
                result.AddError("Authority ID is required");
            }
            else
            {
                // Authority must exist
                var authorityExists = await _context.Authorities
                    .AnyAsync(a => a.Id == rule.AuthorityId && a.IsActive);

                if (!authorityExists)
                {
                    result.AddError("Referenced authority does not exist or is inactive");
                }
            }

            // Date validation
            if (rule.EffectiveTo.HasValue && rule.EffectiveTo < rule.EffectiveFrom)
            {
                result.AddError("Effective-to date must be after effective-from date");
            }

            // Check for overlapping date ranges
            var overlapCheck = await CheckDateRangeOverlapAsync(
                rule.ElementId,
                rule.AuthorityId,
                rule.EffectiveFrom,
                rule.EffectiveTo,
                isUpdate ? rule.Id : null);

            if (!overlapCheck.IsValid)
            {
                result.Errors.AddRange(overlapCheck.Errors);
            }

            // Validate RuleDetails JSON
            var jsonValidation = ValidateRuleDetailsJson(rule.ExemptionType, rule.RuleDetails);
            if (!jsonValidation.IsValid)
            {
                result.Errors.AddRange(jsonValidation.Errors);
            }

            // SourceRef validation (warning only)
            if (string.IsNullOrWhiteSpace(rule.SourceRef))
            {
                result.AddWarning("Legal reference (SourceRef) is recommended for compliance");
            }
            else if (rule.SourceRef.Length > 500)
            {
                result.AddError("Legal reference cannot exceed 500 characters");
            }

            // Check if rule has been in draft for too long
            if (rule.Status == ElementStatus.DRAFT && !isUpdate)
            {
                // Only warn on existing drafts, not new ones
            }
            else if (rule.Status == ElementStatus.DRAFT && isUpdate)
            {
                var daysInDraft = (DateTimeOffset.UtcNow - rule.CreatedAt).Days;
                if (daysInDraft > 30)
                {
                    result.AddWarning($"Rule has been in draft status for {daysInDraft} days");
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public ValidationResult ValidateRuleDetailsJson(ExemptionType exemptionType, string ruleDetailsJson)
        {
            var result = new ValidationResult { IsValid = true };

            // Check if JSON is valid
            try
            {
                var json = JsonDocument.Parse(ruleDetailsJson);
                var root = json.RootElement;

                // Validate based on exemption type
                switch (exemptionType)
                {
                    case ExemptionType.FULLY_EXEMPT:
                    case ExemptionType.FULLY_SUBJECT:
                        // No specific fields required
                        break;

                    case ExemptionType.CAPPED:
                        ValidateCapFields(root, result);
                        break;

                    case ExemptionType.PERCENTAGE:
                        ValidatePercentageFields(root, result);
                        break;

                    case ExemptionType.PERCENTAGE_CAPPED:
                        ValidatePercentageFields(root, result);
                        ValidateCapFields(root, result);
                        break;

                    case ExemptionType.FORMULA:
                        ValidateFormulaFields(root, result);
                        break;

                    case ExemptionType.FORMULA_CAPPED:
                        ValidateFormulaFields(root, result);
                        ValidateCapFields(root, result);
                        break;

                    case ExemptionType.DUAL_CAP:
                        ValidateDualCapFields(root, result);
                        break;

                    case ExemptionType.TIERED:
                        ValidateTieredFields(root, result);
                        break;

                    default:
                        result.AddError($"Unknown exemption type: {exemptionType}");
                        break;
                }
            }
            catch (JsonException ex)
            {
                result.AddError($"Invalid JSON format: {ex.Message}");
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<ValidationResult> CheckDateRangeOverlapAsync(
            int elementId,
            int authorityId,
            DateOnly effectiveFrom,
            DateOnly? effectiveTo,
            int? excludeRuleId = null)
        {
            var result = new ValidationResult { IsValid = true };

            var query = _context.ElementRules
                .Where(r => r.ElementId == elementId &&
                           r.AuthorityId == authorityId &&
                           r.Status == ElementStatus.ACTIVE &&
                           r.DeletedAt == null);

            if (excludeRuleId.HasValue)
            {
                query = query.Where(r => r.Id != excludeRuleId.Value);
            }

            var existingRules = await query.ToListAsync();

            foreach (var existingRule in existingRules)
            {
                // Check for overlap
                bool overlaps = false;

                if (effectiveTo.HasValue && existingRule.EffectiveTo.HasValue)
                {
                    // Both have end dates
                    overlaps = effectiveFrom <= existingRule.EffectiveTo &&
                              effectiveTo >= existingRule.EffectiveFrom;
                }
                else if (!effectiveTo.HasValue && !existingRule.EffectiveTo.HasValue)
                {
                    // Both are open-ended
                    overlaps = true;
                }
                else if (!effectiveTo.HasValue)
                {
                    // New rule is open-ended
                    overlaps = existingRule.EffectiveTo >= effectiveFrom;
                }
                else
                {
                    // Existing rule is open-ended
                    overlaps = effectiveTo >= existingRule.EffectiveFrom;
                }

                if (overlaps)
                {
                    result.AddError(
                        $"Date range overlaps with existing rule (ID: {existingRule.Id}, " +
                        $"from {existingRule.EffectiveFrom} to {existingRule.EffectiveTo?.ToString() ?? "current"})");
                }
            }

            return result;
        }

        #region Private Validation Helpers

        private void ValidateCapFields(JsonElement root, ValidationResult result)
        {
            if (!root.TryGetProperty("capAmount", out var capAmount))
            {
                result.AddError("capAmount is required for capped exemption types");
            }
            else if (capAmount.ValueKind == JsonValueKind.Number && capAmount.GetDecimal() < 0)
            {
                result.AddError("capAmount must be a positive number");
            }

            if (!root.TryGetProperty("capUnit", out var capUnit))
            {
                result.AddError("capUnit is required for capped exemption types");
            }
            else if (capUnit.ValueKind == JsonValueKind.String)
            {
                var validUnits = new[] { "PER_DAY", "PER_MONTH", "PER_YEAR" };
                if (!validUnits.Contains(capUnit.GetString()))
                {
                    result.AddError($"capUnit must be one of: {string.Join(", ", validUnits)}");
                }
            }
        }

        private void ValidatePercentageFields(JsonElement root, ValidationResult result)
        {
            if (!root.TryGetProperty("percentage", out var percentage))
            {
                result.AddError("percentage is required for percentage exemption types");
            }
            else if (percentage.ValueKind == JsonValueKind.Number)
            {
                var value = percentage.GetDecimal();
                if (value < 0 || value > 100)
                {
                    result.AddError("percentage must be between 0 and 100");
                }
            }

            if (!root.TryGetProperty("baseReference", out var baseRef))
            {
                result.AddError("baseReference is required for percentage exemption types");
            }
            else if (baseRef.ValueKind == JsonValueKind.String)
            {
                var validRefs = new[] { "BASE_SALARY", "GROSS_SALARY", "SBI" };
                if (!validRefs.Contains(baseRef.GetString()))
                {
                    result.AddError($"baseReference must be one of: {string.Join(", ", validRefs)}");
                }
            }
        }

        private void ValidateFormulaFields(JsonElement root, ValidationResult result)
        {
            if (!root.TryGetProperty("multiplier", out var multiplier))
            {
                result.AddError("multiplier is required for formula exemption types");
            }
            else if (multiplier.ValueKind == JsonValueKind.Number && multiplier.GetDecimal() < 0)
            {
                result.AddError("multiplier must be a positive number");
            }

            if (!root.TryGetProperty("parameterCode", out _))
            {
                result.AddError("parameterCode is required for formula exemption types");
            }
        }

        private void ValidateDualCapFields(JsonElement root, ValidationResult result)
        {
            if (!root.TryGetProperty("fixedCapAmount", out var fixedCap))
            {
                result.AddError("fixedCapAmount is required for dual cap exemption type");
            }
            else if (fixedCap.ValueKind == JsonValueKind.Number && fixedCap.GetDecimal() < 0)
            {
                result.AddError("fixedCapAmount must be a positive number");
            }

            if (!root.TryGetProperty("percentageCap", out var percentCap))
            {
                result.AddError("percentageCap is required for dual cap exemption type");
            }
            else if (percentCap.ValueKind == JsonValueKind.Number)
            {
                var value = percentCap.GetDecimal();
                if (value < 0 || value > 100)
                {
                    result.AddError("percentageCap must be between 0 and 100");
                }
            }

            if (!root.TryGetProperty("logic", out var logic))
            {
                result.AddError("logic is required for dual cap exemption type");
            }
            else if (logic.ValueKind == JsonValueKind.String)
            {
                var validLogic = new[] { "MIN", "MAX" };
                if (!validLogic.Contains(logic.GetString()))
                {
                    result.AddError($"logic must be one of: {string.Join(", ", validLogic)}");
                }
            }
        }

        private void ValidateTieredFields(JsonElement root, ValidationResult result)
        {
            if (!root.TryGetProperty("tiers", out var tiers))
            {
                result.AddError("tiers array is required for tiered exemption type");
                return;
            }

            if (tiers.ValueKind != JsonValueKind.Array)
            {
                result.AddError("tiers must be an array");
                return;
            }

            var tiersArray = tiers.EnumerateArray().ToList();

            if (tiersArray.Count == 0)
            {
                result.AddError("At least one tier is required");
                return;
            }

            decimal? previousTo = null;

            for (int i = 0; i < tiersArray.Count; i++)
            {
                var tier = tiersArray[i];

                if (!tier.TryGetProperty("fromAmount", out var fromAmount))
                {
                    result.AddError($"Tier {i + 1}: fromAmount is required");
                    continue;
                }

                if (!tier.TryGetProperty("exemptPercent", out var exemptPercent))
                {
                    result.AddError($"Tier {i + 1}: exemptPercent is required");
                }
                else if (exemptPercent.ValueKind == JsonValueKind.Number)
                {
                    var value = exemptPercent.GetDecimal();
                    if (value < 0 || value > 100)
                    {
                        result.AddError($"Tier {i + 1}: exemptPercent must be between 0 and 100");
                    }
                }

                // Check for gaps and overlaps
                if (previousTo.HasValue)
                {
                    var currentFrom = fromAmount.GetDecimal();
                    if (currentFrom != previousTo.Value)
                    {
                        if (currentFrom < previousTo.Value)
                        {
                            result.AddError($"Tier {i + 1}: overlaps with previous tier");
                        }
                        else
                        {
                            result.AddError($"Tier {i + 1}: gap between tiers (previous ends at {previousTo.Value}, this starts at {currentFrom})");
                        }
                    }
                }

                if (tier.TryGetProperty("toAmount", out var toAmount) && toAmount.ValueKind == JsonValueKind.Number)
                {
                    previousTo = toAmount.GetDecimal();

                    if (fromAmount.GetDecimal() >= previousTo.Value)
                    {
                        result.AddError($"Tier {i + 1}: fromAmount must be less than toAmount");
                    }
                }
                else
                {
                    previousTo = null; // Open-ended tier
                }
            }
        }

        #endregion
    }
}
