# Backend Payzen

Ce dossier contient l'API backend de Payzen, construite en .NET avec une architecture en couches (API, Application, Domain, Infrastructure) et une persistance SQL Server via Entity Framework Core.

L'objectif de ce README est de documenter l'architecture technique, les conventions du projet, les flux principaux et la maniere de demarrer le backend localement.

## Sommaire

- Vue d'ensemble
- Architecture en couches
- Structure du repository backend
- Details par projet
- Flux d'une requete HTTP
- Authentification et authorization
- Persistance et base de donnees
- Validation, erreurs et conventions API
- Configuration et secrets
- Demarrage local
- Migrations EF Core
- Tests
- Bonnes pratiques de maintenance

## Vue d'ensemble

Le backend est une API REST orientee gestion RH/paie, avec des domaines metier tels que :

- Authentification et gestion des utilisateurs/roles/permissions
- Entreprises (company), departements, postes
- Employes (contrats, salaires, absences, presence, heures supplementaires, documents)
- Conges (types, demandes, soldes, politiques, exceptions)
- Paie (calcul, export, simulation, packages salariaux)
- Referentiels metier
- Dashboard et evenements

Le code est organise pour separer clairement :

- Le **transport HTTP** (controllers)
- Les **contrats metier** (interfaces, DTOs, validators)
- Le **coeur metier** (entites, enums, logique de domaine)
- Les **implementations techniques** (EF Core, services, integrations externes)

## Architecture en couches

Architecture logique (de haut en bas) :

1. `Payzen.Api`
   - Expose les endpoints REST
   - Configure le pipeline ASP.NET Core (middlewares, auth, CORS, etc.)
   - Ne contient pas la logique technique lourde (delegue aux services)
2. `Payzen.Application`
   - Definit les contrats applicatifs : interfaces de services, DTOs, validators
   - Contient des composants metier purs (ex: moteur de calcul de paie)
3. `Payzen.Domain`
   - Contient le modele metier central : entites et enums
   - Represente le langage metier et les regles de base du domaine
4. `Payzen.Infrastructure`
   - Implemente les interfaces Application
   - Gere la persistence (EF Core, `AppDbContext`, migrations)
   - Integre les services externes (JWT, PDF, LLM, import/export, etc.)

Dependances attendues :

- `Api` -> `Application` + `Infrastructure`
- `Infrastructure` -> `Application` + `Domain`
- `Application` -> `Domain`
- `Domain` -> (idealement aucune couche metier superieure)

## Structure du repository backend

```text
backend/
  Payzen.sln
  Payzen.Api/              # Couche presentation HTTP
  Payzen.Application/      # Contrats applicatifs, DTOs, validators
  Payzen.Domain/           # Entites et enums metier
  Payzen.Infrastructure/   # EF Core, implementations de services, integrations
  Payzen.Tests/            # Tests unitaires et integration
```

## Details par projet

### 1) `Payzen.Api`

Contient :

- `Program.cs` : point d'entree, DI, middleware pipeline, auth, CORS
- `Controllers/` : endpoints REST par domaine
- `appsettings*.json` : configuration runtime

Points notables :

- Filtre global de validation (`ValidationActionFilter`) pour uniformiser les erreurs de model binding
- Gestion globale des exceptions via `UseExceptionHandler`
- Normalisation de certains status HTTP (`UseStatusCodePages`)
- Seed de donnees execute au demarrage (idempotent) via `DbSeeder`

### 2) `Payzen.Application`

Contient :

- `Interfaces/` : contrats de services (ex: `IAuthService`, `IEmployeeService`, `IPayrollService`, etc.)
- `DTOs/` : objets de transport entree/sortie API
- `Validators/` : validations FluentValidation
- `Payroll/` : moteur de calcul de paie et objets associes
- `Common/` : objets transverses (`ServiceResult`, `PagedResult`, etc.)

Role :

- Stabiliser les contrats entre API et Infrastructure
- Eviter que les controllers dependent de details de persistence

### 3) `Payzen.Domain`

Contient :

- `Entities/` : modeles metier persistables (Auth, Company, Employee, Leave, Payroll, Referentiel, Events)
- `Enums/` : enumerations du domaine
- `Common/` : base entities/shared metadata (selon implementation du dossier)

Role :

- Porter le modele metier central, independant des details HTTP
- Favoriser un langage commun entre couches

### 4) `Payzen.Infrastructure`

Contient :

- `Persistence/AppDbContext.cs` : DbContext EF Core
- `Persistence/Configurations/` : configurations Fluent API par aggregate
- `Migrations/` : historique schema base de donnees
- `Services/` : implementations concretes des interfaces Application
- `DependencyInjection.cs` : enregistrement central DI de l'infrastructure
- `Seeding/DbSeeder.cs` : seed initial et/ou technique

Role :

- Relier le modele metier au monde reel (SQL Server, JWT, fichiers, APIs externes)

## Flux d'une requete HTTP

Flux type :

