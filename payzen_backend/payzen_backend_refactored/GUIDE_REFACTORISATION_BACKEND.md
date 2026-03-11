# Guide de Refactorisation Backend PayZen - Pour AI Copilot

**Date :** Mars 2026  
**Cible :** Refactorisation complète du backend ASP.NET Core 9.0  
**Audience :** AI Copilot / GitHub Copilot

---

## 📋 Table des Matières

1. [Vue d'ensemble et Objectifs](#1-vue-densemble-et-objectifs)
2. [Phase 1 : API Versioning](#2-phase-1--api-versioning)
3. [Phase 2 : Middleware Layer](#3-phase-2--middleware-layer)
4. [Phase 3 : Services Layer](#4-phase-3--services-layer)
5. [Phase 4 : Controllers Layer](#5-phase-4--controllers-layer)
6. [Phase 5 : Models & DTOs Layer](#6-phase-5--models--dtos-layer)
7. [Phase 6 : Repository Pattern](#7-phase-6--repository-pattern)
8. [Phase 7 : Validation Layer](#8-phase-7--validation-layer)
9. [Phase 8 : Configuration & Options Pattern](#9-phase-8--configuration--options-pattern)
10. [Checklist de Refactorisation par Domaine](#10-checklist-de-refactorisation-par-domaine)
11. [Ordre d'Exécution Recommandé](#11-ordre-dexécution-recommandé)

---

## 1. Vue d'ensemble et Objectifs

### 1.1 État Actuel (À CORRIGER)

| Problème | Impact | Priorité |
|----------|--------|----------|
| Pas de versioning API | Breaking changes impossibles à gérer | 🔴 HAUTE |
| Pas de middleware global d'exceptions | Fuites d'info, pas de format standardisé | 🔴 HAUTE |
| ≈10 services sur 20 ont des interfaces | Faible testabilité | 🟡 MOYENNE |
| Contrôleurs accèdent directement à DbContext | Couplage fort, duplication | 🟡 MOYENNE |
| Validation manuelle dans contrôleurs | Code dupliqué | 🟡 MOYENNE |
| Configuration via `IConfiguration` directe | Pas de typage fort | 🟢 BASSE |

### 1.2 Objectifs de Refactorisation

```
AVANT:
Controller → DbContext (direct)

APRÈS:
Controller → Service → Repository → DbContext
     ↓           ↓
 Validation   Interface
```

### 1.3 Structure Cible

```
payzen_backend/
├── Authorization/              # ✅ Existe - Conserver
├── Configuration/              # 🆕 CRÉER - Options classes
│   ├── JwtOptions.cs
│   ├── AnthropicOptions.cs
│   ├── GoogleOptions.cs
│   └── DatabaseOptions.cs
├── Controllers/                # ✅ Existe - Refactorer
│   └── v1/                     # 🆕 CRÉER - Versioning
│       ├── Auth/
│       ├── Company/
│       ├── Employee/
│       └── ...
├── Data/                       # ✅ Existe - Étendre
│   ├── AppDbContext.cs
│   ├── Configurations/         # 🆕 CRÉER - EF Config séparées
│   └── Repositories/           # 🆕 CRÉER - Repository pattern
│       ├── Interfaces/
│       └── Implementations/
├── DTOs/                       # ✅ Existe - Réorganiser
│   ├── Common/                 # 🆕 Réponses génériques
│   │   ├── ApiResponse.cs
│   │   ├── PaginatedResponse.cs
│   │   └── ErrorResponse.cs
│   └── [Domaines]/
├── Extensions/                 # ✅ Existe - Étendre
│   ├── ServiceCollectionExtensions.cs    # 🆕 CRÉER
│   ├── ApplicationBuilderExtensions.cs   # 🆕 CRÉER
│   └── ClaimsPrincipalExtensions.cs      # ✅ Existe
├── Middleware/                 # 🆕 CRÉER
│   ├── ExceptionHandlerMiddleware.cs
│   ├── RequestLoggingMiddleware.cs
│   └── ApiVersionMiddleware.cs
├── Models/                     # ✅ Existe - Conserver structure
├── Services/                   # ✅ Existe - Ajouter interfaces
│   ├── Interfaces/             # 🆕 CRÉER - Toutes les interfaces
│   └── Implementations/        # Déplacer implémentations
├── Validators/                 # 🆕 CRÉER - FluentValidation
└── Program.cs                  # ✅ Refactorer
```

---

## 2. Phase 1 : API Versioning

### 2.1 Installation des Packages

```bash
# Dans le terminal, exécuter :
cd payzen_backend/payzen_backend
dotnet add package Asp.Versioning.Http
dotnet add package Asp.Versioning.Mvc.ApiExplorer
```

### 2.2 Configuration dans Program.cs

**FICHIER :** `Program.cs`  
**ACTION :** Ajouter après `builder.Services.AddControllers()`

```csharp
// === API VERSIONING ===
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version")
    );
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### 2.3 Modification des Contrôleurs

**PATTERN DE REFACTORISATION POUR CHAQUE CONTRÔLEUR :**

**AVANT :**
```csharp
[Route("api/employee")]
[ApiController]
public class EmployeeController : ControllerBase
```

**APRÈS :**
```csharp
[Route("api/v{version:apiVersion}/employee")]
[ApiController]
[ApiVersion("1.0")]
public class EmployeeController : ControllerBase
```

### 2.4 Liste des Contrôleurs à Modifier

| Dossier | Contrôleur | Route Actuelle | Route Cible |
|---------|-----------|----------------|-------------|
| Auth | AuthController | `api/auth` | `api/v{version:apiVersion}/auth` |
| Auth | PermissionsController | `api/permissions` | `api/v{version:apiVersion}/permissions` |
| Auth | RolesController | `api/roles` | `api/v{version:apiVersion}/roles` |
| Auth | RolesPermissionsController | `api/roles-permissions` | `api/v{version:apiVersion}/roles-permissions` |
| Auth | UsersController | `api/users` | `api/v{version:apiVersion}/users` |
| Auth | UsersRolesController | `api/users-roles` | `api/v{version:apiVersion}/users-roles` |
| Company | CompanyController | `api/companies` | `api/v{version:apiVersion}/companies` |
| Company | CompanyDocumentsController | `api/companydocuments` | `api/v{version:apiVersion}/company-documents` |
| Company | DepartementsController | `api/departements` | `api/v{version:apiVersion}/departments` |
| Company | JobPositionsController | `api/job-positions` | `api/v{version:apiVersion}/job-positions` |
| Dashboard | DashboardController | `api/dashboard` | `api/v{version:apiVersion}/dashboard` |
| Dashboard | DashboardBackOfficeController | - | `api/v{version:apiVersion}/dashboard/backoffice` |
| Dashboard | DashboardExpertController | `api/dashboard/expert` | `api/v{version:apiVersion}/dashboard/expert` |
| Employee | EmployeeController | `api/employee` | `api/v{version:apiVersion}/employees` |
| Employee | EmployeeAbsenceController | `api/absences` | `api/v{version:apiVersion}/absences` |
| Employee | EmployeeAddresssController | `api/employee-addresses` | `api/v{version:apiVersion}/employee-addresses` |
| Employee | EmployeeAttendanceController | `api/employee-attendance` | `api/v{version:apiVersion}/employee-attendance` |
| Employee | EmployeeAttendanceBreakController | `api/employee-attendance-break` | `api/v{version:apiVersion}/employee-attendance-breaks` |
| Employee | EmployeeCategoryController | `api/employee-categories` | `api/v{version:apiVersion}/employee-categories` |
| Employee | EmployeeChildController | `api/employees/{id}/children` | `api/v{version:apiVersion}/employees/{id}/children` |
| Employee | EmployeeContractsController | `api/employee-contracts` | `api/v{version:apiVersion}/employee-contracts` |
| Employee | EmployeeDocumentsController | `api/employee-documents` | `api/v{version:apiVersion}/employee-documents` |
| Employee | EmployeeOvertimeController | `api/employee-overtimes` | `api/v{version:apiVersion}/employee-overtimes` |
| Employee | EmployeeSalariesController | `api/employee-salaries` | `api/v{version:apiVersion}/employee-salaries` |
| Employee | EmployeeSalaryComponentsController | `api/employee-salary-components` | `api/v{version:apiVersion}/employee-salary-components` |
| Employee | EmployeeSpouseController | `api/employees/{id}/spouse` | `api/v{version:apiVersion}/employees/{id}/spouse` |
| Employee | SalaryPackageAssignmentsController | `api/salary-package-assignments` | `api/v{version:apiVersion}/salary-package-assignments` |
| Event | EventController | `api/events` | `api/v{version:apiVersion}/events` |
| Leave | LeaveAuditLogController | `api/leave-audit-logs` | `api/v{version:apiVersion}/leave-audit-logs` |
| Leave | LeaveBalanceController | `api/leave-balances` | `api/v{version:apiVersion}/leave-balances` |
| Leave | LeaveCarryOverAgreementController | `api/leave-carryover-agreements` | `api/v{version:apiVersion}/leave-carryover-agreements` |
| Leave | LeaveRequestController | `api/leave-requests` | `api/v{version:apiVersion}/leave-requests` |
| Leave | LeaveRequestApprovalHistoryController | `api/leave-request-approval-history` | `api/v{version:apiVersion}/leave-request-approval-history` |
| Leave | LeaveRequestAttachmentController | `api/leave-request-attachments` | `api/v{version:apiVersion}/leave-request-attachments` |
| Leave | LeaveRequestExemptionController | `api/leave-request-exemptions` | `api/v{version:apiVersion}/leave-request-exemptions` |
| Leave | LeaveTypeController | `api/leave-types` | `api/v{version:apiVersion}/leave-types` |
| Leave | LeaveTypeLegalRuleController | `api/leave-type-legal-rules` | `api/v{version:apiVersion}/leave-type-legal-rules` |
| Leave | LeaveTypePolicyController | `api/leave-type-policies` | `api/v{version:apiVersion}/leave-type-policies` |
| Payroll | ClaudeSimulationController | `api/claudesimulation` | `api/v{version:apiVersion}/simulation` |
| Payroll | PayrollController | `api/payroll` | `api/v{version:apiVersion}/payroll` |
| Payroll | PayrollExportController | `api/payroll/exports` | `api/v{version:apiVersion}/payroll/exports` |
| Payroll | PayslipController | `api/payslip` | `api/v{version:apiVersion}/payslips` |
| Payroll | SalaryPreviewController | `api/salary-preview` | `api/v{version:apiVersion}/salary-preview` |
| Payroll/Ref | AncienneteRateSetsController | `api/payroll/anciennete-rate-sets` | `api/v{version:apiVersion}/payroll/anciennete-rate-sets` |
| Payroll/Ref | AuthoritiesController | `api/payroll/authorities` | `api/v{version:apiVersion}/payroll/authorities` |
| Payroll/Ref | BusinessSectorsController | `api/business-sectors` | `api/v{version:apiVersion}/business-sectors` |
| Payroll/Ref | ElementCategoriesController | `api/payroll/element-categories` | `api/v{version:apiVersion}/payroll/element-categories` |
| Payroll/Ref | ElementRulesController | `api/payroll/element-rules` | `api/v{version:apiVersion}/payroll/element-rules` |
| Referentiel | EducationLevelsController | `api/education-levels` | `api/v{version:apiVersion}/education-levels` |
| Referentiel | GendersController | `api/genders` | `api/v{version:apiVersion}/genders` |
| Referentiel | LegalContractTypeController | `api/legal-contract-types` | `api/v{version:apiVersion}/legal-contract-types` |
| Referentiel | MaritalStatusesController | `api/marital-statuses` | `api/v{version:apiVersion}/marital-statuses` |
| Referentiel | OvertimeRateRuleController | `api/overtime-rate-rules` | `api/v{version:apiVersion}/overtime-rate-rules` |
| Referentiel | StateEmploymentProgramController | `api/state-employment-programs` | `api/v{version:apiVersion}/state-employment-programs` |
| Referentiel | StatusesController | `api/statuses` | `api/v{version:apiVersion}/statuses` |
| SystemData | CitiesController | `api/cities` | `api/v{version:apiVersion}/cities` |
| SystemData | ContractTypesController | `api/contract-types` | `api/v{version:apiVersion}/contract-types` |
| SystemData | CountriesController | `api/countries` | `api/v{version:apiVersion}/countries` |
| SystemData | HolidaysController | `api/holidays` | `api/v{version:apiVersion}/holidays` |
| SystemData | PayComponentsController | `api/pay-components` | `api/v{version:apiVersion}/pay-components` |
| SystemData | SalaryPackagesController | `api/salary-packages` | `api/v{version:apiVersion}/salary-packages` |
| SystemData | WorkingCalendarsController | `api/working-calendar` | `api/v{version:apiVersion}/working-calendars` |

---

### 2.5 Phase 2 — Modifications appliquées

Les modifications suivantes ont été appliquées dans la copie refactorisée (`payzen_backend_refactored/`) pour activer le versioning API (Phase 2) :

- **Program.cs** : `Program.cs` (enregistrement d'API Versioning & VersionedApiExplorer)
- **Global usings** : `GlobalUsings.cs` (ajout de `global using Asp.Versioning;`)

Fichiers et changements principaux (chemins relatifs à `payzen_backend_refactored/`):

- **Program.cs** : enregistrement d'API Versioning & VersionedApiExplorer (`AddApiVersioning`, `AddVersionedApiExplorer`).
- **GlobalUsings.cs** : ajout de `global using Asp.Versioning;` pour simplifier les `using` dans les contrôleurs.
- **Packages** : `Asp.Versioning.Http` et `Asp.Versioning.Mvc.ApiExplorer` ajoutés au `payzen_backend.csproj` de la copie refactorisée.

Contrôleurs modifiés / migrés (exemples - chemins relatifs à `payzen_backend_refactored/`) :

- Controllers/v1/Auth/AuthController.cs
- Controllers/v1/Auth/UsersController.cs
- Controllers/v1/Auth/RolesController.cs
- Controllers/v1/Auth/PermissionsController.cs
- Controllers/v1/Auth/UsersRolesController.cs
- Controllers/v1/Auth/RolesPermissionsController.cs
- Controllers/v1/SystemData/WorkingCalendarsController.cs
- Controllers/v1/SystemData/PayComponentsController.cs
- Controllers/v1/SystemData/SalaryPackagesController.cs
- Controllers/v1/Payroll/ClaudeSimulationController.cs
- Controllers/Dashboard/DashboardBackOfficeController.cs

Notes techniques et comportement appliqué :

- Pattern de route de classe : `api/v{version:apiVersion}/...` appliqué sur les contrôleurs.
- Ajout de l'attribut `[ApiVersion("1.0")]` sur chaque contrôleur de la v1.
- `Program.cs` configure :
    - `DefaultApiVersion = new ApiVersion(1,0)`
    - `AssumeDefaultVersionWhenUnspecified = true`
    - `ReportApiVersions = true`
    - `ApiVersionReader` combiné (Url segment, header `X-Api-Version`, query string `api-version`).
    - `VersionedApiExplorer` configuré avec `SubstituteApiVersionInUrl = true`.
- Si Swashbuckle/Swagger est utilisé : ajoutez la configuration `AddSwaggerGen()` + génération d'API docs par version via `IApiVersionDescriptionProvider`.

Vérifications rapides (depuis la copie refactorisée) :

```bash
cd payzen_backend/payzen_backend_refactored
dotnet build
dotnet run
```

Tester quelques routes manuellement :

- GET /api/v1/auth/me
- GET /api/v1/system-data/working-calendars

---

### Utilisation du dossier `Controllers/v1` — migration physique des contrôleurs

Objectif : séparer physiquement les contrôleurs par version pour réduire le risque de breaking changes et faciliter l'évolution et la maintenance.

Étapes recommandées (sécurisées, batch par batch) :

1. Créer la structure `Controllers/v1/...` en miroir de `Controllers/...`.
2. Déplacer les fichiers sources `.cs` dans `Controllers/v1/` en préservant l'arborescence.
3. Mettre à jour les `namespace` dans chaque fichier :
     - Avant : `namespace payzen_backend.Controllers.Auth`
     - Après : `namespace payzen_backend.Controllers.v1.Auth`
     (insérer `.v1` immédiatement après `Controllers`).
4. Vérifier / ajouter les attributs nécessaires sur chaque contrôleur :
     - `[ApiController]`
     - `[ApiVersion("1.0")]`
     - `[Route("api/v{version:apiVersion}/...")]` (pattern de route de classe)
5. Mettre à jour les références dans le code (usings) si nécessaire.
6. Supprimer les fichiers originaux sous `Controllers/...` uniquement après compilation réussie.
7. Committer les changements par petits batches et exécuter `dotnet build` entre chaque batch.

Commandes PowerShell d'exemple (exécuter depuis le répertoire racine du repo) :

```powershell
cd payzen_backend/payzen_backend_refactored

# Créer les dossiers v1 pour les dossiers de contrôleurs de premier niveau
Get-ChildItem Controllers -Directory | ForEach-Object {
    $dest = "Controllers\v1\$($_.Name)"
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
}

# Déplacer tous les fichiers .cs en préservant l'arborescence
Get-ChildItem Controllers -Recurse -Filter *.cs | ForEach-Object {
    $relative = $_.FullName.Substring((Join-Path (Get-Location) 'Controllers').Length + 1)
    $dest = Join-Path (Join-Path (Get-Location) 'Controllers\v1') $relative
    New-Item -ItemType Directory -Path (Split-Path $dest) -Force | Out-Null
    Move-Item -Path $_.FullName -Destination $dest -Force
}
```

Commande PowerShell rapide pour mettre à jour les namespaces (après le déplacement) :

```powershell
Get-ChildItem Controllers -Recurse -Filter *.cs | ForEach-Object {
    (Get-Content $_.FullName) -replace 'namespace payzen_backend.Controllers', 'namespace payzen_backend.Controllers.v1' |
        Set-Content $_.FullName
}
```

Checklist post-migration :

- `dotnet build` OK.
- Tests unitaires (s'il y en a) OK.
- Swagger/OpenAPI mis à jour et expose les groupes par version.
- Aucun `using` brisé.
- Commits atomiques pour faciliter la revue.

Mouvements déjà effectués (exemples) :

- Controllers/v1/Auth/* (AuthController, UsersController, RolesController, PermissionsController, UsersRolesController, RolesPermissionsController)
- Controllers/v1/SystemData/* (WorkingCalendarsController, PayComponentsController, SalaryPackagesController)
- Controllers/v1/Payroll/* (ClaudeSimulationController)

Notes finales :

- Ne pas modifier l'original `payzen_backend/payzen_backend` — travailler uniquement dans `payzen_backend_refactored/`.
- Avancer par petits batches et lancer `dotnet build` entre chaque batch.


### 2.6 Statut actuel — Phase 1 & Phase 2

Résumé rapide des changements appliqués et des tâches restantes (statut au moment présent) :

- **Phase 1 — API Versioning (guide)**
    - Fait :
        - Packages `Asp.Versioning.Http` et `Asp.Versioning.Mvc.ApiExplorer` ajoutés au `payzen_backend.csproj` de la copie refactorisée.
        - Configuration d'API versioning ajoutée dans `Program.cs` (`AddApiVersioning` avec `DefaultApiVersion = 1.0`, `AssumeDefaultVersionWhenUnspecified`, `ReportApiVersions` et lecteur combiné d'API version).
        - `GlobalUsings.cs` ajouté (`global using Asp.Versioning;`).
        - Nombreux contrôleurs mis à jour pour utiliser le pattern de route `api/v{version:apiVersion}/...` et l'attribut `[ApiVersion("1.0")]`.
        - Certains contrôleurs ont été physiquement déplacés vers `Controllers/v1/` (exemples listés ci‑dessus).
    - Restant :
        - Réactiver proprement `AddVersionedApiExplorer(...)` dans `Program.cs` (actuellement commenté pour permettre la compilation). Vérifier la directive `using` correcte et l'extension fournie par le package.
        - Intégrer `VersionedApiExplorer` avec Swagger (`IApiVersionDescriptionProvider`) si on souhaite documentation OpenAPI par version.
        - Revue/traitement des avertissements `nullable` (non bloquant mais recommandé pour qualité).

- **Phase 2 — Middleware Layer (guide)**
    - Fait :
        - Dossier `Middleware/` créé avec `ExceptionHandlerMiddleware.cs` et `RequestLoggingMiddleware.cs`.
        - `UseApplicationMiddlewares()` ajouté et appelé dans `Program.cs` pour enregistrer la pile middleware (logging, exception handler, rate limiter).
        - DTOs communs (`ApiResponse`, `ErrorResponse`, `PaginatedResponse`) et extensions (`Extensions/ServiceCollectionExtensions.cs`, `Extensions/ApplicationBuilderExtensions.cs`) ajoutés.
        - Enregistrement de la configuration et du rate limiting (`AddCustomRateLimiting()`, `AddOptions(...)`, `AddValidators()`) effectué dans `Program.cs`.
    - Restant :
        - Valider et affiner les politiques de rate limiting et les appliquer aux endpoints sensibles (LLM, endpoints intensifs).
        - Ajouter des tests d'intégration pour valider le format d'erreur et le comportement du middleware.
        - Vérifier l'ordre des middlewares et les interactions avec CORS/Authentication/Authorization.

Statut de compilation actuel :

- `dotnet build` a été exécuté depuis la copie refactorisée et **réussi** après qu'on ait temporairement commenté `AddVersionedApiExplorer(...)`. Résultat : build OK, mais **77 avertissements** (principalement liés aux annotations nullable et usages EF Include/ThenInclude).

Prochaines étapes recommandées (priorisées) :

1. Réactiver `AddVersionedApiExplorer` proprement (tâche prioritaire pour documentation/versioning complète).
2. Continuer la migration des contrôleurs vers `Controllers/v1/` par petits batches (2–4 fichiers), mettre à jour les `namespace` et exécuter `dotnet build` entre chaque batch.
3. Corriger les avertissements critiques (nullable/EF includes) qui pourraient masquer des NRE en production.
4. Intégrer Swagger/OpenAPI par version si nécessaire.


## 3. Phase 2 : Middleware Layer

### 3.1 Créer le Dossier Middleware

```bash
mkdir payzen_backend/payzen_backend/Middleware
```

### 3.2 ExceptionHandlerMiddleware.cs

**FICHIER À CRÉER :** `Middleware/ExceptionHandlerMiddleware.cs`

```csharp
using System.Net;
using System.Text.Json;
using payzen_backend.DTOs.Common;

namespace payzen_backend.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlerMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            KeyNotFoundException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = exception.Message,
                Type = "NotFound"
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = "Non autorisé",
                Type = "Unauthorized"
            },
            ArgumentException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = exception.Message,
                Type = "BadRequest"
            },
            InvalidOperationException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Conflict,
                Message = exception.Message,
                Type = "Conflict"
            },
            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = _env.IsDevelopment() ? exception.Message : "Une erreur interne est survenue",
                Type = "InternalServerError",
                Details = _env.IsDevelopment() ? exception.StackTrace : null
            }
        };

        response.StatusCode = errorResponse.StatusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await response.WriteAsJsonAsync(errorResponse, options);
    }
}
```

### 3.3 RequestLoggingMiddleware.cs

**FICHIER À CRÉER :** `Middleware/RequestLoggingMiddleware.cs`

```csharp
using System.Diagnostics;

namespace payzen_backend.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;

        _logger.LogInformation(
            "[{RequestId}] {Method} {Path} started",
            requestId,
            context.Request.Method,
            context.Request.Path);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "[{RequestId}] {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
```

### 3.4 RateLimitingMiddleware (pour endpoints LLM)

**FICHIER À CRÉER :** `Middleware/RateLimitingConfiguration.cs`

```csharp
using System.Threading.RateLimiting;

namespace payzen_backend.Middleware;

public static class RateLimitingConfiguration
{
    public static IServiceCollection AddCustomRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            // Global rate limiter
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Policy pour les endpoints LLM (plus restrictive)
            options.AddPolicy("llm", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Policy pour les endpoints d'authentification
            options.AddPolicy("auth", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(5),
                        SegmentsPerWindow = 5
                    }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
    }
}
```

### 3.5 DTOs Communs pour les Réponses

**FICHIER À CRÉER :** `DTOs/Common/ErrorResponse.cs`

```csharp
namespace payzen_backend.DTOs.Common;

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Type { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Details { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

**FICHIER À CRÉER :** `DTOs/Common/ApiResponse.cs`

```csharp
namespace payzen_backend.DTOs.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}
```

**FICHIER À CRÉER :** `DTOs/Common/PaginatedResponse.cs`

```csharp
namespace payzen_backend.DTOs.Common;

public class PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

### 3.6 Enregistrement des Middlewares dans Program.cs

**FICHIER :** `Program.cs`  
**ACTION :** Ajouter l'utilisation des middlewares

```csharp
// CONFIGURATION SERVICES (avant builder.Build())
builder.Services.AddCustomRateLimiting();

// PIPELINE (après builder.Build(), dans l'ordre)
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseRateLimiter();
// ... puis UseRouting, UseAuthentication, UseAuthorization, MapControllers
```

---

## 4. Phase 3 : Services Layer

### 4.1 Structure des Interfaces de Services

**CRÉER LE DOSSIER :** `Services/Interfaces/`

Pour chaque service existant SANS interface, créer l'interface correspondante.

### 4.2 Liste des Interfaces à Créer

| Service Existant | Interface à Créer | Fichier |
|------------------|-------------------|---------|
| `JwtService` | `IJwtService` | `Services/Interfaces/IJwtService.cs` |
| `PasswordGeneratorService` | `IPasswordGeneratorService` | `Services/Interfaces/IPasswordGeneratorService.cs` |
| `EmployeeEventLogService` | `IEmployeeEventLogService` | `Services/Interfaces/IEmployeeEventLogService.cs` |
| `CompanyEventLogService` | `ICompanyEventLogService` | `Services/Interfaces/ICompanyEventLogService.cs` |
| `LeaveEventLogService` | `ILeaveEventLogService` | `Services/Interfaces/ILeaveEventLogService.cs` |
| `WorkingDaysCalculator` | `IWorkingDaysCalculator` | `Services/Interfaces/IWorkingDaysCalculator.cs` |
| `LeaveBalanceService` | `ILeaveBalanceService` | `Services/Interfaces/ILeaveBalanceService.cs` |
| `PaieService` | `IPaieService` | `Services/Interfaces/IPaieService.cs` |
| `PayrollCalculationEngine` | `IPayrollCalculationEngine` | `Services/Interfaces/IPayrollCalculationEngine.cs` |
| `EmployeePayrollDataService` | `IEmployeePayrollDataService` | `Services/Interfaces/IEmployeePayrollDataService.cs` |

### 4.3 Pattern de Création d'Interface

**TEMPLATE :**

```csharp
// Services/Interfaces/I{ServiceName}.cs
namespace payzen_backend.Services.Interfaces;

public interface I{ServiceName}
{
    // Copier les signatures des méthodes publiques du service
    // Toutes les méthodes retournant des données doivent être async
    // Ajouter CancellationToken en paramètre optionnel
}
```

**EXEMPLE - IJwtService.cs :**

```csharp
namespace payzen_backend.Services.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);
    ClaimsPrincipal? ValidateToken(string token);
    int? GetUserIdFromToken(string token);
    string? GetEmailFromToken(string token);
}
```

**EXEMPLE - ILeaveBalanceService.cs :**

```csharp
namespace payzen_backend.Services.Interfaces;

public interface ILeaveBalanceService
{
    Task<LeaveBalance?> GetBalanceAsync(int employeeId, int leaveTypeId, int year, CancellationToken ct = default);
    Task<IEnumerable<LeaveBalance>> GetBalancesByEmployeeAsync(int employeeId, int year, CancellationToken ct = default);
    Task<LeaveBalance> CreateOrUpdateBalanceAsync(LeaveBalance balance, CancellationToken ct = default);
    Task RecalculateBalanceAsync(int employeeId, int leaveTypeId, int year, int month, CancellationToken ct = default);
    Task<decimal> CalculateUsedDaysAsync(int employeeId, int leaveTypeId, int year, CancellationToken ct = default);
}
```

### 4.4 Refactorisation des Services Existants

**PATTERN DE REFACTORISATION :**

**AVANT (Service sans interface) :**
```csharp
namespace payzen_backend.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;
    // ...
}
```

**APRÈS (Service avec interface) :**
```csharp
namespace payzen_backend.Services;

public class JwtService : IJwtService
{
    private readonly JwtOptions _options;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtOptions> options, ILogger<JwtService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    // Implémentation...
}
```

### 4.5 Enregistrement des Services dans DI

**FICHIER À CRÉER :** `Extensions/ServiceCollectionExtensions.cs`

```csharp
namespace payzen_backend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // === AUTH SERVICES ===
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordGeneratorService, PasswordGeneratorService>();

        // === EVENT LOG SERVICES ===
        services.AddScoped<IEmployeeEventLogService, EmployeeEventLogService>();
        services.AddScoped<ICompanyEventLogService, CompanyEventLogService>();
        services.AddScoped<ILeaveEventLogService, LeaveEventLogService>();

        // === CALCULATORS ===
        services.AddScoped<IWorkingDaysCalculator, WorkingDaysCalculator>();
        services.AddScoped<IPayrollCalculationEngine, PayrollCalculationEngine>();

        // === DOMAIN SERVICES ===
        services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
        services.AddScoped<IPaieService, PaieService>();
        services.AddScoped<IEmployeePayrollDataService, EmployeePayrollDataService>();

        // === EXISTING SERVICES (avec interfaces) ===
        services.AddScoped<IClaudeService, ClaudeService>();
        services.AddScoped<IClaudeSimulationService, ClaudeSimulationService>();
        services.AddScoped<IMoroccanPayrollService, MoroccanPayrollService>();
        services.AddScoped<ICompanyDocumentService, CompanyDocumentService>();
        services.AddScoped<ICompanyOnboardingService, CompanyOnboardingService>();
        services.AddScoped<ICompanyDefaultsSeeder, CompanyDefaultsSeeder>();
        services.AddScoped<IPayrollExportService, PayrollExportService>();
        services.AddScoped<IDashboardHrService, DashboardHrService>();
        services.AddScoped<IConvergenceAnalysisService, ConvergenceAnalysisService>();
        services.AddScoped<IReferentialValidationService, ReferentialValidationService>();
        services.AddScoped<IElementRuleResolutionService, ElementRuleResolutionService>();

        return services;
    }
}
```

### 4.6 Nouveaux Services à Créer (par Domaine)

Pour améliorer la séparation des responsabilités, créer ces services qui encapsulent la logique actuellement dans les contrôleurs :

| Domaine | Service | Responsabilité |
|---------|---------|----------------|
| Employee | `IEmployeeService` | CRUD employés, recherche, filtrage |
| Employee | `IEmployeeContractService` | Gestion des contrats |
| Employee | `IEmployeeAttendanceService` | Pointage, pauses |
| Leave | `ILeaveRequestService` | Workflow des demandes de congés |
| Leave | `ILeaveTypeService` | CRUD types de congés |
| Payroll | `IPayslipService` | Génération bulletins de paie |
| Company | `ICompanyService` | CRUD entreprises |
| Auth | `IAuthService` | Login, logout, gestion tokens |
| Referentiel | `IReferentialDataService` | Données de référence génériques |

---

## 5. Phase 4 : Controllers Layer

### 5.1 Pattern de Refactorisation des Contrôleurs

**OBJECTIF :** Contrôleur mince qui délègue au service

**AVANT (Contrôleur avec logique EF directe) :**

```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<Employee>>> GetAll()
{
    var employees = await _context.Employees
        .AsNoTracking()
        .Where(e => e.DeletedAt == null)
        .Include(e => e.Company)
        .ToListAsync();
    return Ok(employees);
}
```

**APRÈS (Contrôleur délégant au service) :**

```csharp
[HttpGet]
[ProducesResponseType(typeof(ApiResponse<IEnumerable<EmployeeReadDto>>), StatusCodes.Status200OK)]
public async Task<ActionResult<ApiResponse<IEnumerable<EmployeeReadDto>>>> GetAll(
    [FromQuery] EmployeeFilterDto filter,
    CancellationToken cancellationToken)
{
    var employees = await _employeeService.GetAllAsync(filter, cancellationToken);
    return Ok(ApiResponse<IEnumerable<EmployeeReadDto>>.Ok(employees));
}
```

### 5.2 Structure Standard d'un Contrôleur Refactorisé

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using payzen_backend.Authorization;
using payzen_backend.DTOs.Common;
using payzen_backend.Models.Employee.Dtos;
using payzen_backend.Services.Interfaces;

namespace payzen_backend.Controllers.Employee;

/// <summary>
/// Gestion des employés
/// </summary>
[Route("api/v{version:apiVersion}/employees")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(
        IEmployeeService employeeService,
        ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère tous les employés avec filtres optionnels
    /// </summary>
    /// <param name="filter">Critères de filtrage</param>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>Liste des employés</returns>
    [HttpGet]
    [HasPermission("employees.read")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<EmployeeReadDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EmployeeReadDto>>>> GetAll(
        [FromQuery] EmployeeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetAllAsync(filter, cancellationToken);
        return Ok(ApiResponse<PaginatedResponse<EmployeeReadDto>>.Ok(result));
    }

    /// <summary>
    /// Récupère un employé par son ID
    /// </summary>
    [HttpGet("{id:int}")]
    [HasPermission("employees.read")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EmployeeDetailDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeService.GetByIdAsync(id, cancellationToken);
        if (employee == null)
            return NotFound(new ErrorResponse { Message = $"Employé {id} non trouvé" });
        
        return Ok(ApiResponse<EmployeeDetailDto>.Ok(employee));
    }

    /// <summary>
    /// Crée un nouvel employé
    /// </summary>
    [HttpPost]
    [HasPermission("employees.create")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<EmployeeReadDto>>> Create(
        [FromBody] EmployeeCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var created = await _employeeService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            ApiResponse<EmployeeReadDto>.Ok(created, "Employé créé avec succès"));
    }

    /// <summary>
    /// Met à jour un employé
    /// </summary>
    [HttpPut("{id:int}")]
    [HasPermission("employees.update")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EmployeeReadDto>>> Update(
        int id,
        [FromBody] EmployeeUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var updated = await _employeeService.UpdateAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<EmployeeReadDto>.Ok(updated, "Employé mis à jour"));
    }

    /// <summary>
    /// Supprime un employé (soft delete)
    /// </summary>
    [HttpDelete("{id:int}")]
    [HasPermission("employees.delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        await _employeeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
```

### 5.3 Appliquer Rate Limiting aux Contrôleurs LLM

```csharp
[Route("api/v{version:apiVersion}/simulation")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
[EnableRateLimiting("llm")] // <-- Rate limiting spécifique
public class ClaudeSimulationController : ControllerBase
{
    // ...
}
```

---

## 6. Phase 5 : Models & DTOs Layer

### 6.1 Réorganisation des DTOs

**STRUCTURE CIBLE :**

```
DTOs/
├── Common/
│   ├── ApiResponse.cs
│   ├── ErrorResponse.cs
│   ├── PaginatedResponse.cs
│   └── FilterDto.cs
├── Auth/
│   ├── LoginRequestDto.cs
│   ├── LoginResponseDto.cs
│   ├── TokenRefreshDto.cs
│   └── ChangePasswordDto.cs
├── Employee/
│   ├── EmployeeCreateDto.cs
│   ├── EmployeeUpdateDto.cs
│   ├── EmployeeReadDto.cs
│   ├── EmployeeDetailDto.cs
│   ├── EmployeeFilterDto.cs
│   └── EmployeeSummaryDto.cs
├── Leave/
│   ├── LeaveRequestCreateDto.cs
│   ├── LeaveRequestUpdateDto.cs
│   ├── LeaveRequestReadDto.cs
│   ├── LeaveBalanceDto.cs
│   └── LeaveTypeDto.cs
├── Payroll/
│   ├── PayrollCalculationDto.cs
│   ├── PayslipDto.cs
│   ├── SalaryComponentDto.cs
│   └── Referentiel/
│       ├── ElementRuleDto.cs
│       └── ...
└── Company/
    ├── CompanyCreateDto.cs
    ├── CompanyUpdateDto.cs
    └── CompanyReadDto.cs
```

### 6.2 Pattern de Naming pour les DTOs

| Type de DTO | Suffixe | Exemple |
|-------------|---------|---------|
| Création | `CreateDto` | `EmployeeCreateDto` |
| Mise à jour complète | `UpdateDto` | `EmployeeUpdateDto` |
| Mise à jour partielle | `PatchDto` | `EmployeePatchDto` |
| Lecture liste | `ReadDto` | `EmployeeReadDto` |
| Lecture détaillée | `DetailDto` | `EmployeeDetailDto` |
| Filtrage/Recherche | `FilterDto` | `EmployeeFilterDto` |
| Résumé | `SummaryDto` | `EmployeeSummaryDto` |

### 6.3 DTO de Filtrage Standard

**FICHIER :** `DTOs/Common/FilterDto.cs`

```csharp
namespace payzen_backend.DTOs.Common;

public abstract class FilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public string? SearchTerm { get; set; }
}
```

**FICHIER :** `DTOs/Employee/EmployeeFilterDto.cs`

```csharp
namespace payzen_backend.DTOs.Employee;

public class EmployeeFilterDto : FilterDto
{
    public int? CompanyId { get; set; }
    public int? DepartmentId { get; set; }
    public int? StatusId { get; set; }
    public int? CategoryId { get; set; }
    public DateTime? HiredAfter { get; set; }
    public DateTime? HiredBefore { get; set; }
}
```

### 6.4 Mapping Entité ↔ DTO

**OPTION A : Extension Methods (Simple)**

```csharp
// Extensions/MappingExtensions.cs
namespace payzen_backend.Extensions;

public static class MappingExtensions
{
    public static EmployeeReadDto ToReadDto(this Employee entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Email = entity.Email,
        CompanyId = entity.CompanyId,
        CompanyName = entity.Company?.Name
    };

    public static Employee ToEntity(this EmployeeCreateDto dto) => new()
    {
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email,
        CompanyId = dto.CompanyId
    };

    public static void ApplyTo(this EmployeeUpdateDto dto, Employee entity)
    {
        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.Email = dto.Email;
        // ...
    }
}
```

**OPTION B : AutoMapper (Pour projets plus larges)**

```bash
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
```

```csharp
// Mapping/EmployeeProfile.cs
public class EmployeeProfile : Profile
{
    public EmployeeProfile()
    {
        CreateMap<Employee, EmployeeReadDto>()
            .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.Company.Name));
        CreateMap<EmployeeCreateDto, Employee>();
        CreateMap<EmployeeUpdateDto, Employee>();
    }
}
```

---

## 7. Phase 6 : Repository Pattern

### 7.1 Interface Générique de Repository

**FICHIER À CRÉER :** `Data/Repositories/Interfaces/IRepository.cs`

```csharp
using System.Linq.Expressions;

namespace payzen_backend.Data.Repositories.Interfaces;

public interface IRepository<TEntity> where TEntity : class
{
    // Lecture
    Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TEntity?> GetByIdAsync(int id, params Expression<Func<TEntity, object>>[] includes);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    // Écriture
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entities);

    // Pagination
    Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<TEntity, bool>>? predicate,
        Expression<Func<TEntity, object>>? orderBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken ct = default);
}
```

### 7.2 Implémentation Générique

**FICHIER À CRÉER :** `Data/Repositories/Implementations/Repository.cs`

```csharp
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using payzen_backend.Data.Repositories.Interfaces;

namespace payzen_backend.Data.Repositories.Implementations;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, ct);
    }

    public virtual async Task<TEntity?> GetByIdAsync(
        int id,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;
        foreach (var include in includes)
            query = query.Include(include);
        
        return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(ct);
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(ct);
    }

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        return await _dbSet.AnyAsync(predicate, ct);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        return predicate == null
            ? await _dbSet.CountAsync(ct)
            : await _dbSet.CountAsync(predicate, ct);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        await _dbSet.AddRangeAsync(entities, ct);
    }

    public virtual void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public virtual async Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        Expression<Func<TEntity, bool>>? predicate,
        Expression<Func<TEntity, object>>? orderBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(ct);

        if (orderBy != null)
            query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
```

### 7.3 Interface Unit of Work

**FICHIER À CRÉER :** `Data/Repositories/Interfaces/IUnitOfWork.cs`

```csharp
namespace payzen_backend.Data.Repositories.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Employee> Employees { get; }
    IRepository<Company> Companies { get; }
    IRepository<LeaveRequest> LeaveRequests { get; }
    IRepository<LeaveBalance> LeaveBalances { get; }
    // Ajouter autres repositories...

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
```

### 7.4 Implémentation Unit of Work

**FICHIER À CRÉER :** `Data/Repositories/Implementations/UnitOfWork.cs`

```csharp
using Microsoft.EntityFrameworkCore.Storage;
using payzen_backend.Data.Repositories.Interfaces;
using payzen_backend.Models.Employee;
using payzen_backend.Models.Company;
using payzen_backend.Models.Leave;

namespace payzen_backend.Data.Repositories.Implementations;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    private IRepository<Employee>? _employees;
    private IRepository<Company>? _companies;
    private IRepository<LeaveRequest>? _leaveRequests;
    private IRepository<LeaveBalance>? _leaveBalances;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<Employee> Employees => 
        _employees ??= new Repository<Employee>(_context);

    public IRepository<Company> Companies => 
        _companies ??= new Repository<Company>(_context);

    public IRepository<LeaveRequest> LeaveRequests => 
        _leaveRequests ??= new Repository<LeaveRequest>(_context);

    public IRepository<LeaveBalance> LeaveBalances => 
        _leaveBalances ??= new Repository<LeaveBalance>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
```

### 7.5 Enregistrement dans DI

```csharp
// Dans ServiceCollectionExtensions.cs
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

---

## 8. Phase 7 : Validation Layer

### 8.1 Installation FluentValidation

```bash
dotnet add package FluentValidation.AspNetCore
```

### 8.2 Création du Dossier Validators

```bash
mkdir payzen_backend/payzen_backend/Validators
mkdir payzen_backend/payzen_backend/Validators/Employee
mkdir payzen_backend/payzen_backend/Validators/Leave
mkdir payzen_backend/payzen_backend/Validators/Payroll
mkdir payzen_backend/payzen_backend/Validators/Company
mkdir payzen_backend/payzen_backend/Validators/Auth
```

### 8.3 Exemples de Validators

**FICHIER :** `Validators/Employee/EmployeeCreateDtoValidator.cs`

```csharp
using FluentValidation;
using payzen_backend.DTOs.Employee;

namespace payzen_backend.Validators.Employee;

public class EmployeeCreateDtoValidator : AbstractValidator<EmployeeCreateDto>
{
    public EmployeeCreateDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Le prénom est requis")
            .MaximumLength(100).WithMessage("Le prénom ne peut pas dépasser 100 caractères");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Le nom est requis")
            .MaximumLength(100).WithMessage("Le nom ne peut pas dépasser 100 caractères");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est requis")
            .EmailAddress().WithMessage("Format d'email invalide")
            .MaximumLength(255).WithMessage("L'email ne peut pas dépasser 255 caractères");

        RuleFor(x => x.CompanyId)
            .GreaterThan(0).WithMessage("L'entreprise est requise");

        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("La date d'embauche est requise")
            .LessThanOrEqualTo(DateTime.Today).WithMessage("La date d'embauche ne peut pas être dans le futur");

        RuleFor(x => x.CIN)
            .Matches(@"^[A-Z]{1,2}\d{5,6}$")
            .When(x => !string.IsNullOrEmpty(x.CIN))
            .WithMessage("Format CIN invalide (ex: AB123456)");
    }
}
```

**FICHIER :** `Validators/Leave/LeaveRequestCreateDtoValidator.cs`

```csharp
using FluentValidation;
using payzen_backend.DTOs.Leave;

