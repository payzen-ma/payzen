# Guide – Structure des dossiers, fichiers et conventions de code

Ce document décrit **comment créer** chaque dossier et fichier du projet Payzen payzen_backend, **à quoi ils servent**, et **comment nommer** variables, fonctions et types pour rester cohérent avec les standards .NET.

---

## 1. Vue d’ensemble de la structure recommandée

```
payzen_backend/
├── Authorization/           # Attributs et handlers d'autorisation
├── Controllers/             # Contrôleurs API par domaine
│   ├── Auth/
│   ├── Company/
│   ├── Dashboard/
│   ├── Employee/
│   ├── Event/
│   ├── Leave/
│   ├── Referentiel/
│   └── SystemData/
├── Data/                    # Accès aux données
│   ├── Configurations/      # Configuration EF par entité (optionnel)
│   ├── Repositories/        # Interfaces et implémentations (optionnel)
│   └── AppDbContext.cs
├── Extensions/              # Méthodes d'extension (DI, pipeline, Claims)
├── Middleware/              # Middlewares HTTP (exceptions, logging)
├── Migrations/              # Migrations EF (généré)
├── Models/                  # Entités et DTOs par domaine
│   ├── Auth/
│   ├── Common/
│   ├── Company/
│   │   └── Dtos/
│   ├── Employee/
│   │   └── Dtos/
│   ├── Leave/
│   │   └── Dtos/
│   └── ...
├── Seeding/                 # Données initiales (DbSeeder)
├── Services/                # Services métier et techniques
│   └── Application/        # Services applicatifs (optionnel)
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

---

## 2. Dossiers – Rôle, utilisation, quand créer

### 2.1 `Authorization/`

| Rôle | Contient les attributs et handlers qui vérifient les **permissions** ou **rôles** (au-delà de `[Authorize]`). |
| Utilisation | Fichiers comme `HasPermissionAttribute.cs`, ou `PermissionAuthorizationHandler.cs` si vous passez aux policies. |
| Quand créer | Déjà présent. À compléter si vous ajoutez des policies ou des handlers personnalisés. |

**Exemple de fichier à créer :**
- `HasPermissionAttribute.cs` (existant)
- `PermissionAuthorizationHandler.cs` (si migration vers policy-based auth)

---

### 2.2 `Controllers/`

| Rôle | Regroupe les **contrôleurs API** par **domaine métier**. Chaque sous-dossier = un domaine (Auth, Company, Employee, Leave, etc.). |
| Utilisation | Un fichier par contrôleur. Le contrôleur ne fait qu’appeler des services et renvoyer des réponses HTTP (status + body). Pas de logique métier ni de requêtes EF directes. |
| Quand créer | Un nouveau sous-dossier quand un **nouveau domaine** apparaît. Un nouveau fichier quand une **nouvelle ressource API** (ex. `PaySlipController`) est ajoutée. |

**Convention de nom :**
- Dossier : **PascalCase**, pluriel ou nom de domaine (ex. `Employee`, `Leave`, `Referentiel`).
- Fichier : **NomDuControleur + "Controller"** → `EmployeeController.cs`, `LeaveRequestController.cs`.

---

### 2.3 `Data/`

| Rôle | Tout ce qui concerne la **persistance** : contexte EF, configurations des entités, éventuellement repositories. |
| Utilisation | `AppDbContext.cs` pour le contexte. `Configurations/` pour les classes `IEntityTypeConfiguration<T>`. `Repositories/` pour les interfaces et implémentations de repository. |
| Quand créer | `Configurations/` quand vous découpez `OnModelCreating`. `Repositories/` quand vous introduisez une couche repository. |

**Sous-dossiers :**

| Dossier | Contenu | Exemple de fichier |
|---------|---------|--------------------|
| (racine Data) | `AppDbContext.cs` | Contexte EF, `DbSet<T>`, `OnModelCreating` (ou appel à `ApplyConfigurationsFromAssembly`) |
| `Data/Configurations/` | Une classe par entité (ou groupe) | `EmployeeConfiguration.cs`, `LeaveRequestConfiguration.cs` |
| `Data/Repositories/` | Interfaces + implémentations | `IRepository.cs`, `Repository.cs`, `IEmployeeRepository.cs`, `EmployeeRepository.cs` |

---

### 2.4 `Extensions/`

| Rôle | **Méthodes d’extension** pour `IServiceCollection` (enregistrement des services), `IApplicationBuilder` (pipeline), ou types métier (ex. `ClaimsPrincipal`). |
| Utilisation | Centraliser la configuration (DI, CORS, JWT, Health Checks) et le pipeline (UseCors, UseAuthentication, UseExceptionHandler, etc.). |
| Quand créer | Dès que `Program.cs` devient chargé (ex. après extraction de la config). |

**Convention de nom des fichiers :**
- `ServiceCollectionExtensions.cs` → méthodes `AddXxx(this IServiceCollection services, ...)`
- `ApplicationBuilderExtensions.cs` → méthodes `UseXxx(this IApplicationBuilder app)`
- `ClaimsPrincipalExtensions.cs` → méthodes sur `ClaimsPrincipal` (existant)

---

### 2.5 `Middleware/`

| Rôle | **Middlewares HTTP** qui interviennent dans le pipeline (gestion d’exceptions, logging de requêtes, etc.). |
| Utilisation | Un fichier par middleware. Le middleware traite `HttpContext`, appelle `next()`, et gère les erreurs ou logs. |
| Quand créer | Dès que vous ajoutez un middleware personnalisé (ex. `ExceptionHandlerMiddleware.cs`). |

**Convention de nom :**
- **NomDuComportement + "Middleware"** → `ExceptionHandlerMiddleware.cs`, `RequestLoggingMiddleware.cs`.

---

### 2.6 `Models/`

| Rôle | **Entités** (modèles de domaine / EF) et **DTOs** (objets d’échange API). Organisé par **domaine**. |
| Utilisation | Un sous-dossier par domaine (Auth, Company, Employee, Leave, Referentiel, etc.). Dans chaque domaine : entités à la racine, DTOs dans un sous-dossier `Dtos/`. |
| Quand créer | Nouveau dossier quand un **nouveau domaine** apparaît. Nouveau fichier pour une nouvelle entité ou un nouveau DTO. |

**Structure type d’un domaine :**
```
Models/
└── Employee/
    ├── Employee.cs           # Entité EF
    ├── EmployeeAddress.cs
    └── Dtos/
        ├── EmployeeCreateDto.cs
        ├── EmployeeReadDto.cs
        └── EmployeeUpdateDto.cs
