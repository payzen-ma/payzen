using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Permissions.Dtos
{
    public class RolePermissionAssignDto
    {
        [Required(ErrorMessage = "L'ID du r�le est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du r�le doit �tre valide")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "L'ID de la permission est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la permission doit �tre valide")]
        public int PermissionId { get; set; }
    }
}