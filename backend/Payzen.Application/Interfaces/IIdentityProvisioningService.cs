using Payzen.Application.Common;

namespace Payzen.Application.Interfaces;

public sealed class ProvisionedIdentityResult
{
    public required string ExternalId { get; init; }

    public required string Login { get; init; }

    public required string TemporaryPassword { get; init; }

    public required string LoginUrl { get; init; }
}

public interface IIdentityProvisioningService
{
    Task<ServiceResult<ProvisionedIdentityResult>> ProvisionEmployeeAccountAsync(
        string email,
        string firstName,
        string lastName,
        CancellationToken ct = default
    );
}
