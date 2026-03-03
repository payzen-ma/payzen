using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Payroll.Dtos
{
    /// <summary>
    /// DTO for duplicating a salary package template
    /// </summary>
    public class SalaryPackageDuplicateDto
    {
        /// <summary>
        /// Optional new name for the duplicated template
        /// If not provided, defaults to "{OriginalName} (Copy)"
        /// </summary>
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
        public string? Name { get; set; }
    }
}
