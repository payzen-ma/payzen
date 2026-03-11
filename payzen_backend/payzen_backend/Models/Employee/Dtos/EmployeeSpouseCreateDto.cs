using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeSpouseCreateDto
    {
        [Required(ErrorMessage = "L'ID de l'employ� est requis")]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Le pr�nom est requis")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le pr�nom doit contenir entre 2 et 100 caract�res")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caract�res")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "La date de naissance est requise")]
        public required DateTime DateOfBirth { get; set; }

        public int? GenderId { get; set; }

        [StringLength(50, ErrorMessage = "Le num�ro CIN ne peut pas d�passer 50 caract�res")]
        public string? CinNumber { get; set; }

        public DateTime? MarriageDate { get; set; }
        public bool IsDependent { get; set; } = false;
    }
}
