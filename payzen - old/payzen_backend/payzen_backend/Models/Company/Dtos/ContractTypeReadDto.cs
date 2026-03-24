namespace payzen_backend.Models.Company.Dtos
{
    public class ContractTypeReadDto
    {
        public int Id { get; set; }
        public string ContractTypeName { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public int? LegalContractTypeId { get; set; }
        public string? LegalContractTypeName { get; set; }
        public int? StateEmploymentProgramId { get; set; }
        public string? StateEmploymentProgramName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
