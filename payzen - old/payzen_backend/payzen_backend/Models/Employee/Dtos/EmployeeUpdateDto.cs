using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeUpdateDto
    {
        // ===== DONN�ES PRINCIPALES (Employee) =====
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le pr�nom doit contenir entre 2 et 500 caract�res")]
        public string? FirstName { get; set; }

        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom de famille doit contenir entre 2 et 500 caract�res")]
        public string? LastName { get; set; }

        [StringLength(500, ErrorMessage = "Le num�ro CIN ne peut pas d�passer 500 caract�res")]
        public string? CinNumber { get; set; }

        public DateOnly? DateOfBirth { get; set; }
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(500, ErrorMessage = "L'email ne peut pas d�passer 500 caract�res")]
        public string? Email { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "L'ID du departement doit �tre valide")]
        public int? DepartementId { get; set; }

        public int? ManagerId { get; set; }
        public int? StatusId { get; set; }
        public int? GenderId { get; set; }
        public int? NationalityId { get; set; }
        public int? EducationLevelId { get; set; }
        public int? MaritalStatusId { get; set; }        
        public int? CategoryId { get; set; }
        public int? CnssNumber { get; set; }
        public int? CimrNumber { get; set; }

        // ===== DONN�ES AVEC HISTORIQUE =====
        // Contrat - Si fourni, cr�era un nouveau contrat et fermera l'ancien
        public int? JobPositionId { get; set; }
        public int? ContractTypeId { get; set; }
        public DateTime? ContractStartDate { get; set; }

        // Salaire - Si fourni, cr�era un nouveau salaire et fermera l'ancien
        [Range(0.01, double.MaxValue, ErrorMessage = "Le salaire doit �tre sup�rieur � 0")]
        public decimal? Salary { get; set; }
        public DateTime? SalaryEffectiveDate { get; set; }

        // Adresse - Si fourni, cr�era une nouvelle adresse
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? ZipCode { get; set; }
        public int? CityId { get; set; }
    }
}
