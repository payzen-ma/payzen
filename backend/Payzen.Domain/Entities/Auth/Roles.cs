using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Auth;

public class Roles : BaseEntity
{
    public required string Name
    {
        get; set;
    }
    public required string Description
    {
        get; set;
    }
}
