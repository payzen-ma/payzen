using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeSalaryCreateDto
    {
        [Required(ErrorMessage = "L'ID de l'employ� est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'employ� doit etre valide")]
        public required int EmployeeId { get; set; }

        [Required(ErrorMessage = "L'ID du contrat est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du contrat doit etre valide")]
        public required int ContractId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Le salaire doit etre superieur a 0")]
        public decimal? BaseSalary { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Le salaire horaire doit etre superieur a 0")]
        public decimal? BaseSalaryHourly { get; set; }

        [Required(ErrorMessage = "La date d'effet est requise")]
        public required DateTime EffectiveDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
