using payzen_backend.Models.Payroll.Referentiel;

namespace payzen_backend.Models.Payroll
{
    public class SalaryPackageItem
    {
        public int Id { get; set; }
        public int SalaryPackageId { get; set; }
        public int? PayComponentId { get; set; } // Reference to global component catalog (optional)
        /// <summary>Reference to referentiel element for rule-driven payroll (CNSS/IR/CIMR).</summary>
        public int? ReferentielElementId { get; set; }
        public required string Label { get; set; }
        public decimal DefaultValue { get; set; }
        public int SortOrder { get; set; }

        // Moroccan regulatory fields (2026 compliance)
        public required string Type { get; set; } = "allowance"; // base_salary, allowance, bonus, benefit_in_kind, social_charge
        public bool IsTaxable { get; set; } = true; // Subject to IR (Impot sur le Revenu)
        public bool IsSocial { get; set; } = true; // Subject to CNSS contributions
        public bool IsCIMR { get; set; } = false; // Subject to CIMR contributions
        public bool IsVariable { get; set; } = false; // Monthly estimate vs fixed amount
        public decimal? ExemptionLimit { get; set; } // Cap for exemptions (e.g., Transport 500/750 MAD)

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation properties
        public SalaryPackage? SalaryPackage { get; set; }
        public PayComponent? PayComponent { get; set; }
        public ReferentielElement? ReferentielElement { get; set; }
    }
}
