namespace payzen_backend.Models.Employee
{
    public class EmployeeAttendance
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public DateOnly WorkDate { get; set; }  // le jour du pointage
        public TimeOnly? CheckIn { get; set; }
        public TimeOnly? CheckOut { get; set; }

        // Pause appliquée (depuis CompanyPolicyVersion)
        public int BreakMinutesApplied { get; set; }

        public AttendanceStatus Status { get; set; }
        public AttendanceSource Source { get; set; }

        // Calculé automatiquement
        public decimal WorkedHours { get; set; }

        // Audit
        public DateTimeOffset CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }

        // Collection des pauses
        public ICollection<EmployeeAttendanceBreak>? Breaks { get; set; }
    }

    public enum AttendanceStatus
    {
        Present = 1,
        Absent = 2,
        Holiday = 3,
        Leave = 4
    }

    public enum AttendanceSource
    {
        System = 1,
        Manual = 2
    }
}
