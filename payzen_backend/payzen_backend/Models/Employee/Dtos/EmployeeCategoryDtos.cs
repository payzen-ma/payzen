using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Employee.Dtos
{
    /// <summary>
    /// DTO pour cr�er une nouvelle cat�gorie d'employ�
    /// </summary>
    public class EmployeeCategoryCreateDto
    {
        [Required(ErrorMessage = "L'ID de la soci�t� est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la soci�t� doit �tre valide")]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Le nom de la cat�gorie est requis")]
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caract�res")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Le mode de la cat�gorie est requis")]
        public EmployeeCategoryMode Mode { get; set; }
    }

    /// <summary>
    /// DTO pour lire une cat�gorie d'employ�
    /// </summary>
    public class EmployeeCategoryReadDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public EmployeeCategoryMode Mode { get; set; }
        public string ModeDescription { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO pour mettre � jour une cat�gorie d'employ� (tous les champs optionnels)
    /// </summary>
    public class EmployeeCategoryUpdateDto
    {
        [StringLength(500, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 500 caract�res")]
        public string? Name { get; set; }

        public EmployeeCategoryMode? Mode { get; set; }
    }

    /// <summary>
    /// DTO simplifi� pour lister les cat�gories dans des dropdowns
    /// </summary>
    public class EmployeeCategorySimpleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public EmployeeCategoryMode Mode { get; set; }
    }
}