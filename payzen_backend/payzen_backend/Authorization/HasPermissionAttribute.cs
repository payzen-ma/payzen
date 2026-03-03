using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using payzen_backend.Extensions;

namespace payzen_backend.Authorization
{
    /// <summary>
    /// Attribut d'autorisation bas� sur les permissions
    /// Utilis� pour restreindre l'acc�s aux endpoints selon les permissions de l'utilisateur
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class HasPermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _permissions;
        private readonly bool _requireAll;

        /// <summary>
        /// Constructeur pour une seule permission
        /// </summary>
        /// <param name="permission">Nom de la permission requise</param>
        public HasPermissionAttribute(string permission)
        {
            _permissions = new[] { permission };
            _requireAll = true;
        }

        /// <summary>
        /// Constructeur pour plusieurs permissions
        /// </summary>
        /// <param name="requireAll">Si true, toutes les permissions sont requises. Si false, au moins une est requise.</param>
        /// <param name="permissions">Liste des permissions</param>
        public HasPermissionAttribute(bool requireAll, params string[] permissions)
        {
            _permissions = permissions;
            _requireAll = requireAll;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // V�rifier si l'utilisateur est authentifi�
            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Message = "Authentification requise"
                });
                return;
            }

            // V�rifier les permissions
            bool hasPermission;

            if (_requireAll)
            {
                hasPermission = context.HttpContext.User.HasAllPermissions(_permissions);
            }
            else
            {
                hasPermission = context.HttpContext.User.HasAnyPermission(_permissions);
            }

            if (!hasPermission)
            {
                context.Result = new ObjectResult(new
                {
                    Message = "Vous n'avez pas les permissions n�cessaires pour effectuer cette action",
                    RequiredPermissions = _permissions
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}