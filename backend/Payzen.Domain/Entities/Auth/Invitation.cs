using Payzen.Domain.Common;
using Payzen.Domain.Enums.Auth;

namespace Payzen.Domain.Entities.Auth;

public class Invitation : BaseEntity
{
    public required string Token
    {
        get; set;
    }
    public required string Email
    {
        get; set;
    }
    public int CompanyId
    {
        get; set;
    }
    public int RoleId
    {
        get; set;
    }
    public int? EmployeeId
    {
        get; set;
    }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime ExpiresAt
    {
        get; set;
    }

    // Navigation
    public Company.Company? Company
    {
        get; set;
    }
    public Auth.Roles? Role
    {
        get; set;
    }
}
