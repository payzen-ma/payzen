using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    public class ContractTypeCreateDto
    {
        [Required(ErrorMessage = "Le nom du type de contrat est requis")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom du type de contrat doit contenir entre 2 et 100 caractères")]
        public required string ContractTypeName { get; set; }

        [Required(ErrorMessage = "L'identifiant de la société est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'identifiant de la société doit être valide")]
        public int CompanyId { get; set; }
        
        public int? LegalContractTypeId { get; set; }

        public int? StateEmploymentProgramId { get; set; }

    }
}
