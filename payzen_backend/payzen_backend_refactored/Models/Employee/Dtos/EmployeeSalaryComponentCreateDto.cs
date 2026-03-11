using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeSalaryComponentCreateDto
    {
        [Required(ErrorMessage = "L'ID du salaire est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du salaire doit �tre valide")]
        public required int EmployeeSalaryId { get; set; }

        [Required(ErrorMessage = "Le type de composant est requis")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le type doit contenir entre 2 et 100 caract�res")]
        public required string ComponentType { get; set; }

        [Required(ErrorMessage = "Le montant est requis")]
        public required decimal Amount { get; set; }

        [Required(ErrorMessage = "La date d'effet est requise")]
        public required DateTime EffectiveDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}