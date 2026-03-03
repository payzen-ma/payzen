using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Referentiel.Dtos
{
    /// <summary>
    /// DTO pour la lecture d'une nationalité
    /// </summary>
    public class NationalityReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour la création d'une nationalité
    /// </summary>
    public class NationalityCreateDto
    {
        [Required(ErrorMessage = "Le nom de la nationalité est requis")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
        public required string Name { get; set; }
    }

    /// <summary>
    /// DTO pour la mise à jour d'une nationalité
    /// </summary>
    public class NationalityUpdateDto
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères")]
        public string? Name { get; set; }
    }
}
