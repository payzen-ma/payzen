using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Referentiel.Dtos
{
    public class CityUpdateDto
    {
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caract�res")]
        public string? CityName { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "L'ID du pays doit �tre valide")]
        public int? CountryId { get; set; }
    }
}
