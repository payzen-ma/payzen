using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Payzen.Application.Validators.Company;
using Payzen.Infrastructure;
using Payzen.Infrastructure.Persistence;
using Payzen.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);
// ===== AUTHENTICATION =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    // Session cookie — expire après 30 min d'inactivité
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;   // renouvelle le timer si l'user est actif
    options.Cookie.HttpOnly = true;   // inaccessible depuis JavaScript
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/auth/login";
    options.LogoutPath = "/auth/logout";
    options.Events.OnRedirectToLogin = context =>
    {
        // Pour une API REST, retourner 401 au lieu de rediriger
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
})
.AddOpenIdConnect("EntraExternalId", options =>
{
    options.Authority = builder.Configuration["EntraExternalId:Instance"]
                         + builder.Configuration["EntraExternalId:TenantId"];
    options.ClientId = builder.Configuration["EntraExternalId:ClientId"];
    options.ClientSecret = builder.Configuration["EntraExternalId:ClientSecret"];
    options.CallbackPath = builder.Configuration["EntraExternalId:CallbackPath"];

    options.ResponseType = OpenIdConnectResponseType.Code;  // Authorization Code Flow
    options.UsePkce = true;   // obligatoire pour SPA + sécurité renforcée
    options.SaveTokens = true;   // conserve les tokens pour le refresh

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("offline_access");  // nécessaire pour le refresh token

    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name",
        RoleClaimType = "roles"
    };

    // Mapper le claim "oid" vers un claim standard
    options.ClaimActions.MapJsonKey("oid",
        "http://schemas.microsoft.com/identity/claims/objectidentifier");
});

// ===== AUTHORIZATION =====
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));
    options.AddPolicy("ActiveUser", policy =>
        policy.RequireClaim("isActive", "true"));
});

// ===== SESSION =====
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== SERVICES =====
// NOTE:
// Les services de provisioning/invitations utilisés dans un autre workflow Entra
// ne sont pas implémentés dans ce codebase (ce sont uniquement des références).
// On les retire pour conserver une compilation et un démarrage serveur stables
// (JWT + endpoint /api/auth/entra-login).

// ── Controllers + JSON + Validation global ────────────────────────────────────
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationActionFilter>();
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true; // géré par ValidationActionFilter
});

builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});

// ── Infrastructure (EF Core + tous les services) ──────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── JWT Authentication ─────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["JwtSettings:Key"]
    ?? throw new InvalidOperationException("JWT Key not found in configuration");
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "unique_name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:50171",
                "http://localhost:4200",
                "https://localhost:4200",
                "http://localhost:4201",
                "https://localhost:4201",
                "http://localhost:55879",
                "https://app-demo.payzenhr.com",
                "https://app-test.payzenhr.com",
                "https://admin-test.payzenhr.com",
                "https://app.payzenhr.com",
                "https://admin.payzen.ma")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// option recommandée — moteur pure/stateless : singleton
builder.Services.AddSingleton<Payzen.Application.Payroll.PayrollCalculationEngine>();

// Enregistrer les validators FluentValidation (scanne l'assembly contenant CompanyCreateValidator)
builder.Services.AddValidatorsFromAssemblyContaining<CompanyCreateValidator>();

// Activer l'auto-validation côté serveur (optionnel selon votre version de FluentValidation)
// Important : CompanyCreateValidator contient des règles async (MustAsync).
// L'auto-validation FluentValidation actuelle exécute le pipeline en mode synchronisé,
// ce qui déclenche AsyncValidatorInvokedSynchronouslyException.
// On garde la validation manuelle dans les controllers (ValidateAsync).
// builder.Services.AddFluentValidationAutoValidation();

// ou scope si vous préférez vie par requête
// builder.Services.AddScoped<Payzen.Application.Payroll.PayrollCalculationEngine>();

var app = builder.Build();

// ── IronPDF licence ────────────────────────────────────────────────────────────
IronPdf.License.LicenseKey = app.Configuration["IronPdf:LicenseKey"] ?? "";

// ── Middleware pipeline (ordre critique) ──────────────────────────────────────

// 1. Exception handler global — DOIT être en premier
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var error = context.Features.Get<IExceptionHandlerFeature>();
        if (error != null)
        {
            var logger = context.RequestServices
                .GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error,
                "Exception non gérée sur {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }

        var exceptionType = error?.Error?.GetType().Name;
        var exceptionMessage = error?.Error?.Message;
        var inDevelopment = app.Environment.IsDevelopment();

        await context.Response.WriteAsJsonAsync(new
        {
            Message = "Une erreur interne est survenue.",
            TraceId = context.TraceIdentifier,
            // Débogage: aide à identifier rapidement la cause d'un 500.
            // En prod, on évite de renvoyer le message exceptionnel.
            ExceptionType = inDevelopment ? exceptionType : null,
            ExceptionMessage = inDevelopment ? exceptionMessage : null
        });
    });
});

// 2. Normaliser 401/403/404 sans body
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    if (response.ContentType == null ||
        !response.ContentType.Contains("application/json"))
    {
        response.ContentType = "application/json";
        var message = response.StatusCode switch
        {
            401 => "Authentification requise.",
            403 => "Accès refusé.",
            404 => "Ressource introuvable.",
            405 => "Méthode non autorisée.",
            _ => $"Erreur HTTP {response.StatusCode}."
        };
        await response.WriteAsJsonAsync(new
        {
            Message = message
        });
    }
});

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Démarrage du seed...");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DbSeeder.SeedAsync(db);
        logger.LogInformation("Seed terminé.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Seed ignoré (DB non disponible). L'API continue.");
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", async (AppDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return Results.Ok(new
        {
            status = "API is running",
            database = canConnect ? "connected" : "unreachable",
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            status = "API is running",
            database = "error",
            innerError = ex.InnerException?.Message,
            timestamp = DateTime.UtcNow
        });
    }
});
app.Run();

// ── Filtre de validation global ────────────────────────────────────────────────
public class ValidationActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            context.Result = new BadRequestObjectResult(new
            {
                Message = "Données invalides.",
                Errors = errors
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
