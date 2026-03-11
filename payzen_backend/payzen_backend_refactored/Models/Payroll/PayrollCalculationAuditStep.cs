namespace payzen_backend.Models.Payroll;

/// <summary>
/// Un enregistrement d'audit pour une étape du calcul de paie (un module).
/// Permet de tracer la formule utilisée et les entrées/sorties.
/// </summary>
public class PayrollCalculationAuditStep
{
    public int Id { get; set; }
    public int PayrollResultId { get; set; }

    /// <summary>Ordre d'exécution du module (1 à 13).</summary>
    public int StepOrder { get; set; }

    /// <summary>Nom du module (ex. "Module01_Anciennete", "Module06_Cnss").</summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>Description de la formule appliquée (texte lisible).</summary>
    public string FormulaDescription { get; set; } = string.Empty;

    /// <summary>Entrées du module (JSON : valeurs utilisées pour le calcul).</summary>
    public string? InputsJson { get; set; }

    /// <summary>Sorties du module (JSON : résultats du calcul).</summary>
    public string? OutputsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public PayrollResult PayrollResult { get; set; } = null!;
}
