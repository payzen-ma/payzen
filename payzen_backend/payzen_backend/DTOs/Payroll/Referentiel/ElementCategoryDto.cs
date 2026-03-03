namespace payzen_backend.DTOs.Payroll.Referentiel
{
    /// <summary>
    /// DTO for Element Category (IND_PRO, IND_SOCIAL, PRIME_SPEC, AVANTAGE)
    /// </summary>
    public class ElementCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for creating a new Element Category
    /// </summary>
    public class CreateElementCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
