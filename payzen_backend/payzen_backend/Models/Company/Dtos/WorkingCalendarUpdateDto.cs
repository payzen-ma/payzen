using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    public class WorkingCalendarUpdateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la soci�t� doit �tre valide")]
        public int? CompanyId { get; set; }

        [Range(0, 6, ErrorMessage = "Le jour doit �tre entre 0 (Dimanche) et 6 (Samedi)")]
        public int? DayOfWeek { get; set; }

        public bool? IsWorkingDay { get; set; }

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }
    }
}