```

**Conventions :**
- **Entité** : singulier, PascalCase → `Employee.cs`, `LeaveRequest.cs`.
- **DTO** : **Sens + "Dto"** → `EmployeeCreateDto.cs`, `LeaveBalanceReadDto.cs`, `CompanyUpdateDto.cs`.

---

### 2.7 `Seeding/`

| Rôle | Données **initiales** ou de référence (référentiels, rôles, permissions). |
| Utilisation | Une classe statique ou un service qui remplit la base de manière **idempotente** (vérifier avant d’insérer). |
| Quand créer | Déjà présent (`DbSeeder.cs`). Nouveau fichier seulement si vous séparez plusieurs seeders (ex. `RoleSeeder.cs`, `LeaveTypeSeeder.cs`). |

---

### 2.8 `Services/`

| Rôle | **Services métier** (règles, orchestration) et **services techniques** (JWT, génération de mot de passe, event log, calculs). |
| Utilisation | Les controllers appellent des services ; les services utilisent le `DbContext` ou les repositories. |
| Quand créer | Un fichier par service. Sous-dossier `Application/` optionnel pour les services “métier” (ex. `EmployeeService`, `LeaveBalanceService`). |

**Convention de nom :**
- **Nom du domaine/capacité + "Service"** → `JwtService.cs`, `EmployeeEventLogService.cs`, `WorkingDaysCalculator.cs` (ou considérer un sous-dossier `Calculators/` si vous en avez plusieurs).

---

## 3. Fichiers – Types, nommage, utilité

### 3.1 Contrôleur (`Controllers/**/*Controller.cs`)

- **Utilité** : Exposer des endpoints HTTP. Reçoit la requête, appelle un service, renvoie une réponse (Ok, NotFound, BadRequest, etc.).
- **Une classe par fichier**, nommée comme le fichier (sans `.cs`).
- **Hérite de** `ControllerBase`.
- **Attributs courants** : `[ApiController]`, `[Route("api/...")]`, `[Authorize]`, `[HasPermission("...")]`.

```csharp
// EmployeeController.cs
namespace payzen_backend.Controllers.Employee;

