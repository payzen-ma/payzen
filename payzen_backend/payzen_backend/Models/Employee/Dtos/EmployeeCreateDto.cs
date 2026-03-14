using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    /// <summary>
    /// DTO pour cr�er un employ� avec toutes les informations associ�es
    /// </summary>
    public class EmployeeCreateDto
    {
        // ========== Informations de base Employee (Obligatoires) ==========
        
        [Required(ErrorMessage = "Le pr�nom est requis")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le pr�nom doit contenir entre 2 et 500 caract�res")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "Le nom de famille est requis")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom de famille doit contenir entre 2 et 500 caract�res")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "Le num�ro CIN est requis")]
        [StringLength(500, ErrorMessage = "Le num�ro CIN ne peut pas d�passer 500 caract�res")]
        public required string CinNumber { get; set; }

        [Required(ErrorMessage = "La date de naissance est requise")]
        public required DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "Le num�ro de t�l�phone est requis")]
        [StringLength(20, ErrorMessage = "Le num�ro de t�l�phone ne peut pas d�passer 20 caract�res")]
        public required string Phone { get; set; }

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(500, ErrorMessage = "L'email ne peut pas d�passer 500 caract�res")]
        public required string Email { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la soci�t� doit �tre valide")]
        public int? CompanyId { get; set; }

        [Required(ErrorMessage = "L'ID du statut est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du statut doit �tre valide")]
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

        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la cat�gorie doit �tre valide")]
        public int? CategoryId { get; set; }

        // ========== Informations Adresse (Optionnelles) ==========

        [StringLength(10, ErrorMessage = "Le code t�l�phonique ne peut pas d�passer 10 caract�res")]
        public string? CountryPhoneCode { get; set; }
        
        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        
        [StringLength(500, ErrorMessage = "L'adresse ligne 1 ne peut pas d�passer 500 caract�res")]
        public string? AddressLine1 { get; set; }
        
        [StringLength(500, ErrorMessage = "L'adresse ligne 2 ne peut pas d�passer 500 caract�res")]
        public string? AddressLine2 { get; set; }

        [StringLength(20, ErrorMessage = "Le code postal ne peut pas d�passer 20 caract�res")]
        public string? ZipCode { get; set; } = string.Empty;

        // ========== Informations Contrat (Optionnelles) ==========
        
        public int? JobPositionId { get; set; }
        public int? ContractTypeId { get; set; }
        public DateTime? StartDate { get; set; }

        // ========== Informations Salaire (Optionnelles) ==========
        
        [Range(0, double.MaxValue, ErrorMessage = "Le salaire doit �tre positif")]
        public decimal? Salary { get; set; }        
        [Range(0, double.MaxValue, ErrorMessage = "Le salaire horaire doit être positif")]
        public decimal? SalaryHourly { get; set; }        /// <summary>
        /// Date d'effet du salaire. Si non renseign\u00e9e, la date de d\u00e9but de contrat (StartDate) est utilis\u00e9e.
        /// Permet d'onboarder un salari\u00e9 avec une date de salaire ant\u00e9rieure \u00e0 la date du jour.
        /// </summary>
        public DateTime? SalaryEffectiveDate { get; set; }
        // ========== Informations Compte Utilisateur (Optionnelles) ==========
        
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caract�res")]
        public string? Password { get; set; }
        
        public bool CreateUserAccount { get; set; } = true;
    }
}