1. Le client appelle un endpoint dans un controller de `Payzen.Api`.
2. ASP.NET Core execute middleware + auth + validation modele.
3. Le controller mappe la requete sur un DTO Application.
4. Le controller invoque une interface (`Payzen.Application.Interfaces`).
5. L'implementation concrete (dans `Payzen.Infrastructure.Services`) execute la logique:
   - lecture/ecriture DB via `AppDbContext`
   - calcul metier
   - integration externe si necessaire
6. Le service renvoie un DTO/resultat.
7. Le controller renvoie la reponse HTTP normalisee.

Cette separation simplifie :

- les tests unitaires (mock des interfaces),
- la maintenance (surface d'impact limitee),
- l'evolution (ajout de services sans coupler au transport HTTP).

## Authentification et authorization

Le projet expose une configuration mixte autour de :

- JWT Bearer (API stateless)
- OpenID Connect / Entra External ID (auth federee)
- Cookie/Session (configures dans le pipeline)

Parametres de token JWT :

- Issuer, Audience, SigningKey depuis `JwtSettings`
- validation stricte de l'expiration (`ClockSkew = 0`)
- claims principaux maps (`unique_name`, `role`)

Autorisation :

- politiques declarees dans la configuration ASP.NET Core (ex: `AdminOnly`, `ActiveUser`)
- controle d'acces fin par endpoint selon attributs et logique service

## Persistance et base de donnees

Base :

- SQL Server (souvent `localdb` en local)
- EF Core avec migrations dans `Payzen.Infrastructure`

`AppDbContext` :

- Expose les `DbSet<>` pour les domaines Auth, Company, Employee, Leave, Payroll, Referentiel et Events
- Applique automatiquement toutes les configurations EF (`ApplyConfigurationsFromAssembly`)
- Override `SaveChangesAsync` pour gerer les timestamps d'audit (`CreatedAt`, `UpdatedAt`)
- Ajoute un filtre global soft-delete sur les entites possedant `DeletedAt`

## Validation, erreurs et conventions API

Validation :

- FluentValidation present dans `Payzen.Application.Validators`
- validation modele uniforme via filtre global dans `Payzen.Api`
- reponses `400` structurees avec details des champs en erreur

Gestion d'erreurs :

- middleware d'exception global renvoyant un payload JSON coherent
- logs serveur sur exceptions non gerees
- details d'exception exposes en environnement de developpement uniquement

Convention JSON :

- noms de proprietes preserves (pas de renommage camelCase force)
- deserialisation case-insensitive activee
- enums serialisees en string

## Configuration et secrets

Fichiers principaux :

- `Payzen.Api/appsettings.json`
- `Payzen.Api/appsettings.Development.json`
- User Secrets (ID defini dans le `.csproj` API)

Sections de configuration notables :

- `ConnectionStrings:DefaultConnection`
- `JwtSettings`
- `EntraExternalId` / `EntraNativeAuth`
- `AzureCommunication`
- `IronPdf`

Important :

- Ne pas versionner des secrets reels (cles JWT, client secrets, connection strings sensibles, licences).
- Preferer les variables d'environnement, User Secrets (`dotnet user-secrets`) ou un coffre de secrets.

## Demarrage local

Prerequis :

- .NET SDK 10
- SQL Server LocalDB (ou instance SQL Server accessible)

Etapes :

1. Se placer dans le dossier backend
2. Restaurer les packages
3. Appliquer les migrations (si necessaire)
4. Lancer l'API

Commandes :

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project Payzen.Api
```

## Migrations EF Core

Depuis `backend/` :

Creer une migration :

```bash
dotnet ef migrations add NomMigration \
  --project Payzen.Infrastructure \
  --startup-project Payzen.Api
```

Appliquer les migrations :

```bash
dotnet ef database update \
  --project Payzen.Infrastructure \
  --startup-project Payzen.Api
```

## Tests

Le projet de tests est `Payzen.Tests` et contient :

- tests unitaires (ex: moteur de paie, utilitaires, objets de resultat)
- tests d'integration (ex: services Auth/Employee/Leave avec DB InMemory)

Execution :

```bash
cd backend
dotnet test
```

Packages de test utilises :

- xUnit
- Moq
- FluentAssertions
- EF Core InMemory
- Coverlet collector (couverture)

## Bonnes pratiques de maintenance

- Ajouter une interface dans `Payzen.Application.Interfaces` avant l'implementation Infrastructure.
- Garder les controllers fins : orchestration HTTP uniquement.
- Centraliser les acces DB dans les services Infrastructure.
- Ajouter/mettre a jour validators et DTOs en meme temps que les endpoints.
- Couvrir chaque nouveau service critique par des tests unitaires/integration.
- Eviter d'introduire des dependances techniques dans `Payzen.Domain` quand possible.

## Notes d'evolution possibles

- Renforcer la strategie de secrets (suppression des secrets en clair du repository)
- Clarifier la strategie definitive d'auth (JWT only vs parcours federes hybrides)
- Ajouter une documentation OpenAPI/Swagger formelle pour l'integration front/externe
- Documenter les conventions de versioning API (breaking/non-breaking changes)