namespace payzen_backend.Validators.Leave;

public class LeaveRequestCreateDtoValidator : AbstractValidator<LeaveRequestCreateDto>
{
    public LeaveRequestCreateDtoValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("L'employé est requis");

        RuleFor(x => x.LeaveTypeId)
            .GreaterThan(0).WithMessage("Le type de congé est requis");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("La date de début est requise")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("La date de début ne peut pas être dans le passé");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("La date de fin est requise")
            .GreaterThanOrEqualTo(x => x.StartDate).WithMessage("La date de fin doit être après la date de début");

        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Le motif ne peut pas dépasser 500 caractères");

        RuleFor(x => x)
            .Must(x => (x.EndDate - x.StartDate).TotalDays <= 30)
            .WithMessage("La durée du congé ne peut pas dépasser 30 jours");
    }
}
```

### 8.4 Enregistrement des Validators

```csharp
// Dans ServiceCollectionExtensions.cs
using FluentValidation;
using FluentValidation.AspNetCore;

public static IServiceCollection AddValidators(this IServiceCollection services)
{
    services.AddFluentValidationAutoValidation();
    services.AddValidatorsFromAssemblyContaining<EmployeeCreateDtoValidator>();
    return services;
}
```

### 8.5 Gestion des Erreurs de Validation

Le middleware `ExceptionHandlerMiddleware` gère déjà les `ValidationException`. Pour personnaliser :

```csharp
// Dans Program.cs, après AddControllers()
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
                );

            var response = new ErrorResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Type = "ValidationError",
                Message = "Erreur de validation",
                Errors = errors
            };

            return new BadRequestObjectResult(response);
        };
    });
