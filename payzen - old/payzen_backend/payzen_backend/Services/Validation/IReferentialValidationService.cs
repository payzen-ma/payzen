using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.Services.Validation
{
    /// <summary>
    /// Service for validating referential elements and rules
    /// </summary>
    public interface IReferentialValidationService
    {
        /// <summary>
        /// Validate an element for creation or update
        /// </summary>
        Task<ValidationResult> ValidateElementAsync(ReferentielElement element, bool isUpdate = false);

        /// <summary>
        /// Validate an element for activation (stricter validation)
        /// </summary>
        Task<ValidationResult> ValidateElementForActivationAsync(int elementId);

        /// <summary>
        /// Validate a rule for creation or update
        /// </summary>
        Task<ValidationResult> ValidateRuleAsync(ElementRule rule, bool isUpdate = false);

        /// <summary>
        /// Validate rule JSON details match exemption type schema
        /// </summary>
        ValidationResult ValidateRuleDetailsJson(ExemptionType exemptionType, string ruleDetailsJson);

        /// <summary>
        /// Check for overlapping date ranges for same element+authority
        /// </summary>
        Task<ValidationResult> CheckDateRangeOverlapAsync(int elementId, int authorityId, DateOnly effectiveFrom, DateOnly? effectiveTo, int? excludeRuleId = null);
    }
}
