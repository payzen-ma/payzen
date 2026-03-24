namespace payzen_backend.Services
{
    /// <summary>
    /// Resolves legal parameters and element rules for payroll calculation.
    /// Used by the payroll engine to get parameter values by label+date, rules by element+authority, and exempt amounts per line.
    /// </summary>
    public interface IElementRuleResolutionService
    {
        /// <summary>
        /// Get the effective value of a legal parameter by Code at the given date.
        /// Returns null if no parameter is found for that code and date.
        /// </summary>
        Task<decimal?> GetParameterValueEffectiveAtAsync(string code, DateOnly asOfDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all parameter values effective at the given date, keyed by Code.
        /// Use this to pass into ComputeExemptAmount for formula resolution.
        /// </summary>
        Task<IReadOnlyDictionary<string, decimal>> GetParameterValuesEffectiveAtAsync(DateOnly asOfDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get applicable element rules for the given element and authority at the given date.
        /// Returns rules with Cap, Formula (with Parameter), Percentage, DualCap, Tiers, and Variants loaded.
        /// </summary>
        Task<IReadOnlyList<Models.Payroll.Referentiel.ElementRule>> GetRulesForElementAuthorityEffectiveAtAsync(
            int elementId,
            int authorityId,
            DateOnly asOfDate,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolve authority ID by code (e.g. "CNSS", "DGI", "CIMR"). Returns null if not found.
        /// </summary>
        Task<int?> GetAuthorityIdByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Compute the exempt amount for a salary line given the rule, line amount, and context.
        /// Returns the amount that is exempt from the given authority (taxable amount = lineAmount - exempt).
        /// </summary>
        /// <param name="rule">Element rule with details loaded (Cap, Formula, Percentage, DualCap, Tiers, Variants).</param>
        /// <param name="lineAmount">The gross amount of the line (e.g. item.DefaultValue).</param>
        /// <param name="baseSalary">Base salary (salaire de base) for PERCENTAGE/DUAL_CAP when BaseReference is BASE_SALARY.</param>
        /// <param name="grossSalary">Gross salary (salaire brut) for PERCENTAGE when BaseReference is GROSS_SALARY.</param>
        /// <param name="sbi">Salaire brut imposable for PERCENTAGE/DUAL_CAP when BaseReference is SBI.</param>
        /// <param name="paramValuesByCode">Parameter values by Code (e.g. from GetParameterValuesEffectiveAtAsync). Formula rules use these.</param>
        /// <param name="workingDaysPerMonth">Used to convert PER_DAY caps to monthly (default 26).</param>
        decimal ComputeExemptAmount(
            Models.Payroll.Referentiel.ElementRule rule,
            decimal lineAmount,
            decimal? baseSalary,
            decimal? grossSalary,
            decimal? sbi,
            IReadOnlyDictionary<string, decimal>? paramValuesByCode,
            int workingDaysPerMonth = 26);
    }
}
