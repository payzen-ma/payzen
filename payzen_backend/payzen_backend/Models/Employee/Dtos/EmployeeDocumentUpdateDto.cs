using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeDocumentUpdateDto
    {
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caract�res")]
        public string? Name { get; set; }

        [StringLength(1000, ErrorMessage = "Le chemin ne peut pas d�passer 1000 caract�res")]
        public string? FilePath { get; set; }

        public DateTime? ExpirationDate { get; set; }

        [StringLength(100, ErrorMessage = "Le type ne peut pas d�passer 100 caract�res")]
        public string? DocumentType { get; set; }
    }
}