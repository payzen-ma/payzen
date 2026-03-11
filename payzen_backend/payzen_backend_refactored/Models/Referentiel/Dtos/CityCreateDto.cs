using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Referentiel.Dtos
{
    public class CityCreateDto
    {
        [Required(ErrorMessage = "Le nom de la ville est requis")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caract�res")]
        public required string CityName { get; set; }

        [Required(ErrorMessage = "L'ID du pays est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du pays doit �tre valide")]
        public required int CountryId { get; set; }
    }
}