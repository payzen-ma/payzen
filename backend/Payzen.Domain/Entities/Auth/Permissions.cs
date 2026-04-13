using Payzen.Domain.Common;

namespace Payzen.Domain.Entities.Auth;

public class Permissions : BaseEntity
{
    public required string Name
    {
        get; set;
    }
    public required string Description
    {
        get; set;
    }
    public string? Resource { get; set; } = string.Empty;
    public string? Action { get; set; } = string.Empty;
}
