namespace payzen_backend.Models.Payroll.Referentiel
{
    /// <summary>
    /// Payment frequency for compensation elements
    /// </summary>
    public enum PaymentFrequency
    {
        DAILY,
        MONTHLY,
        QUARTERLY,
        ANNUAL,
        ONE_TIME
    }

    /// <summary>
    /// Type of exemption calculation
    /// </summary>
    public enum ExemptionType
    {
        FULLY_EXEMPT,       // 100% exempt, no cap
        FULLY_SUBJECT,      // 0% exempt, fully taxable
        CAPPED,             // Exempt up to a fixed cap
        PERCENTAGE,         // Exempt as % of base salary
        PERCENTAGE_CAPPED,  // % of base with max cap
        FORMULA,            // Dynamic calculation (e.g., 2 × SMIG)
        FORMULA_CAPPED,     // Formula with max cap
        TIERED,             // Multiple tiers with different rates
        DUAL_CAP            // Fixed cap AND percentage cap (both must be respected, e.g., DGI ticket-restaurant)
    }

    /// <summary>
    /// Logic for combining dual caps
    /// </summary>
    public enum DualCapLogic
    {
        MIN,    // Take the minimum of (fixed, percentage) - most restrictive
        MAX     // Take the maximum of (fixed, percentage) - most favorable
    }

    /// <summary>
    /// Unit for cap amounts
    /// </summary>
    public enum CapUnit
    {
        PER_DAY,
        PER_MONTH,
        PER_YEAR
    }

    /// <summary>
    /// Base reference for percentage calculations
    /// </summary>
    public enum BaseReference
    {
        BASE_SALARY,        // Salaire de base only
        GROSS_SALARY,       // Salaire brut (includes primes)
        SBI                 // Salaire Brut Imposable
    }

    /// <summary>
    /// Status of an element or rule (draft, active, archived)
    /// </summary>
    public enum ElementStatus
    {
        DRAFT,      // Work in progress, not yet active
        ACTIVE,     // Currently in use
        ARCHIVED    // No longer in use
    }
}
