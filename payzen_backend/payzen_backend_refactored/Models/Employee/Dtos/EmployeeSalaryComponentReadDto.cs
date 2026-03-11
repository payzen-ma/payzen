namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeSalaryComponentReadDto
    {
        public int Id { get; set; }
        public int EmployeeSalaryId { get; set; }
        public string ComponentType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}