[Route("api/employee")]
[ApiController]
[Authorize]
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

    /// <summary>Récupère le résumé des employés.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardResponseDto>> GetSummary(
        [FromQuery] int? companyId,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetSummaryAsync(companyId, cancellationToken);
        return Ok(result);
    }
}
```

---

### 3.2 Entité (`Models/**/*.cs`, hors `Dtos/`)

- **Utilité** : Modèle de domaine mappé en base (tables EF). Propriétés = colonnes ou relations.
- **Nom** : **Singulier**, PascalCase → `Employee`, `LeaveRequest`, `User` (pas `Users`).
- **Une classe par fichier**. Pas de logique métier lourde dans l’entité (éventuellement des méthodes simples de validation ou de formatage).

```csharp
// Employee.cs
namespace payzen_backend.Models.Employee;

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int CompanyId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Company? Company { get; set; }
}
```

---

### 3.3 DTO (`Models/**/Dtos/*.cs`)

- **Utilité** : Objets d’**entrée** (Create, Update, Patch) ou de **sortie** (Read, Summary) de l’API. Séparer les noms par usage.
- **Nom** : **Intention + "Dto"** → `EmployeeCreateDto`, `EmployeeReadDto`, `LeaveBalanceReadDto`, `LoginRequest` (ou `LoginRequestDto` si vous uniformisez).

```csharp
// EmployeeCreateDto.cs
namespace payzen_backend.Models.Employee.Dtos;

public class EmployeeCreateDto
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int CompanyId { get; set; }
    // ...
}
```

---

### 3.4 Interface de service (`Services/**/I*.cs` ou `Contracts/`)

- **Utilité** : Contrat du service pour l’injection de dépendances et les tests.
- **Nom** : **I + Nom du service** → `IEmployeeService`, `IJwtService`.
- **Même namespace** que l’implémentation (ou namespace `Contracts` si vous séparez).

```csharp
// IEmployeeService.cs
namespace payzen_backend.Services.Application;

