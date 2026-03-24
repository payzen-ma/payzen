using System.ComponentModel.DataAnnotations;

namespace payzen_backend.Models.Permissions.Dtos
{
    public class RolePermissionsBulkAssignDto
    {
        [Required(ErrorMessage = "L'ID du r�le est requis")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du r�le doit �tre valide")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Au moins une permission doit �tre sp�cifi�e")]
        [MinLength(1, ErrorMessage = "Au moins une permission doit �tre sp�cifi�e")]
        public List<int> PermissionIds { get; set; } = new();
    }
}
