namespace Payzen.Domain.Enums;

public enum PaymentFrequency
{
    DAILY,
    MONTHLY,
    QUARTERLY,
    ANNUAL,
    ONE_TIME,
}

public enum ExemptionType
{
    FULLY_EXEMPT,
    FULLY_SUBJECT,
    CAPPED,
    PERCENTAGE,
    PERCENTAGE_CAPPED,
    FORMULA,
    FORMULA_CAPPED,
    TIERED,
    DUAL_CAP,
}

public enum DualCapLogic
{
    MIN,
    MAX,
}

public enum CapUnit
{
    PER_DAY,
    PER_MONTH,
    PER_YEAR,
}

public enum BaseReference
{
    BASE_SALARY, // Salaire de base only
    GROSS_SALARY, // Salaire brut (includes primes)
    SBI, // Salaire Brut Imposable
}

public enum ElementStatus
{
    DRAFT,
    ACTIVE,
    ARCHIVED,
}

/// <summary>Régime CIMR (DSL @ENUM RegimeCIMR)</summary>
public enum RegimeCimr
{
    AUCUN,
    AL_KAMIL,
    AL_MOUNASSIB,
}
