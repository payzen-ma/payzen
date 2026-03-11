using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Permissions.Dtos
{
    /// <summary>
    /// DTO pour assigner un r�le � un utilisateur
    /// </summary>
    public class UserRoleAssignDto
    {
        [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'utilisateur doit �tre valide")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "L'ID du r�le est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du r�le doit �tre valide")]
        public int RoleId { get; set; }
    }
}