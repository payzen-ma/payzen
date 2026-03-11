using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeSalaryCreateDto
    {
        [Required(ErrorMessage = "L'ID de l'employ� est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'employ� doit �tre valide")]
        public required int EmployeeId { get; set; }

        [Required(ErrorMessage = "L'ID du contrat est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du contrat doit �tre valide")]
        public required int ContractId { get; set; }

        [Required(ErrorMessage = "Le salaire de base est requis")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le salaire doit �tre sup�rieur � 0")]
        public required decimal BaseSalary { get; set; }

        [Required(ErrorMessage = "La date d'effet est requise")]
        public required DateTime EffectiveDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
