using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    public class HolidayUpdateDto
    {
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caract�res")]
        public string? NameFr { get; set; }

        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caract�res")]
        public string? NameAr { get; set; }

        [StringLength(200, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 200 caract�res")]
        public string? NameEn { get; set; }

        public DateOnly? HolidayDate { get; set; }

        [StringLength(1000, ErrorMessage = "La description ne peut pas d�passer 1000 caract�res")]
        public string? Description { get; set; }

        public int? CountryId { get; set; }
        public HolidayScope? Scope { get; set; }

        [StringLength(50, ErrorMessage = "Le type ne peut pas d�passer 50 caract�res")]
        public string? HolidayType { get; set; }

        public bool? IsMandatory { get; set; }
        public bool? IsPaid { get; set; }
        public bool? IsRecurring { get; set; }

        [StringLength(500, ErrorMessage = "La r�gle de r�currence ne peut pas d�passer 500 caract�res")]
        public string? RecurrenceRule { get; set; }

        [Range(2020, 2100, ErrorMessage = "L'ann�e doit �tre entre 2020 et 2100")]
        public int? Year { get; set; }

        public bool? AffectPayroll { get; set; }
        public bool? AffectAttendance { get; set; }
        public bool? IsActive { get; set; }
    }
}
