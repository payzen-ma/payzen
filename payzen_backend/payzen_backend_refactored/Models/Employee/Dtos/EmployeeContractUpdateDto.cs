using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeContractUpdateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du poste doit �tre valide")]
        public int? JobPositionId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "L'ID du type de contrat doit �tre valide")]
        public int? ContractTypeId { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}