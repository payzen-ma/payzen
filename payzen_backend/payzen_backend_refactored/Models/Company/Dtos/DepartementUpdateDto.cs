using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    /// <summary>
    /// DTO pour mettre à jour un département (tous les champs sont optionnels)
    /// </summary>
    public class DepartementUpdateDto
    {
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom du département doit contenir entre 2 et 500 caractères")]
        public string? DepartementName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")]
        public int? CompanyId { get; set; }
    }
}
