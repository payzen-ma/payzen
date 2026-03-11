using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    public class ContractTypeUpdateDto
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom du type de contrat doit contenir entre 2 et 100 caractères")]
        public string? ContractTypeName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "L'identifiant du type de contrat légal doit être valide")]
        public int? LegalContractTypeId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "L'identifiant du programme d'emploi d'état doit être valide")]
        public int? StateEmploymentProgramId { get; set; }
    }
}
