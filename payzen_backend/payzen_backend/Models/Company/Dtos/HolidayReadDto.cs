namespace payzen_backend.Models.Company.Dtos
{
    public class HolidayReadDto
    {
        public int Id { get; set; }
        public string NameFr { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public DateOnly HolidayDate { get; set; }
        public string? Description { get; set; }
        public int? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public int CountryId { get; set; }
        public string CountryName { get; set; } = string.Empty;
        public HolidayScope Scope { get; set; }
        public string ScopeDescription { get; set; } = string.Empty;
        public string HolidayType { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
        public bool IsPaid { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrenceRule { get; set; }
        public int? Year { get; set; }
        public bool AffectPayroll { get; set; }
        public bool AffectAttendance { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
