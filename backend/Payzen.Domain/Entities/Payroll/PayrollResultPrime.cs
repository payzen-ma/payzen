using System.ComponentModel.DataAnnotations.Schema;
using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Payroll;

/// <summary>
/// Détail d'une prime imposable ou non pour un résultat de paie.
/// Permet de stocker un nombre illimité de primes par fiche de paie.
/// </summary>
public class PayrollResultPrime : BaseEntity
{
    public int PayrollResultId { get; set; }

    public string Label { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Montant { get; set; }

    public int Ordre { get; set; }

    public bool IsTaxable { get; set; }

    // Navigation
    public PayrollResult PayrollResult { get; set; } = null!;
}
