using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Payroll.Dtos
{
    /// <summary>
    /// DTO for cloning a global template into a tenant-owned package
    /// </summary>
    public class SalaryPackageCloneDto
    {
        /// <summary>
        /// Target company ID for the cloned package
        /// </summary>
        [Required(ErrorMessage = "Company ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Company ID must be valid")]
        public int CompanyId { get; set; }

        /// <summary>
        /// Optional custom name for the cloned package (defaults to original name)
        /// </summary>
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
        public string? Name { get; set; }

        /// <summary>
        /// Optional effective start date for the cloned package
        /// </summary>
        public DateTime? ValidFrom { get; set; }
    }
}
