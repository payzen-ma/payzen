using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Auth;

public class Users : BaseEntity
{
    public int? EmployeeId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public string? EmailPersonal { get; set; }

    // Identifiant externe fourni par le provider (ex: Microsoft Entra External ID).
    public string? ExternalId { get; set; }

    // Source d'authentification (ex: "entra" pour Type C).
    public string? Source { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Employee.Employee? Employee { get; set; }
    public ICollection<UsersRoles>? UsersRoles { get; set; }
}
