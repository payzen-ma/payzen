using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    /// <summary>
    /// DTO utilisé par un cabinet expert pour créer une entreprise cliente.
    /// Structure proche de CompanyCreateDto mais inclut l'identifiant du cabinet qui gère la société.
    /// </summary>
    public class CompanyCreateByExpertDto
    {
        // ========== INFORMATIONS DE L'ENTREPRISE ==========

        [Required(ErrorMessage = "Le nom de l'entreprise est requis")]
        [StringLength(500, MinimumLength = 2)]
        public required string CompanyName { get; set; }

        [Required(ErrorMessage = "L'email professionnel est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(500)]
        public required string CompanyEmail { get; set; }

        [Required(ErrorMessage = "Le numéro de téléphone est requis")]
        [StringLength(20)]
        public required string CompanyPhoneNumber { get; set; }

        [StringLength(10)]
        public string? CountryPhoneCode { get; set; }

        [Required(ErrorMessage = "L'adresse est requise")]
        [StringLength(1000)]
        public required string CompanyAddress { get; set; }

        [Required(ErrorMessage = "Le pays est requis")]
        public required int CountryId { get; set; }

        // Ville : Existante OU Nouvelle
        public int? CityId { get; set; }

        [StringLength(500)]
        public string? CityName { get; set; }

        [Required(ErrorMessage = "Le numéro CNSS employeur est requis")]
        [StringLength(100)]
        public required string CnssNumber { get; set; }

        /// <summary>
        /// Indique si l'entité créée est elle-même un cabinet expert.
        /// </summary>
        public bool IsCabinetExpert { get; set; } = false;

        // Informations optionnelles entreprise
        [StringLength(100)]
        public string? IceNumber { get; set; }

        [StringLength(100)]
        public string? IfNumber { get; set; }

        [StringLength(100)]
        public string? RcNumber { get; set; }

        [StringLength(100)]
        public string? RibNumber { get; set; }
        [StringLength(100)]
        public string? PatenteNumber { get; set; }
        [StringLength(500)]
        [Url(ErrorMessage = "Format d'URL invalide")]
        public string? WebsiteUrl { get; set; }

        [StringLength(50)]
        public string? LegalForm { get; set; }

        public DateTime? FoundingDate { get; set; }

        [StringLength(200)]
        public string? BusinessSector { get; set; }

        [StringLength(100)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Identifiant du cabinet expert qui gère cette entreprise cliente.
        /// Requis car la création est faite "par expert".
        /// </summary>
        [Required(ErrorMessage = "Identifiant du cabinet expert requis")]
        public required int ManagedByCompanyId { get; set; }

        // ========== INFORMATIONS DE L'ADMINISTRATEUR ==========

        [Required(ErrorMessage = "Le prénom de l'administrateur est requis")]
        [StringLength(100, MinimumLength = 2)]
        public required string AdminFirstName { get; set; }

        [Required(ErrorMessage = "Le nom de l'administrateur est requis")]
        [StringLength(100, MinimumLength = 2)]
        public required string AdminLastName { get; set; }

        [Required(ErrorMessage = "L'email de l'administrateur est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(500)]
        public required string AdminEmail { get; set; }

        [Required(ErrorMessage = "Le téléphone de l'administrateur est requis")]
        [StringLength(20)]
        public required string AdminPhone { get; set; }

        /// <summary>
        /// Si true, génère un mot de passe temporaire automatiquement.
        /// Si false, le mot de passe doit être fourni dans AdminPassword.
        /// </summary>
        public bool GeneratePassword { get; set; } = true;

        /// <summary>
        /// Mot de passe personnalisé (requis si GeneratePassword = false)
        /// </summary>
        [StringLength(100, MinimumLength = 8)]
        public string? AdminPassword { get; set; }

        public bool isActive { get; set; }
    }
}