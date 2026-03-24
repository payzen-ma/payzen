namespace payzen_backend.Models.Payroll.Dtos
{
    public class SalaryPackageItemReadDto
    {
        public int Id { get; set; }
        public int? PayComponentId { get; set; }
        public string? PayComponentCode { get; set; }
        public int? ReferentielElementId { get; set; }
        public string? ReferentielElementName { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal DefaultValue { get; set; }
        public int SortOrder { get; set; }

        // Moroccan regulatory fields (2026 compliance)
        public string Type { get; set; } = "allowance";
        public bool IsTaxable { get; set; }
        public bool IsSocial { get; set; }
        public bool IsCIMR { get; set; }
        public bool IsVariable { get; set; }
        public decimal? ExemptionLimit { get; set; }
    }
}
