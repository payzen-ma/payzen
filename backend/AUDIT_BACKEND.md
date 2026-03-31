# Audit Backend - Payzen

Date: 2026-03-30
Scope: `backend/` (API, Application, Infrastructure, Domain, tests d'integration)
Type: audit statique du code (sans execution de tests dans ce rapport)

## Points positifs

- Architecture en couches claire (`Payzen.Api`, `Payzen.Application`, `Payzen.Infrastructure`, `Payzen.Domain`) avec injection de dependances centralisee via `AddInfrastructure`.
- Bonne base de securite API:
  - authentification JWT configuree (validation issuer/audience/signature/lifetime, `ClockSkew = 0`),
  - endpoints majoritairement proteges par `[Authorize]`,
  - soft-delete coherent via `DeletedAt` et filtres globaux EF.
- Modelisation riche et metier detaillee (RH, conges, paie, referentiels) avec `IEntityTypeConfiguration` et conventions EF propres.
- Validation cote serveur presente (FluentValidation + filtre de validation global) et erreurs JSON homogenes.
- Seeder idempotent sur beaucoup d'entites de reference, ce qui facilite les environnements de dev/recette.
- Presence de tests d'integration sur des domaines critiques (auth, employee, leave), ce qui est un bon socle de non-regression.

## Points negatifs

### Critiques (a traiter en priorite)

- Configuration d'authentification dupliquee dans `Program.cs`:
  - un premier `AddAuthentication` (cookie/OIDC) puis un second `AddAuthentication` (JWT) ecrase le precedent.
  - Impact: comportement potentiellement incoherent et difficile a maintenir selon les flux.
- Logique de privilege "Admin Payzen" basee sur une allowlist hardcodee dans `AuthService`:
  - domaines et emails sensibles sont en dur dans le code.
  - Impact: risque de securite, faible tracabilite, difficulte d'operation (rotation/changement).
- Logging de donnees d'auth via `Console.WriteLine` dans `AuthService`:
  - traces de flux login et emails en clair.
  - Impact: fuite d'information potentielle et non-conformite observabilite (pas de niveau ni correlation standard).

### Importants

- Seeder cree un compte admin par defaut (`admin@payzen.ma`) si absent:
  - pratique en dev, mais risque fort si non controle en prod.
- Controle de suppression utilisateur probablement incorrect:
  - dans `DeleteUserAsync`, condition `if(user.Id == id)` est toujours vraie apres chargement de `user` par `id`.
  - Impact: suppression toujours bloquee avec un message "propre compte" (bug fonctionnel).
- Cohabitation de conventions heterogenes:
  - champs et naming mixtes (`isActive` vs `IsActive`, anglais/francais, typos type `RepalceRolesForUserAsync`).
  - Impact: dette de maintainability et hausse du risque d'erreur.
- Commentaires "dev/prod" dans le code sans garde technique reelle:
  - certaines regles semblent reposer sur discipline humaine plutot que config d'environnement.

### Modere / Qualite

- Beaucoup de logique metier lourde concentree dans des services volumineux (`CompanyService`, `AuthService`), ce qui rend les revues et evolutions plus couteuses.
- Traces de code incomplet ou technique:
  - TODO en prod (`EmailService`, `PayrollCalculationEngine`),
  - blocs commentes de logs metier non implementes.
- Tests d'integration relies majoritairement a `UseInMemoryDatabase`:
  - utile pour la vitesse, mais ecart potentiel avec SQL Server reel (comportements EF/SQL non couverts).

## Recommandations prioritaires (court terme)

1. Unifier la strategie d'auth dans `Program.cs` (un seul `AddAuthentication`) et expliciter le scenario cible (JWT only ou hybride correctement mappe).
2. Externaliser toute allowlist sensible (emails/domaines) vers configuration securisee + validation stricte par environnement.
3. Remplacer `Console.WriteLine` par `ILogger<T>` avec niveaux, masking des donnees sensibles, et correlation.
4. Corriger `DeleteUserAsync` pour comparer l'utilisateur courant (claim/token) et non l'id cible a lui-meme.
5. Proteger la creation de compte admin seed (feature flag/env guard) pour eviter tout bootstrap dangereux en production.

## Recommandations moyen terme

- Extraire les services tres volumineux en sous-services orientes cas d'usage.
- Harmoniser conventions de nommage (pascalCase, orthographe, langue des symboles) et ajouter des regles analyzers.
- Renforcer les tests avec une couche SQL Server reelle (container/localdb) pour completer les tests in-memory.

