using payzen_backend.Models.Employee.Dtos;

namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeAttendanceCreateDto
    {
        public int EmployeeId { get; set; }
        public DateOnly WorkDate { get; set; }
        public TimeOnly? CheckIn { get; set; }
        public TimeOnly? CheckOut { get; set; }
        public AttendanceSource Source { get; set; }
    }

    public class EmployeeAttendanceReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateOnly WorkDate { get; set; }
        public TimeOnly? CheckIn { get; set; }
        public TimeOnly? CheckOut { get; set; }
        public decimal WorkedHours { get; set; }
        public int BreakMinutesApplied { get; set; }
        public AttendanceStatus Status { get; set; }
        public AttendanceSource Source { get; set; }

        // ICI: utiliser le bon type de DTO pour lecture
        public List<EmployeeAttendanceBreakReadDto> Breaks { get; set; } = new();
    }

    public class EmployeeAttendanceCheckDto
    {
        public int EmployeeId { get; set; }
    }
}
