using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Referentiel.Dtos
{
    public class CountryUpdateDto
    {
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caract�res")]
        public string? CountryName { get; set; }

        [StringLength(500, ErrorMessage = "Le nom arabe ne peut pas d�passer 500 caract�res")]
        public string? CountryNameAr { get; set; }

        [StringLength(3, MinimumLength = 2, ErrorMessage = "Le code doit contenir 2 ou 3 caract�res")]
        public string? CountryCode { get; set; }

        [StringLength(10, ErrorMessage = "Le code t�l�phonique ne peut pas d�passer 10 caract�res")]
        public string? CountryPhoneCode { get; set; }
    }
}