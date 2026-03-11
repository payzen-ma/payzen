using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    /// <summary>
    /// DTO pour mises � jour partielles d'une entreprise (PATCH)
    /// Toutes les propri�t�s sont optionnelles (nullable) pour permettre des mises � jour partielles.
    /// </summary>
    public class CompanyUpdateDto
    {
        // Informations de base
        [StringLength(500, MinimumLength = 2)]
        public string? CompanyName { get; set; }

        [EmailAddress]
        [StringLength(500)]
        public string? email { get; set; }

        [StringLength(20)]
        public string? phoneNumber { get; set; }

        [StringLength(10)]
        public string? CountryPhoneCode { get; set; }

        [StringLength(1000)]
        public string? CompanyAddress { get; set; }

        public int? CountryId { get; set; }
        public string? CountryName { get; set; } = string.Empty;
        public int? CityId { get; set; }
        [StringLength(500)]
        public string? CityName { get; set; }

        // Identifiants
        [StringLength(100)]
        public string? CnssNumber { get; set; }

        public bool? IsCabinetExpert { get; set; }

        // Optionnels
        [StringLength(100)]
        public string? IceNumber { get; set; }

        [StringLength(100)]
        public string? IfNumber { get; set; }

        [StringLength(100)]
        public string? RcNumber { get; set; }
        [StringLength(100)]
        public string? PatenteNumber { get; set; }
        [StringLength(100)]
        public string? RibNumber { get; set; }
        [StringLength(500)]
        //[Url(ErrorMessage = "Invalid URL format")]
        public string? WebsiteUrl { get; set; }

        [StringLength(50)]
        public string? LegalForm { get; set; }

        public DateTime? FoundingDate { get; set; }

        public int? ManagedByCompanyId { get; set; }
        public bool? isActive { get; set; }

        [StringLength(200)]
        public string? SignatoryName { get; set; }

        [StringLength(100)]
        public string? SignatoryTitle { get; set; }
    }
}
