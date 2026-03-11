using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Company.Dtos
{
    /// <summary>
    /// DTO pour la lecture d'un document d'entreprise
    /// </summary>
    public class CompanyDocumentReadDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? DocumentType { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO pour la création d'un document d'entreprise
    /// </summary>
    public class CompanyDocumentCreateDto
    {
        [Required(ErrorMessage = "L'ID de l'entreprise est requis")]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Le nom du document est requis")]
        [StringLength(500, ErrorMessage = "Le nom ne peut pas dépasser 500 caractères")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le chemin du fichier est requis")]
        [StringLength(1000, ErrorMessage = "Le chemin ne peut pas dépasser 1000 caractères")]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Le type ne peut pas dépasser 100 caractères")]
        public string? DocumentType { get; set; }
    }

    /// <summary>
    /// DTO pour la mise à jour d'un document d'entreprise
    /// </summary>
    public class CompanyDocumentUpdateDto
    {
        [StringLength(500, ErrorMessage = "Le nom ne peut pas dépasser 500 caractères")]
        public string? Name { get; set; }

        [StringLength(100, ErrorMessage = "Le type ne peut pas dépasser 100 caractères")]
        public string? DocumentType { get; set; }
    }

    /// <summary>
    /// DTO pour l'upload d'un fichier
    /// </summary>
    public class CompanyDocumentUploadDto
    {
        [Required(ErrorMessage = "L'ID de l'entreprise est requis")]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Le fichier est requis")]
        public IFormFile File { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Le type ne peut pas dépasser 100 caractères")]
        public string? DocumentType { get; set; }
    }
}