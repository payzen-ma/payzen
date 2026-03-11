namespace payzen_backend.DTOs.Payroll.Referentiel
{
    /// <summary>
    /// DTO for Authority (CNSS, IR, AMO, CIMR)
    /// </summary>
    public class AuthorityDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
