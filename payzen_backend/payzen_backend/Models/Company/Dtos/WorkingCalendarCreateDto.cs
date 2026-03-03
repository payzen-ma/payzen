using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    public class WorkingCalendarCreateDto
    {
        [Required(ErrorMessage = "L'ID de la soci�t� est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la soci�t� doit �tre valide")]
        public required int CompanyId { get; set; }

        [Required(ErrorMessage = "Le jour de la semaine est requis")]
        [Range(0, 6, ErrorMessage = "Le jour doit �tre entre 0 (Dimanche) et 6 (Samedi)")]
        public required int DayOfWeek { get; set; }

        [Required(ErrorMessage = "Le statut jour travaill� est requis")]
        public required bool IsWorkingDay { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }
    }
}