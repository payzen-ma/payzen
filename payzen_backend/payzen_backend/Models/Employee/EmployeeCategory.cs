namespace payzen_backend.Models.Employee
{
    public class EmployeeCategory
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public payzen_backend.Models.Company.Company Company { get; set; } = null!;
        public string Name { get; set; } = null!;

        public EmployeeCategoryMode Mode { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public int CreatedBy { get; set; }

        public DateTimeOffset? ModifiedAt { get; set; }
        public int? ModifiedBy { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }
        public int? DeletedBy { get; set; }

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
    public enum EmployeeCategoryMode
    {
        Attendance = 1,
        Absence = 2
    }
}
