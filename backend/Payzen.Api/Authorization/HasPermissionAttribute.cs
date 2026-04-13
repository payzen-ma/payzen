using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Payzen.Api.Extensions;

namespace Payzen.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : Attribute, IAuthorizationFilter
    {
    private readonly string[] _permissions;
    private readonly bool _requireAll;

    public HasPermissionAttribute(string permission)
        {
        _permissions = [permission];
        _requireAll = true;
        }

    public HasPermissionAttribute(bool requireAll, params string[] permissions)
        {
        _permissions = permissions;
        _requireAll = requireAll;
        }

    public void OnAuthorization(AuthorizationFilterContext context)
        {
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
            context.Result = new UnauthorizedObjectResult(new
                {
                Message = "Authentification requise"
                });
            return;
            }

        var hasPermission = _requireAll
            ? context.HttpContext.User.HasAllPermissions(_permissions)
            : context.HttpContext.User.HasAnyPermission(_permissions);

        if (!hasPermission)
            {
            context.Result = new ObjectResult(new
                {
                Message = "Vous n'avez pas les permissions nécessaires",
                RequiredPermissions = _permissions
                })
                {
                StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
