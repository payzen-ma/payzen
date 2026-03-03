using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Permissions.Dtos
{
    /// <summary>
    /// DTO pour assigner plusieurs r�les � un utilisateur en masse
    /// </summary>
    public class UserRolesBulkAssignDto
    {
        [Required(ErrorMessage = "L'ID de l'utilisateur est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de l'utilisateur doit �tre valide")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Au moins un r�le doit �tre sp�cifi�")]
        [MinLength(1, ErrorMessage = "Au moins un r�le doit �tre sp�cifi�")]
        public List<int> RoleIds { get; set; } = new();
    }
}