```

---

## 9. Phase 8 : Configuration & Options Pattern

### 9.1 Création des Classes Options

**FICHIER À CRÉER :** `Configuration/JwtOptions.cs`

```csharp
namespace payzen_backend.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpirationMinutes { get; set; } = 60;
    public int RefreshExpirationDays { get; set; } = 7;
}
```

**FICHIER À CRÉER :** `Configuration/AnthropicOptions.cs`

```csharp
namespace payzen_backend.Configuration;

public class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    public string ApiKey { get; set; } = null!;
    public string Model { get; set; } = "claude-3-5-sonnet-20241022";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.7;
}
```

**FICHIER À CRÉER :** `Configuration/GoogleOptions.cs`

```csharp
namespace payzen_backend.Configuration;

public class GoogleOptions
{
    public const string SectionName = "Google";

    public string ApiKey { get; set; } = null!;
    public string Model { get; set; } = "gemini-pro";
}
```

**FICHIER À CRÉER :** `Configuration/DatabaseOptions.cs`

```csharp
namespace payzen_backend.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    public string DefaultConnection { get; set; } = null!;
    public int CommandTimeout { get; set; } = 30;
    public bool EnableDetailedErrors { get; set; } = false;
    public bool EnableSensitiveDataLogging { get; set; } = false;
}
```

### 9.2 Enregistrement des Options

**FICHIER :** `Extensions/ServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddOptions(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
    services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));
    services.Configure<GoogleOptions>(configuration.GetSection(GoogleOptions.SectionName));
    services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

    // Ajout de la validation au démarrage
    services.AddOptions<JwtOptions>()
        .Bind(configuration.GetSection(JwtOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    return services;
}
```

### 9.3 Utilisation dans les Services

**AVANT :**
```csharp
public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(...)
    {
        var key = _configuration["Jwt:Key"]; // String nullable, pas de validation
    }
}
```

**APRÈS :**
```csharp
public class JwtService : IJwtService
{
    private readonly JwtOptions _options;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtOptions> options, ILogger<JwtService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string GenerateToken(...)
    {
        var key = _options.Key; // Fortement typé, validé au démarrage
    }
}
```

---

## 10. Checklist de Refactorisation par Domaine

### 10.1 Auth (6 contrôleurs)

| Fichier | Versioning | Service Interface | Rate Limiting | Validation | DTO |
|---------|------------|-------------------|---------------|------------|-----|
| AuthController | ⬜ | ⬜ IAuthService | ⬜ "auth" | ⬜ LoginRequestValidator | ⬜ |
| PermissionsController | ⬜ | ⬜ IPermissionService | ⬜ - | ⬜ PermissionDtoValidator | ⬜ |
| RolesController | ⬜ | ⬜ IRoleService | ⬜ - | ⬜ RoleDtoValidator | ⬜ |
| RolesPermissionsController | ⬜ | ⬜ - | ⬜ - | ⬜ | ⬜ |
| UsersController | ⬜ | ⬜ IUserService | ⬜ - | ⬜ UserCreateDtoValidator | ⬜ |
| UsersRolesController | ⬜ | ⬜ - | ⬜ - | ⬜ | ⬜ |

### 10.2 Company (4 contrôleurs)

| Fichier | Versioning | Service Interface | Validation | DTO |
|---------|------------|-------------------|------------|-----|
| CompanyController | ⬜ | ⬜ ICompanyService | ⬜ CompanyCreateDtoValidator | ⬜ |
| CompanyDocumentsController | ⬜ | ✅ ICompanyDocumentService | ⬜ | ⬜ |
| DepartementsController | ⬜ | ⬜ IDepartmentService | ⬜ | ⬜ |
| JobPositionsController | ⬜ | ⬜ IJobPositionService | ⬜ | ⬜ |

### 10.3 Employee (14 contrôleurs)

| Fichier | Versioning | Service Interface | Validation | DTO |
|---------|------------|-------------------|------------|-----|
| EmployeeController | ⬜ | ⬜ IEmployeeService | ⬜ | ⬜ |
| EmployeeAbsenceController | ⬜ | ⬜ IAbsenceService | ⬜ | ⬜ |
| EmployeeAddresssController | ⬜ | ⬜ IEmployeeAddressService | ⬜ | ⬜ |
| EmployeeAttendanceController | ⬜ | ⬜ IAttendanceService | ⬜ | ⬜ |
| EmployeeAttendanceBreakController | ⬜ | ⬜ - | ⬜ | ⬜ |
| EmployeeCategoryController | ⬜ | ⬜ - | ⬜ | ⬜ |
| EmployeeChildController | ⬜ | ⬜ - | ⬜ | ⬜ |
| EmployeeContractsController | ⬜ | ⬜ IContractService | ⬜ | ⬜ |
| EmployeeDocumentsController | ⬜ | ⬜ - | ⬜ | ⬜ |
| EmployeeOvertimeController | ⬜ | ⬜ IOvertimeService | ⬜ | ⬜ |
| EmployeeSalariesController | ⬜ | ⬜ - | ⬜ | ⬜ |
| EmployeeSalaryComponentsController | ⬜ | ⬜ - | ⬜ | ⬜ |
| EmployeeSpouseController | ⬜ | ⬜ - | ⬜ | ⬜ |
| SalaryPackageAssignmentsController | ⬜ | ⬜ - | ⬜ | ⬜ |

### 10.4 Leave (10 contrôleurs)

| Fichier | Versioning | Service Interface | Validation | DTO |
|---------|------------|-------------------|------------|-----|
| LeaveAuditLogController | ⬜ | ⬜ - | ⬜ | ⬜ |
| LeaveBalanceController | ⬜ | ⬜ ILeaveBalanceService | ⬜ | ⬜ |
| LeaveCarryOverAgreementController | ⬜ | ⬜ - | ⬜ | ⬜ |
| LeaveRequestController | ⬜ | ⬜ ILeaveRequestService | ⬜ | ⬜ |
| LeaveRequestApprovalHistoryController | ⬜ | ⬜ - | ⬜ | ⬜ |
| LeaveRequestAttachmentController | ⬜ | ⬜ - | ⬜ | ⬜ |
| LeaveRequestExemptionController | ⬜ | ⬜ - | ⬜ | ⬜ |
| LeaveTypeController | ⬜ | ⬜ ILeaveTypeService | ⬜ | ⬜ |
| LeaveTypeLegalRuleController | ⬜ | ⬜ - | ⬜ | ⬜ |
| LeaveTypePolicyController | ⬜ | ⬜ - | ⬜ | ⬜ |

### 10.5 Payroll (11 contrôleurs)

| Fichier | Versioning | Service Interface | Rate Limiting | Validation |
|---------|------------|-------------------|---------------|------------|
| ClaudeSimulationController | ⬜ | ✅ IClaudeSimulationService | ⬜ "llm" | ⬜ |
| PayrollController | ⬜ | ⬜ IPayrollService | ⬜ | ⬜ |
| PayrollExportController | ⬜ | ✅ IPayrollExportService | ⬜ | ⬜ |
| PayslipController | ⬜ | ⬜ IPayslipService | ⬜ | ⬜ |
| SalaryPreviewController | ⬜ | ⬜ ISalaryPreviewService | ⬜ | ⬜ |
| AncienneteRateSetsController | ⬜ | ⬜ - | ⬜ | ⬜ |
| AuthoritiesController | ⬜ | ⬜ - | ⬜ | ⬜ |
| BusinessSectorsController | ⬜ | ⬜ - | ⬜ | ⬜ |
| ElementCategoriesController | ⬜ | ⬜ - | ⬜ | ⬜ |
| ElementRulesController | ⬜ | ✅ IElementRuleResolutionService | ⬜ | ⬜ |

### 10.6 Referentiel & SystemData (14 contrôleurs)

| Fichier | Versioning | Service Interface |
|---------|------------|-------------------|
| EducationLevelsController | ⬜ | ⬜ IReferentialService<EducationLevel> |
| GendersController | ⬜ | ⬜ IReferentialService<Gender> |
| LegalContractTypeController | ⬜ | ⬜ |
| MaritalStatusesController | ⬜ | ⬜ |
| OvertimeRateRuleController | ⬜ | ⬜ |
| StateEmploymentProgramController | ⬜ | ⬜ |
| StatusesController | ⬜ | ⬜ |
| CitiesController | ⬜ | ⬜ |
| ContractTypesController | ⬜ | ⬜ |
| CountriesController | ⬜ | ⬜ |
| HolidaysController | ⬜ | ⬜ |
| PayComponentsController | ⬜ | ⬜ |
| SalaryPackagesController | ⬜ | ⬜ |
| WorkingCalendarsController | ⬜ | ⬜ |

---

## 11. Ordre d'Exécution Recommandé

### Phase 1: Infrastructure (Priorité HAUTE) - 2-3 jours

```
1. ⬜ Installer packages (Asp.Versioning, FluentValidation, etc.)
2. ⬜ Créer dossier Middleware/
3. ⬜ Créer ExceptionHandlerMiddleware.cs
4. ⬜ Créer RequestLoggingMiddleware.cs
5. ⬜ Créer RateLimitingConfiguration.cs
6. ⬜ Créer DTOs/Common/ (ApiResponse, ErrorResponse, PaginatedResponse)
7. ⬜ Créer dossier Configuration/
8. ⬜ Créer classes Options (JwtOptions, AnthropicOptions, etc.)
9. ⬜ Créer Extensions/ServiceCollectionExtensions.cs
10. ⬜ Créer Extensions/ApplicationBuilderExtensions.cs
11. ⬜ Refactorer Program.cs pour utiliser les extensions
```

### Phase 2: API Versioning (Priorité HAUTE) - 1-2 jours

```
12. ⬜ Configurer API Versioning dans Program.cs
13. ⬜ Refactorer TOUS les contrôleurs (ajouter attributs versioning)
    - ⬜ Auth (6 contrôleurs)
    - ⬜ Company (4 contrôleurs)
    - ⬜ Employee (14 contrôleurs)
    - ⬜ Leave (10 contrôleurs)
    - ⬜ Payroll (11 contrôleurs)
    - ⬜ Referentiel (7 contrôleurs)
    - ⬜ SystemData (7 contrôleurs)
