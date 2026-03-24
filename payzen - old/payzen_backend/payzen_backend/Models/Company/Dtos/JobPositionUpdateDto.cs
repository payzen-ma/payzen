using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    public class JobPositionUpdateDto
    {
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom du poste doit contenir entre 2 et 200 caractères")]
        public string? Name { get; set; }
    }
}
