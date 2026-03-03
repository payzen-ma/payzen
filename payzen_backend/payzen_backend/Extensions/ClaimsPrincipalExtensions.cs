using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace payzen_backend.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// R�cup�re l'ID de l'utilisateur depuis le claim "uid"
        /// </summary>
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst("uid")?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new InvalidOperationException("User ID claim not found in token");
            }

            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new InvalidOperationException("Invalid User ID format in token");
            }

            return userId;
        }

        /// <summary>
        /// R�cup�re l'email de l'utilisateur depuis les claims JWT
        /// </summary>
        public static string GetUserEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Email)?.Value 
                ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? principal.FindFirst("email")?.Value
                ?? throw new UnauthorizedAccessException("Email non trouv� dans le token");
        }

        /// <summary>
        /// R�cup�re le nom d'utilisateur depuis les claims JWT
        /// </summary>
        public static string GetUsername(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Name)?.Value 
                ?? principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value 
                ?? principal.FindFirst("unique_name")?.Value
                ?? throw new UnauthorizedAccessException("Nom d'utilisateur non trouv�");
        }

        /// <summary>
        /// V�rifie si l'utilisateur est authentifi�
        /// </summary>
        public static bool IsAuthenticated(this ClaimsPrincipal principal)
        {
            return principal.Identity?.IsAuthenticated ?? false;
        }

        /// <summary>
        /// R�cup�re toutes les permissions de l'utilisateur depuis les claims
        /// </summary>
        public static IEnumerable<string> GetPermissions(this ClaimsPrincipal user)
        {
            return user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();
        }

        /// <summary>
        /// V�rifie si l'utilisateur poss�de une permission sp�cifique
        /// </summary>
        public static bool HasPermission(this ClaimsPrincipal user, string permission)
        {
            return user.Claims
                .Any(c => c.Type == "permission" && c.Value == permission);
        }

        /// <summary>
        /// V�rifie si l'utilisateur poss�de au moins une des permissions sp�cifi�es
        /// </summary>
        public static bool HasAnyPermission(this ClaimsPrincipal user, params string[] permissions)
        {
            var userPermissions = user.GetPermissions();
            return permissions.Any(p => userPermissions.Contains(p));
        }

        /// <summary>
        /// V�rifie si l'utilisateur poss�de toutes les permissions sp�cifi�es
        /// </summary>
        public static bool HasAllPermissions(this ClaimsPrincipal user, params string[] permissions)
        {
            var userPermissions = user.GetPermissions();
            return permissions.All(p => userPermissions.Contains(p));
        }
    }
}