public interface IEmployeeService
{
    Task<DashboardResponseDto> GetSummaryAsync(
        int? companyId,
        CancellationToken cancellationToken = default);
    Task<EmployeeDetailDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);
}
```

---

### 3.5 Implémentation de service (`Services/**/*Service.cs`)

- **Utilité** : Logique métier et appels au `DbContext` ou aux repositories.
- **Nom** : **Nom du domaine/capacité + "Service"** → `EmployeeService`, `JwtService`.
- **Constructeur** : injecter uniquement les dépendances nécessaires (DbContext, repositories, autres services, ILogger).

```csharp
// EmployeeService.cs
namespace payzen_backend.Services.Application;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(AppDbContext db, ILogger<EmployeeService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DashboardResponseDto> GetSummaryAsync(
        int? companyId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Employees.AsNoTracking().Where(e => e.DeletedAt == null);
        if (companyId.HasValue)
            query = query.Where(e => e.CompanyId == companyId.Value);
        // ...
        return result;
    }
}
```

---

### 3.6 Extension (`Extensions/*.cs`)

- **Utilité** : Méthode d’extension pour un type existant. Classe **statique**, méthode **statique** avec `this TypeDuPremierParamètre`.
- **Nom du fichier** : **Type ciblé + "Extensions"** → `ServiceCollectionExtensions`, `ClaimsPrincipalExtensions`.

```csharp
// ServiceCollectionExtensions.cs
namespace payzen_backend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IEmployeeService, EmployeeService>();
        return services;
    }
}
```

---

### 3.7 Middleware (`Middleware/*.cs`)

- **Utilité** : Traiter chaque requête/réponse dans le pipeline. Constructeur reçoit `RequestDelegate next`, méthode `InvokeAsync(HttpContext context)`.
- **Nom** : **Comportement + "Middleware"** → `ExceptionHandlerMiddleware.cs`.

```csharp
// ExceptionHandlerMiddleware.cs
namespace payzen_backend.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur non gérée");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { Message = "Une erreur est survenue." });
        }
    }
}
```

---

### 3.8 Configuration EF (`Data/Configurations/*.cs`)

- **Utilité** : Déplacer la configuration d’une entité hors de `OnModelCreating`. Implémente `IEntityTypeConfiguration<T>`.
- **Nom** : **Nom de l’entité + "Configuration"** → `EmployeeConfiguration.cs`.

```csharp
// EmployeeConfiguration.cs
namespace payzen_backend.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employee");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
        builder.HasIndex(e => e.Email).IsUnique().HasFilter("[DeletedAt] IS NULL");
    }
}
```

---

## 4. Conventions de nommage – Variables, propriétés, méthodes

### 4.1 Variables et champs

| Contexte | Convention | Exemple |
|----------|------------|--------|
| **Champ privé** (classe) | `_camelCase` (préfixe `_`) | `_db`, `_employeeService`, `_logger` |
| **Paramètre de méthode** | `camelCase` | `companyId`, `cancellationToken` |
| **Variable locale** | `camelCase` | `var employee = ...;`, `var totalCount = ...` |
| **Constante locale** | `PascalCase` ou `camelCase` | `const int MaxPageSize = 100;` |
| **Constante de classe / static** | `PascalCase` | `public const string RoleAdmin = "Admin";` |

**À éviter :**
- Champs publics (préférer des propriétés).
- Abréviations obscures (`emp` → `employee`, `ct` → `cancellationToken` si pas le paramètre standard).

---

### 4.2 Propriétés (entités, DTOs, options)

| Contexte | Convention | Exemple |
|----------|------------|--------|
| **Propriété publique** | `PascalCase` | `FirstName`, `CompanyId`, `DeletedAt` |
| **Propriété JSON (API)** | Idem `PascalCase` (ou `[JsonPropertyName("...")]` si vous voulez du camelCase en JSON) | `FirstName` → JSON `"firstName"` si naming policy camelCase |

Pour les **entités** et **DTOs**, rester en **PascalCase** en C# ; la sérialisation JSON peut être configurée en camelCase dans `Program.cs` ou `JsonSerializerOptions`.

---

### 4.3 Méthodes et fonctions

| Contexte | Convention | Exemple |
|----------|------------|--------|
| **Méthode publique / privée** | `PascalCase` | `GetSummaryAsync`, `ValidateEmployee` |
| **Méthode asynchrone** | Suffixe **`Async`** | `GetByIdAsync`, `SaveChangesAsync` |
| **Méthode qui retourne un booléen** | Préfixe **`Is`**, **`Has`**, **`Can`** si pertinent | `IsValid`, `HasPermission`, `CanEdit` |
| **Méthode d’extension** | `PascalCase` (comme toute méthode) | `GetUserId`, `AddApplicationServices` |

**Signatures recommandées :**
- Toujours accepter `CancellationToken cancellationToken = default` pour les méthodes async qui font de l’I/O.
- Retourner `Task<T>` ou `ValueTask<T>` pour l’async, pas `void` (sauf event handlers).

```csharp
// Bon
public async Task<EmployeeDetailDto?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)

// À éviter (pas de CancellationToken)
public async Task<EmployeeDetailDto?> GetDetailsAsync(int id)
```

---

### 4.4 Interfaces et classes

| Élément | Convention | Exemple |
|---------|------------|--------|
| **Interface** | Préfixe **`I`** + PascalCase | `IEmployeeService`, `IRepository` |
| **Classe** | PascalCase, **singulier** pour entités | `Employee`, `LeaveRequest`, `User` |
| **Classe générique** | `T` pour un type, `TKey`, `TEntity` si plusieurs | `IRepository<T>`, `Repository<TEntity, TKey>` |

---

### 4.5 Namespaces

- **Alignés sur la structure des dossiers** : `payzen_backend.Controllers.Employee`, `payzen_backend.Models.Employee.Dtos`, `payzen_backend.Services.Application`, `payzen_backend.Data.Configurations`.
- **Un niveau de namespace par dossier** sous la racine du projet (sauf `Dtos` qui reste sous le domaine).

---

## 5. Récapitulatif – Création d’un nouveau dossier/fichier

| Besoin | Où créer | Nom du fichier / dossier |
|--------|-----------|---------------------------|
| Nouveau domaine API (ex. Paie) | `Controllers/Paie/` | `PaySlipController.cs` |
| Nouvelle entité | `Models/<Domaine>/` | `PaySlip.cs` (singulier) |
| Nouveau DTO | `Models/<Domaine>/Dtos/` | `PaySlipCreateDto.cs`, `PaySlipReadDto.cs` |
| Nouveau service métier | `Services/` ou `Services/Application/` | `IPaySlipService.cs`, `PaySlipService.cs` |
| Nouvelle méthode d’extension | `Extensions/` | `ServiceCollectionExtensions.cs` (ajouter une méthode) ou nouveau fichier si nouveau type ciblé |
| Nouveau middleware | `Middleware/` | `ExceptionHandlerMiddleware.cs` |
| Nouvelle config EF | `Data/Configurations/` | `PaySlipConfiguration.cs` |
| Nouvelle constante (rôles, permissions) | `Models/Permissions/` ou `Constants/` | `RoleNames.cs`, `PermissionsConstants.cs` (existant) |

---

## 6. Checklist rapide avant de commiter

- [ ] Aucun `Console.WriteLine` ; utiliser `ILogger`.
- [ ] Controllers : pas de logique métier ni d’accès direct au `DbContext` si des services existent pour ce domaine.
- [ ] Méthodes async : suffixe `Async` et `CancellationToken` si I/O.
- [ ] Entités et DTOs : propriétés en PascalCase ; entités au singulier.
- [ ] Champs privés : préfixe `_` et camelCase.
- [ ] Nouveau service : interface `IXxxService` + implémentation enregistrée en DI.
- [ ] Nouveau dossier : namespace cohérent avec l’arborescence.

Ce guide peut être utilisé comme référence lors de la refactorisation ou de l’ajout de nouvelles fonctionnalités pour garder une structure et un style de code homogènes.
