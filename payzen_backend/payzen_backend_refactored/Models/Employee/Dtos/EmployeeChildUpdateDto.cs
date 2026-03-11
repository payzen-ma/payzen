using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeChildUpdateDto
    {
        [Required(ErrorMessage = "Le pr�nom est requis")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le pr�nom doit contenir entre 2 et 100 caract�res")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caract�res")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "La date de naissance est requise")]
        public required DateTime DateOfBirth { get; set; }

        public int? GenderId { get; set; }

        public bool IsDependent { get; set; }

        public bool IsStudent { get; set; }
    }
}