```

### Phase 3: Services Layer (Priorité MOYENNE) - 3-4 jours

```
14. ⬜ Créer dossier Services/Interfaces/
15. ⬜ Créer interfaces pour services existants SANS interface:
    - ⬜ IJwtService
    - ⬜ IPasswordGeneratorService
    - ⬜ IEmployeeEventLogService
    - ⬜ ICompanyEventLogService
    - ⬜ ILeaveEventLogService
    - ⬜ IWorkingDaysCalculator
    - ⬜ ILeaveBalanceService
    - ⬜ IPaieService
    - ⬜ IPayrollCalculationEngine
    - ⬜ IEmployeePayrollDataService
16. ⬜ Modifier les services pour implémenter les interfaces
17. ⬜ Mettre à jour l'enregistrement DI
```

### Phase 4: Repository Pattern (Priorité MOYENNE) - 2-3 jours

```
18. ⬜ Créer Data/Repositories/Interfaces/
19. ⬜ Créer IRepository<T>
20. ⬜ Créer IUnitOfWork
21. ⬜ Créer Data/Repositories/Implementations/
22. ⬜ Créer Repository<T>
23. ⬜ Créer UnitOfWork
24. ⬜ Enregistrer dans DI
```

### Phase 5: Validation (Priorité MOYENNE) - 2 jours

```
25. ⬜ Créer dossier Validators/
26. ⬜ Créer validators pour DTOs critiques:
    - ⬜ Employee (Create, Update)
    - ⬜ LeaveRequest (Create, Update)
    - ⬜ Company (Create, Update)
    - ⬜ Auth (Login)
