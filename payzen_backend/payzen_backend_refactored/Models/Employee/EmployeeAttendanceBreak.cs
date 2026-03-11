namespace payzen_backend.Models.Employee
{
    public class EmployeeAttendanceBreak
    {
        public int Id { get; set; }
        public int EmployeeAttendanceId { get; set; }
        public EmployeeAttendance? EmployeeAttendance { get; set; }
        public TimeOnly BreakStart { get; set; }
        public TimeOnly? BreakEnd { get; set; } // Nullable pour supporter les pauses ouvertes
        public string? BreakType { get; set; }  // ex: "Lunch", "Coffee", etc.
        
        // Audit
        public DateTimeOffset CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }
    }
}
