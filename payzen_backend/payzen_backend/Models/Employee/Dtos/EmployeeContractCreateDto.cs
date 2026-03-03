using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeContractCreateDto
    {
        [Required(ErrorMessage = "L'ID de l'employ� est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'employ� doit �tre valide")]
        public required int EmployeeId { get; set; }

        [Required(ErrorMessage = "L'ID de la soci�t� est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la soci�t� doit �tre valide")]
        public required int CompanyId { get; set; }

        [Required(ErrorMessage = "L'ID du poste est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du poste doit �tre valide")]
        public required int JobPositionId { get; set; }

        [Required(ErrorMessage = "L'ID du type de contrat est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du type de contrat doit �tre valide")]
        public required int ContractTypeId { get; set; }

        [Required(ErrorMessage = "La date de d�but est requise")]
        public required DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}