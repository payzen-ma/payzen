namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeSalaryReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public int ContractId { get; set; }
        public decimal BaseSalary { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}