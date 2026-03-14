namespace payzen_backend.Models.Company.Dtos
{
    public class CompanyReadDto
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? CountryPhoneCode { get; set; }
        public string CompanyAddress { get; set; } = string.Empty;

        public int CityId { get; set; }
        public string? CityName { get; set; }
        public int CountryId { get; set; }
        public string? CountryName { get; set; }

        public string CnssNumber { get; set; } = string.Empty;
        public bool IsCabinetExpert { get; set; }

        // Informations optionnelles
        public string? IceNumber { get; set; }
        public string? IfNumber { get; set; }
        public string? RcNumber { get; set; }
        public string? LegalForm { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? PatentNumber { get; set; }
        public string? RibNumber { get; set; }
        public DateTime? FoundingDate { get; set; }
        public string? BusinessSector { get; set; }
        public bool isActive { get; set; }

        public string? SignatoryName { get; set; }
        public string? SignatoryTitle { get; set; }
        public string? PayrollPeriodicity { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
