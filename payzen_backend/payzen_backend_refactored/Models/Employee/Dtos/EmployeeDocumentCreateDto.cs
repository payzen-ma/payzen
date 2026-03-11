using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeDocumentCreateDto
    {
        [Required(ErrorMessage = "L'ID de l'employ� est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'employ� doit �tre valide")]
        public required int EmployeeId { get; set; }

        [Required(ErrorMessage = "Le nom du document est requis")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caract�res")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Le chemin du fichier est requis")]
        [StringLength(1000, ErrorMessage = "Le chemin ne peut pas d�passer 1000 caract�res")]
        public required string FilePath { get; set; }

        public DateTime? ExpirationDate { get; set; }

        [Required(ErrorMessage = "Le type de document est requis")]
        [StringLength(100, ErrorMessage = "Le type ne peut pas d�passer 100 caract�res")]
        public required string DocumentType { get; set; }
    }
}