using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    public class HolidayCreateDto
    {
        [Required(ErrorMessage = "Le nom en fran�ais est requis")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caract�res")]
        public required string NameFr { get; set; }

        [Required(ErrorMessage = "Le nom en arabe est requis")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caract�res")]
        public required string NameAr { get; set; }

        [Required(ErrorMessage = "Le nom en anglais est requis")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caract�res")]
        public required string NameEn { get; set; }

        [Required(ErrorMessage = "La date du jour f�ri� est requise")]
        public required DateOnly HolidayDate { get; set; }

        [StringLength(1000, ErrorMessage = "La description ne peut pas d�passer 1000 caract�res")]
        public string? Description { get; set; }

        public int? CompanyId { get; set; } // NULL = Holiday global (au niveau pays)

        [Required(ErrorMessage = "L'ID du pays est requis")]
        public int CountryId { get; set; }

        [Required(ErrorMessage = "Le scope est requis")]
        public HolidayScope Scope { get; set; } = HolidayScope.Global;

        [Required(ErrorMessage = "Le type de jour f�ri� est requis")]
        [StringLength(50, ErrorMessage = "Le type ne peut pas d�passer 50 caract�res")]
        public required string HolidayType { get; set; } // National, Religieux, Company, etc.

        public bool IsMandatory { get; set; } = true;
        public bool IsPaid { get; set; } = true;
        public bool IsRecurring { get; set; } = false;

        [StringLength(500, ErrorMessage = "La r�gle de r�currence ne peut pas d�passer 500 caract�res")]
        public string? RecurrenceRule { get; set; }

        [Range(2020, 2100, ErrorMessage = "L'ann�e doit �tre entre 2020 et 2100")]
        public int? Year { get; set; }

        public bool AffectPayroll { get; set; } = true;
        public bool AffectAttendance { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}
