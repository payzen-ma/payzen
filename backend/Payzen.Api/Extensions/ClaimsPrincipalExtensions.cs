using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Payzen.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var val = user.FindFirst("uid")?.Value ?? throw new InvalidOperationException("User ID claim not found");
        return int.TryParse(val, out var id) ? id : throw new InvalidOperationException("Invalid User ID format");
    }

    public static string GetUserEmail(this ClaimsPrincipal principal) =>
        principal.FindFirst(ClaimTypes.Email)?.Value
        ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
        ?? principal.FindFirst("email")?.Value
        ?? throw new UnauthorizedAccessException("Email non trouvé dans le token");

    public static IEnumerable<string> GetPermissions(this ClaimsPrincipal user) =>
        user.Claims.Where(c => c.Type == "permission").Select(c => c.Value);

    public static bool HasPermission(this ClaimsPrincipal user, string permission) =>
        user.Claims.Any(c => c.Type == "permission" && c.Value == permission);

    public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] permissions)
    {
        var perms = user.GetPermissions();
        return permissions.Any(p => perms.Contains(p));
    }

    public static bool HasAllPermissions(this ClaimsPrincipal user, params string[] permissions)
    {
        var perms = user.GetPermissions();
        return permissions.All(p => perms.Contains(p));
    }
}
