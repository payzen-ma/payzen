using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    /// <summary>
    /// Used for ADMIN / MANUAL creation or update
    /// </summary>
    public class EmployeeAttendanceBreakCreateDto
    {
        [Required]
        public int AttendanceId { get; set; }

        [Required]
        public TimeOnly BreakStart { get; set; }

        [Required]
        public TimeOnly BreakEnd { get; set; }

        [MaxLength(50)]
        public string? BreakType { get; set; }
    }

    /// <summary>
    /// Read-only DTO returned by API
    /// </summary>
    public class EmployeeAttendanceBreakReadDto
    {
        public int Id { get; set; }

        public TimeOnly BreakStart { get; set; }

        /// <summary>
        /// Null = break still open
        /// </summary>
        public TimeOnly? BreakEnd { get; set; }

        public string BreakType { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? ModifiedAt { get; set; }
    }

    /// <summary>
    /// Used when employee STARTS a break
    /// </summary>
    public class StartBreakDto
    {
        [Required]
        public int AttendanceId { get; set; }

        [Required]
        public TimeOnly BreakStart { get; set; }

        [MaxLength(50)]
        public string? BreakType { get; set; }
    }

    /// <summary>
    /// Used when employee ENDS a break
    /// </summary>
    public class EndBreakDto
    {
        [Required]
        public TimeOnly BreakEnd { get; set; }
    }
    public class EmployeeBreakDto
    {
        public int Id { get; set; }
        public DateTime BreakStart { get; set; }
        public DateTime? BreakEnd { get; set; }
        public string BreakType { get; set; } = string.Empty;
    }
    public class EmployeeDailyBreaksDto
    {
        public DateTime Date { get; set; }
        public int TotalBreakMinutes { get; set; }
        public int BreakCount { get; set; }
        public List<EmployeeBreakDto> Breaks { get; set; } = new();
    }
}
