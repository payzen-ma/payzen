using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeSalaryComponentUpdateDto
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le type doit contenir entre 2 et 100 caract�res")]
        public string? ComponentType { get; set; }

        public decimal? Amount { get; set; }

        public bool? IsTaxable { get; set; }

        public DateTime? EffectiveDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
