using System.ComponentModel.DataAnnotations.Schema;

namespace payzen_backend.Models.Payroll
{
    /// <summary>
    /// Détail d'une prime imposable pour un résultat de paie
    /// Permet de stocker un nombre illimité de primes par fiche de paie
    /// </summary>
    public class PayrollResultPrime
    {
        public int Id { get; set; }

        // ========== Relation ==========
        public int PayrollResultId { get; set; }

        // ========== Détails de la prime ==========
        public string Label { get; set; }  // "Prime d'excellence", "Prime de commission", etc.
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }
        
        public int Ordre { get; set; }  // Pour conserver l'ordre d'affichage (1, 2, 3, ...)
        
        public bool IsTaxable { get; set; }  // true pour prime imposable, false pour indemnité

        // ========== Navigation ==========
        public PayrollResult PayrollResult { get; set; }
    }
}
