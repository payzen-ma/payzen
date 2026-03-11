using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Payroll.Referentiel
{
    public class BusinessSector
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public required string Code { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Name { get; set; }

        public bool IsStandard { get; set; } = false;
        public int SortOrder { get; set; } = 0;

        // Audit fields
        public DateTimeOffset CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        // Navigation
        public ICollection<SalaryPackage> SalaryPackages { get; set; } = new List<SalaryPackage>();
    }
}
