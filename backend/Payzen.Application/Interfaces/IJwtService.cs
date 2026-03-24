using Payzen.Domain.Entities.Auth;

namespace Payzen.Application.Interfaces;

/// <summary>
/// Génération et validation de tokens JWT.
/// </summary>
public interface IJwtService
{
    Task<string> GenerateTokenAsync(Users user, CancellationToken ct = default);
    int? ValidateToken(string token);
}
