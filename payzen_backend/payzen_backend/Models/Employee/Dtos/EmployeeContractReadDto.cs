namespace payzen_backend.Models.Employee.Dtos
{
    public class EmployeeContractReadDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int JobPositionId { get; set; }
        public string JobPositionName { get; set; } = string.Empty;
        public int ContractTypeId { get; set; }
        public string ContractTypeName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}