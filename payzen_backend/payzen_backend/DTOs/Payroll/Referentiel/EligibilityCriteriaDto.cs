namespace payzen_backend.DTOs.Payroll.Referentiel
{
    /// <summary>
    /// DTO for Eligibility Criteria (ALL, CADRES_SUP, PDG_DG, etc.)
    /// </summary>
    public class EligibilityCriteriaDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