27. ⬜ Configurer FluentValidation dans Program.cs
28. ⬜ Configurer la gestion des erreurs de validation
```

### Phase 6: Controllers Refactoring (Priorité BASSE) - 5-7 jours

```
29. ⬜ Refactorer contrôleurs par domaine (enlever accès direct DbContext):
    - ⬜ EmployeeController → IEmployeeService
    - ⬜ LeaveRequestController → ILeaveRequestService
    - ⬜ PayrollController → IPayrollService
    - ⬜ ... (autres)
30. ⬜ Ajouter documentation Swagger (/// <summary>)
31. ⬜ Ajouter [ProducesResponseType] partout
```

### Phase 7: Tests (Priorité BASSE) - Continu

```
32. ⬜ Ajouter tests unitaires pour nouveaux services
33. ⬜ Ajouter tests d'intégration pour endpoints critiques
```

---

## 📝 Notes pour AI Copilot

### Règles de Refactorisation

1. **NE JAMAIS casser la rétrocompatibilité** - Les routes v1 doivent fonctionner comme avant
2. **Un commit = une tâche** - Commits atomiques et bien nommés
3. **Tester après chaque modification** - `dotnet build` puis `dotnet test`
4. **Conserver les permissions** - Les `[HasPermission]` existants doivent être préservés
5. **Logging partout** - Ajouter `ILogger<T>` dans tous les nouveaux composants

### Patterns à Utiliser

```csharp
// Pour les méthodes async - TOUJOURS utiliser CancellationToken
Task<T> MethodAsync(int id, CancellationToken ct = default);

// Pour le soft delete - TOUJOURS vérifier DeletedAt
query.Where(e => e.DeletedAt == null)

// Pour la pagination - TOUJOURS retourner TotalCount
(IEnumerable<T> Items, int TotalCount)

// Pour les erreurs - TOUJOURS utiliser les exceptions typées
throw new KeyNotFoundException($"Entity {id} not found");
throw new InvalidOperationException("Cannot perform operation");
```

### Commandes Utiles

```bash
# Build
dotnet build

# Tests
dotnet test

# Run
dotnet run --project payzen_backend/payzen_backend

# Ajouter migration
dotnet ef migrations add MigrationName --project payzen_backend/payzen_backend

# Appliquer migrations
dotnet ef database update --project payzen_backend/payzen_backend
```

---

---

## Phase 1 - Implémentation appliquée (copy: payzen_backend_refactored)

Les fichiers suivants ont été créés ou modifiés lors de la Phase 1 (Infrastructure) dans la copie refactorisée `payzen_backend_refactored/` :

- Middleware/RequestLoggingMiddleware.cs
- Middleware/ExceptionHandlerMiddleware.cs
- Middleware/RateLimitingConfiguration.cs
- DTOs/Common/ErrorResponse.cs
- DTOs/Common/ApiResponse.cs
- DTOs/Common/PaginatedResponse.cs
- Configuration/JwtOptions.cs
- Configuration/AnthropicOptions.cs
- Configuration/GoogleOptions.cs
- Configuration/DatabaseOptions.cs
- Extensions/ServiceCollectionExtensions.cs
- Extensions/ApplicationBuilderExtensions.cs
- Program.cs (modifié : enregistre le rate limiter, options et utilise les middlewares)

Tous ces fichiers contiennent des commentaires explicatifs pour faciliter la revue.

Après revue, validez la compilation avec :

```bash
dotnet build
```

---

*Guide généré le Mars 2026 - Version 1.0*
