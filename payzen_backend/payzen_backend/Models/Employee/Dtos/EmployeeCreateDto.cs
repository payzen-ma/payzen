using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    /// <summary>
    /// DTO pour créer un employé avec toutes les informations associées
    /// </summary>
    public class EmployeeCreateDto
    {
        // ========== Informations de base Employee (Obligatoires) ==========
        
        [Required(ErrorMessage = "Le prénom est requis")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le prénom doit contenir entre 2 et 500 caractères")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom de famille est requis")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom de famille doit contenir entre 2 et 500 caractères")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "Le numéro CIN est requis")]
        [StringLength(500, ErrorMessage = "Le numéro CIN ne peut pas dépasser 500 caractères")]
        public required string CinNumber { get; set; }

        [Required(ErrorMessage = "La date de naissance est requise")]
        public required DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "Le numéro de téléphone est requis")]
        [StringLength(20, ErrorMessage = "Le numéro de téléphone ne peut pas dépasser 20 caractères")]
        public required string Phone { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(500, ErrorMessage = "L'email ne peut pas dépasser 500 caractères")]
        public required string Email { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la société doit être valide")]
        public int? CompanyId { get; set; }

        [Required(ErrorMessage = "L'ID du statut est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du statut doit être valide")]
        public required int StatusId { get; set; }

        // ========== Informations Employee (Optionnelles) ==========
        
        public int? DepartementId { get; set; }
        public int? ManagerId { get; set; }
        public int? GenderId { get; set; }
        public int? NationalityId { get; set; }
        public int? EducationLevelId { get; set; }
        public int? MaritalStatusId { get; set; }
        public string? CnssNumber { get; set; }
        public string? CimrNumber { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la catégorie doit être valide")]
        public int? CategoryId { get; set; }

        // ========== Informations Adresse (Optionnelles) ==========

        [StringLength(10, ErrorMessage = "Le code téléphonique ne peut pas dépasser 10 caractères")]
        public string? CountryPhoneCode { get; set; }
        
        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        
        [StringLength(500, ErrorMessage = "L'adresse ligne 1 ne peut pas dépasser 500 caractères")]
        public string? AddressLine1 { get; set; }
        
        [StringLength(500, ErrorMessage = "L'adresse ligne 2 ne peut pas dépasser 500 caractères")]
        public string? AddressLine2 { get; set; }

        [StringLength(20, ErrorMessage = "Le code postal ne peut pas dépasser 20 caractères")]
        public string? ZipCode { get; set; } = string.Empty;

        // ========== Informations Contrat (Optionnelles) ==========
        
        public int? JobPositionId { get; set; }
        public int? ContractTypeId { get; set; }
        public DateTime? StartDate { get; set; }

        // ========== Informations Salaire (Optionnelles) ==========
        
        [Range(0, double.MaxValue, ErrorMessage = "Le salaire doit être positif")]
        public decimal? Salary { get; set; }

        // ========== Informations Compte Utilisateur (Optionnelles) ==========
        
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
        public string? Password { get; set; }
        
        public bool CreateUserAccount { get; set; } = true;
    }
}
