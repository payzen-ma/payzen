# PayZen — Système d'Authentification via Microsoft Entra External ID

> Document de référence technique — Architecture, configuration et implémentation complète

---

## Table des matières

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [Configuration Azure / Entra (Step-by-step)](#3-configuration-azure--entra-step-by-step)
4. [Modèle de Domaine](#4-modèle-de-domaine)
5. [Flux d'Authentification](#5-flux-dauthentification)
6. [Flux d'Invitation](#6-flux-dinvitation)
7. [Provisioning Utilisateur](#7-provisioning-utilisateur)
8. [API Design](#8-api-design)
9. [Frontend Angular](#9-frontend-angular)
10. [Considérations de Sécurité](#10-considérations-de-sécurité)
11. [Notes de Développement Local](#11-notes-de-développement-local)
12. [Guide de Production](#12-guide-de-production)
13. [Pièges Courants](#13-pièges-courants)

---

## 1. Overview

### Stratégie d'authentification

PayZen délègue **intégralement** l'authentification à **Microsoft Entra External ID** (anciennement Azure AD B2C / CIAM). Aucun mot de passe n'est stocké, géré ou transmis par l'application PayZen.

### Principes clés

| Principe | Détail |
|----------|--------|
| **Zéro mot de passe côté PayZen** | Le champ `PasswordHash` est `null` pour les comptes Entra. Le backend ne reçoit jamais de mot de passe. |
| **OIDC + PKCE** | Le frontend utilise MSAL.js v3 avec Authorization Code Flow + PKCE. |
| **JWT interne** | Après authentification Entra, le backend émet un JWT PayZen signé (HMAC-SHA256) contenant rôles et permissions. |
| **Pas de B2B** | Uniquement des identités consommateur (External ID / CIAM). |
| **Invitation par token** | Les utilisateurs sont invités par lien. Le premier login crée le compte Entra (mot de passe géré par Microsoft). |

### Flux résumé

```
Landing page → "Se connecter" / "S'inscrire"
       ↓
Angular /login (auto-redirect, pas d'UI visible)
       ↓
MSAL (PKCE) → Entra External ID (page brandée : Microsoft, Google, email/mdp)
       ↓
Entra → /auth/callback (id_token avec oid + email)
       ↓
Frontend → POST /api/auth/entra-login {email, externalId}
       ↓
Backend vérifie email + oid → émet JWT PayZen
       ↓
Frontend stocke JWT → accès API protégées
```

---

## 2. Architecture

### Diagramme de haut niveau

```mermaid
graph TB
    subgraph "Client (Browser)"
        FE[Frontend Angular<br/>localhost:4200]
        MSAL[MSAL.js v3<br/>@azure/msal-browser]
    end

    subgraph "Microsoft Cloud"
        ENTRA[Microsoft Entra<br/>External ID<br/>payzenhr.ciamlogin.com]
    end

    subgraph "Backend"
        API[ASP.NET Core API<br/>localhost:5119]
        JWT[JwtService<br/>HMAC-SHA256]
        DB[(SQL Server<br/>EF Core)]
    end

    subgraph "Backoffice"
        BO[Backoffice Admin<br/>localhost:50171]
    end

    FE -->|1. loginRedirect PKCE| MSAL
    MSAL -->|2. Authorization Code| ENTRA
    ENTRA -->|3. id_token + code| MSAL
    MSAL -->|4. email + oid| FE
    FE -->|5. POST /entra-login| API
    API -->|6. Lookup/Create user| DB
    API -->|7. Generate JWT| JWT
    JWT -->|8. JWT PayZen| FE
    FE -->|9. Bearer JWT| API
    BO -->|POST /invitations/invite-admin| API
    API -->|Email simulé (log)| API
```

### Composants

| Composant | Technologie | Rôle |
|-----------|------------|------|
| **Frontend** | Angular 19+ standalone, MSAL.js v3, PrimeNG | Initie le flux OIDC via MSAL, gère les tokens, route guards |
| **Backend API** | ASP.NET Core (.NET 9+), EF Core, SQL Server | Valide l'identité Entra, émet JWT interne, expose l'API REST |
| **Entra External ID** | Microsoft CIAM (payzenhr.ciamlogin.com) | Gère les identités, mots de passe, MFA, sign-up/sign-in |
| **Backoffice** | Angular 19+ standalone | Crée entreprises et invitations (admin PayZen) |

---

## 3. Configuration Azure / Entra (Step-by-step)

### 3.1 Créer un compte Azure

1. Aller sur [https://portal.azure.com](https://portal.azure.com)
2. Créer un compte avec un email professionnel ou personnel
3. Un tenant Azure AD par défaut est créé automatiquement

### 3.2 Créer un tenant External ID (CIAM)

1. Dans le portail Azure, chercher **"Microsoft Entra External ID"**
2. Cliquer **"Create a new tenant"**
3. Choisir **"Customer identity experiences"** (pas Workforce)
4. Remplir :
   - **Organization name** : `PayZen HR`
   - **Domain name** : `payzenhr` → génère `payzenhr.onmicrosoft.com`
   - **Location** : Europe (ou votre région)
5. Cliquer **Create**

> **Authority résultante** : `https://payzenhr.ciamlogin.com/payzenhr.onmicrosoft.com`

### 3.3 App Registration — SPA (Frontend)

1. Dans le tenant External ID, aller à **"App registrations"** → **"New registration"**
2. Remplir :
   - **Name** : `PayZen SPA`
   - **Supported account types** : "Accounts in this organizational directory only"
   - **Redirect URI** : Platform = **Single-page application (SPA)**

3. Ajouter les **Redirect URIs** (SPA) pour le frontend **et** le backoffice (même `Client ID`, plusieurs origines) :

```
http://localhost:4200/auth/callback
http://localhost:50171/auth/callback
```

4. Dans **"Authentication"** :
   - Cocher **"ID tokens"** (implicit grant — nécessaire pour MSAL redirect)
   - Cocher **"Access tokens"** si vous utilisez des scopes API
   - **Implicit grant** : Non requis (on utilise Auth Code + PKCE)
   - **Front-channel logout URL** : ajouter les deux URL (Azure accepte une liste selon la version du portail) :
     - `http://localhost:4200/login`
     - `http://localhost:50171/login`

5. Récupérer le **Client ID** (Application ID) :

```
0bd1e09a-2b59-4b49-a802-a91f70851e38
```

> PKCE est automatiquement géré par MSAL.js pour les SPA — aucun client secret n'est nécessaire côté frontend.

### 3.4 App Registration — API (Backend)

1. Créer une seconde App Registration :
   - **Name** : `PayZen API`
   - **Supported account types** : même tenant

2. Dans **"Expose an API"** :
   - Définir un **Application ID URI** : `api://payzen-api`
   - Ajouter un scope : `api://payzen-api/access_as_user`

3. Dans **"Certificates & secrets"** :
   - Créer un **Client secret** (pour le backend OIDC server-side si nécessaire)
   - Le noter pour `appsettings.json`

### 3.5 Configuration des scopes

Dans l'app `PayZen SPA`, aller à **"API permissions"** :

1. Cliquer **"Add a permission"** → **"My APIs"** → `PayZen API`
2. Sélectionner le scope `access_as_user`
3. Cliquer **"Grant admin consent"**

Scopes OIDC utilisés par MSAL :

```json
["openid", "profile", "email"]
```

### 3.6 User Flows (Sign-up / Sign-in)

1. Dans le tenant External ID, aller à **"User flows"**
2. Créer un flow **"Sign up and sign in"** :
   - **Name** : `B2C_1_signupsignin`
   - **Identity providers** : Email with password
   - Optionnel : Google, Microsoft Personal
3. **User attributes to collect** (sign-up) :
   - Email Address (requis)
   - Display Name
4. **Token claims to return** :
   - `email`
   - `oid` (Object ID)
   - `given_name`, `family_name`

### 3.7 Création du mot de passe au premier login

Lorsqu'un utilisateur clique sur un lien d'invitation PayZen :

1. Il est redirigé vers Entra External ID via MSAL
2. **S'il n'a pas de compte Entra** : le flow "Sign Up" lui demande de créer un mot de passe (géré entièrement par Microsoft)
3. **S'il a déjà un compte** : il se connecte normalement
4. Le mot de passe est stocké et validé **exclusivement par Entra** — PayZen n'en a jamais connaissance

> Le champ `PasswordHash` dans la table `Users` est `null` pour tous les comptes Entra (source = "entra").

---

## 4. Modèle de Domaine

### Diagramme entité-relation

```mermaid
erDiagram
    Company ||--o{ Employee : "emploie"
    Employee ||--o| Users : "a un compte"
    Users ||--o{ UsersRoles : "possède"
    Roles ||--o{ UsersRoles : "assigné à"
    Roles ||--o{ RolesPermissions : "contient"
    Permissions ||--o{ RolesPermissions : "accordée via"
    Company ||--o{ Invitation : "associée à"
    Roles ||--o{ Invitation : "rôle assigné"
    Employee ||--o| Invitation : "optionnellement lié"

    Company {
        int Id PK
        string CompanyName
        string AuthType "C = Entra"
        bool isActive
        bool IsCabinetExpert
    }

    Employee {
        int Id PK
        int CompanyId FK
        string FirstName
        string LastName
        string Email
        int CategoryId
    }

    Users {
        int Id PK
        int EmployeeId FK "nullable"
        string Username
        string Email
        string PasswordHash "null pour Entra"
        string ExternalId "oid Entra"
        string Source "entra | visitor_entra"
        bool IsActive
    }

    Roles {
        int Id PK
        string Name
        string Description
    }

    UsersRoles {
        int Id PK
        int UserId FK
        int RoleId FK
    }

    Permissions {
        int Id PK
        string Name
        string Description
        string Resource
        string Action
    }

    RolesPermissions {
        int Id PK
        int RoleId FK
        int PermissionId FK
    }

    Invitation {
        int Id PK
        string Token "GUID 32 chars"
        string Email
        int CompanyId FK
        int RoleId FK
        int EmployeeId FK "nullable"
        InvitationStatus Status "Pending|Accepted|Expired"
        DateTime ExpiresAt
    }
```

### Entité `Users` (C#)

```csharp
public class Users : BaseEntity
{
    public int? EmployeeId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public string? EmailPersonal { get; set; }
    public string? ExternalId { get; set; }   // oid Entra
    public string? Source { get; set; }        // "entra" | "visitor_entra"
    public string? PasswordHash { get; set; }  // null pour comptes Entra
    public bool IsActive { get; set; } = true;

    public Employee.Employee? Employee { get; set; }
    public ICollection<UsersRoles>? UsersRoles { get; set; }
}
```

### Entité `Invitation` (C#)

```csharp
public class Invitation : BaseEntity
{
    public required string Token { get; set; }
    public required string Email { get; set; }
    public int CompanyId { get; set; }
    public int RoleId { get; set; }
    public int? EmployeeId { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime ExpiresAt { get; set; }

    public Company.Company? Company { get; set; }
    public Auth.Roles? Role { get; set; }
}

public enum InvitationStatus
{
    Pending,
    Accepted,
    Expired
}
```

### Rôles applicatifs

| Rôle | Code | Description |
|------|------|-------------|
| Admin Société | `admin` | Administration complète de l'entreprise |
| RH | `rh` | Gestion des employés et de la paie |
| Manager | `manager` | Supervision d'équipe |
| Employé | `employee` | Accès à son propre profil |
| Cabinet Comptable | `cabinet` | Mode expert multi-entreprise |
| Admin PayZen | `admin_payzen` | Super-administrateur plateforme |

---

## 5. Flux d'Authentification

### Diagramme de séquence — Login standard

```mermaid
sequenceDiagram
    actor U as Utilisateur
    participant FE as Frontend Angular
    participant MSAL as MSAL.js
    participant ENTRA as Entra External ID
    participant API as Backend API
    participant DB as SQL Server

    U->>FE: Clic "Se connecter" (landing page)
    Note over FE: /login auto-redirige (pas d'UI visible)
    FE->>MSAL: loginRedirect({scopes, prompt})
    Note over MSAL: Génère code_verifier + code_challenge (PKCE)
    MSAL->>ENTRA: GET /authorize (code_challenge, redirect_uri)
    ENTRA->>U: Page de login Microsoft
    U->>ENTRA: Email + Mot de passe (géré par Entra)
    ENTRA->>MSAL: Redirect → /auth/callback?code=xxx
    Note over MSAL: Échange code + code_verifier
    MSAL->>ENTRA: POST /token (code, code_verifier)
    ENTRA->>MSAL: id_token + access_token
    MSAL->>FE: AuthenticationResult {account, idTokenClaims}
    FE->>FE: Extrait email + oid depuis idTokenClaims
    FE->>API: POST /api/auth/entra-login {email, externalId}
    API->>DB: SELECT * FROM Users WHERE Email = @email
    alt Utilisateur existe
        API->>DB: UPDATE ExternalId, Source, IsActive
    else Nouvel utilisateur
        API->>DB: INSERT Users (PasswordHash = null, Source = "entra")
        API->>DB: INSERT UsersRoles (Employee role)
    end
    API->>API: GenerateTokenAsync(user) → JWT PayZen
    API->>FE: 200 OK {token, user: {id, email, roles, permissions...}}
    FE->>FE: Stocke JWT dans localStorage
    FE->>FE: Redirige vers /dashboard ou /select-context
```

### Composant Callback (`EntraCallbackComponent`)

Le composant `/auth/callback` est le point de retour après l'authentification Entra :

```typescript
@Component({
  selector: 'app-entra-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="callback-container">
      <div class="spinner"></div>
      <p>Authentification en cours...</p>
    </div>
  `
})
export class EntraCallbackComponent implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  private entraService = inject(EntraRedirectService);
  private invitationService = inject(InvitationService);

  async ngOnInit() {
    const result = await this.entraService.handleRedirectPromise();

    if (!result) {
      this.router.navigate(['/login']);
      return;
    }

    const account = result.account;
    const email = account.username || account.idTokenClaims?.['email'] as string;
    const oid = account.idTokenClaims?.['oid'] as string;

    if (!email || !oid) {
      this.router.navigate(['/login'], {
        queryParams: { error: 'missing_claims' }
      });
      return;
    }

    // Cas invitation : token présent dans sessionStorage
    const invitationToken = sessionStorage.getItem('pending_invitation_token');

    if (invitationToken) {
      sessionStorage.removeItem('pending_invitation_token');
      this.handleInvitationFlow(invitationToken, email, oid);
      return;
    }

    // Cas connexion normale
    this.handleNormalLogin(email, oid);
  }

  private handleNormalLogin(email: string, oid: string): void {
    this.authService.loginWithEntra(email, oid).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        this.router.navigate(['/login'], {
          queryParams: { error: 'auth_failed' }
        });
      }
    });
  }

  private handleInvitationFlow(token: string, email: string, oid: string): void {
    this.authService.loginWithEntra(email, oid).pipe(
      switchMap(() => this.invitationService.acceptViaIdp(token))
    ).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        if (err?.error?.Code === 'EMAIL_MISMATCH') {
          this.router.navigate(['/auth/accept-invite'], {
            queryParams: {
              token,
              error: 'email_mismatch',
              message: err.error.Message
            }
          });
        } else {
          this.router.navigate(['/login'], {
            queryParams: { error: 'invite_failed' }
          });
        }
      }
    });
  }
}
```

### Endpoint Backend `entra-login`

```csharp
[HttpPost("entra-login")]
[AllowAnonymous]
[Produces("application/json")]
public async Task<ActionResult> EntraLogin([FromBody] EntraLoginRequestDto dto, CancellationToken ct)
{
    if (!ModelState.IsValid) return BadRequest(ModelState);
    var result = await _auth.LoginWithEntraAsync(dto, ct);
    return result.Success ? Ok(result.Data) : Unauthorized(new { Message = result.Error });
}
```

Le service `LoginWithEntraAsync` :

1. Recherche un `Employee` avec le même email
2. Vérifie que la company a `AuthType = "C"` (Entra)
3. Crée ou met à jour l'utilisateur (`PasswordHash = null`, `Source = "entra"`)
4. Assigne un rôle par défaut (Employee)
5. Génère un JWT PayZen signé

---

## 6. Flux d'Invitation

### Diagramme de séquence — Invitation complète

```mermaid
sequenceDiagram
    actor BO as Admin Backoffice
    participant API as Backend API
    participant DB as SQL Server
    participant LOG as Console (email simulé)
    actor U as Utilisateur invité
    participant FE as Frontend Angular
    participant MSAL as MSAL.js
    participant ENTRA as Entra External ID

    BO->>API: POST /api/invitations/invite-admin<br/>{email, companyId, roleId}
    API->>DB: INSERT Invitation (token=GUID, status=Pending, expiresAt=+48h)
    API->>LOG: [EMAIL SIMULÉ] Lien d'invitation loggé en console
    Note over LOG: http://localhost:4200/auth/accept-invite?token=abc123...

    U->>FE: Clic sur le lien d'invitation
    FE->>API: GET /api/invitations/validate?token=abc123
    API->>DB: SELECT Invitation WHERE token AND status=Pending AND expiresAt > now
    API->>FE: 200 {companyName, roleName, maskedEmail, expiresAt}
    FE->>FE: Affiche page d'acceptation

    U->>FE: Clic "Accepter avec Microsoft"
    FE->>FE: sessionStorage.set('pending_invitation_token', token)
    FE->>MSAL: loginRedirect (PKCE)
    MSAL->>ENTRA: /authorize
    ENTRA->>U: Sign-up / Sign-in (mot de passe créé par l'utilisateur sur Entra)
    ENTRA->>MSAL: Redirect → /auth/callback
    MSAL->>FE: AuthenticationResult

    FE->>API: POST /api/auth/entra-login {email, oid}
    API->>FE: JWT PayZen

    FE->>API: POST /api/invitations/accept-via-idp {token}<br/>+ Bearer JWT
    API->>API: Vérifie email JWT == email invitation
    API->>DB: UPDATE Invitation SET status=Accepted
    API->>DB: UPDATE Users SET employeeId = invitation.employeeId
    API->>DB: INSERT UsersRoles (invitation.roleId)
    API->>FE: 200 "Invitation acceptée"

    FE->>FE: Redirige vers /dashboard
```

### Création d'invitation (Backend)

```csharp
public async Task<string> CreateInvitationAsync(InviteAdminDto dto, CancellationToken ct)
{
    var token = Guid.NewGuid().ToString("N"); // 32 caractères hex

    var invitation = new Invitation
    {
        Token = token,
        Email = dto.Email.ToLowerInvariant(),
        CompanyId = dto.CompanyId,
        RoleId = dto.RoleId,
        EmployeeId = null,
        Status = InvitationStatus.Pending,
        ExpiresAt = DateTime.UtcNow.AddHours(48),
        CreatedAt = DateTime.UtcNow
    };

    _db.Invitations.Add(invitation);
    await _db.SaveChangesAsync(ct);

    // Email envoyé via IEmailService (simulé en dev)
    await _emailService.SendInvitationEmailAsync(
        dto.Email, company ?? "Payzen", role ?? "Admin", token, ct);

    return token;
}
```

### Simulation d'email (Développement local)

Aucun serveur SMTP n'est configuré en développement. L'implémentation de `IEmailService` logue le lien en console :

```csharp
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
        => _logger = logger;

    public Task SendInvitationEmailAsync(
        string email, string companyName, string role, string token, CancellationToken ct)
    {
        var link = $"http://localhost:4200/auth/accept-invite?token={token}";

        _logger.LogInformation(
            "======== EMAIL D'INVITATION (SIMULÉ) ========\n" +
            "  Destinataire : {Email}\n" +
            "  Entreprise   : {Company}\n" +
            "  Rôle         : {Role}\n" +
            "  Lien         : {Link}\n" +
            "  Expire dans  : 48h\n" +
            "==============================================",
            email, companyName, role, link);

        return Task.CompletedTask;
    }
}
```

Pour utiliser le lien en développement :

1. Lancer le backend : `dotnet run` (port 5119)
2. Créer une invitation via Backoffice ou API
3. **Copier le lien** depuis la sortie console du backend
4. Ouvrir le lien dans un navigateur

### Acceptation côté Frontend

```typescript
@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './accept-invite.component.html',
  styleUrls: ['./accept-invite.component.css']
})
export class AcceptInviteComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private invitationService = inject(InvitationService);
  private entraService = inject(EntraRedirectService);

  invitation = signal<InvitationInfo | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  token = '';

  ngOnInit() {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';

    if (!this.token) {
      this.error.set('Token d\'invitation manquant.');
      this.isLoading.set(false);
      return;
    }

    this.invitationService.validate(this.token).subscribe({
      next: (info) => {
        this.invitation.set(info);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Invitation invalide ou expirée.');
        this.isLoading.set(false);
      }
    });
  }

  acceptWithMicrosoft(): void {
    sessionStorage.setItem('pending_invitation_token', this.token);
    this.entraService.loginWithMicrosoft();
  }

  acceptWithGoogle(): void {
    sessionStorage.setItem('pending_invitation_token', this.token);
    this.entraService.loginWithGoogle();
  }
}
```

---

## 7. Provisioning Utilisateur

### Stratégie de correspondance

Le provisioning repose sur la **correspondance par email** entre l'identité Entra et l'employé PayZen.

```mermaid
flowchart TD
    A[POST /api/auth/entra-login<br/>email + externalId] --> B{User existe<br/>dans DB ?}
    B -->|Oui| C[Mise à jour ExternalId + Source]
    B -->|Non| D[Créer User<br/>PasswordHash = null<br/>Source = entra]
    C --> E{Employee existe<br/>avec même email ?}
    D --> E
    E -->|Oui + Company.AuthType = C| F[Lier User ↔ Employee<br/>Rôle = Employee]
    E -->|Non| G[Compte visiteur<br/>Source = visitor_entra]
    F --> H[Générer JWT PayZen]
    G --> H
```

### Administrateur Payzen (backoffice)

En base, le rôle plateforme est nommé exactement **`Admin Payzen`** (voir seed `DbSeeder`). Si l’utilisateur qui appelle `POST /api/auth/entra-login` existe déjà et possède ce rôle, le backend **ne force pas** l’assignation Employee/Visitor : les rôles existants sont conservés, `ExternalId` est mis à jour, et le JWT contient les claims `role` attendus pour `[Authorize(Roles = "Admin Payzen")]` (ex. `POST /api/invitations/invite-admin`).

Pour le développement, le compte seed utilise l’email **`admin@payzen.ma`** : l’administrateur doit se connecter sur Entra avec **le même email** (ou mettre à jour l’email en base pour qu’il corresponde à son compte Entra).

### Code de provisioning (Backend)

```csharp
// Extrait de AuthService.LoginWithEntraAsync
var email = dto.Email.Trim();
var externalId = dto.ExternalId.Trim();

// 1) Chercher un employé avec cet email
var employee = await _db.Employees
    .Include(e => e.Company)
    .FirstOrDefaultAsync(e => e.Email == email && e.DeletedAt == null, ct);

var isEmployeeEligible =
    employee != null &&
    employee.Company != null &&
    string.Equals(employee.Company.AuthType, "C", StringComparison.OrdinalIgnoreCase);

// 2) Chercher ou créer l'utilisateur
var user = await _db.Users
    .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null, ct);

if (user == null)
{
    user = new Users
    {
        Username = GenerateUsername(email),
        Email = email,
        PasswordHash = null,  // Entra : pas de mot de passe stocké
        IsActive = true,
        EmployeeId = isEmployeeEligible ? employee!.Id : null,
        ExternalId = externalId,
        Source = isEmployeeEligible ? "entra" : "visitor_entra",
        CreatedBy = 1
    };
    _db.Users.Add(user);
    await _db.SaveChangesAsync(ct);
}
else
{
    user.ExternalId = externalId;
    user.Source = isEmployeeEligible ? "entra" : "visitor_entra";
    user.EmployeeId = isEmployeeEligible ? employee.Id : null;
    user.PasswordHash = null; // purge éventuelle
    user.IsActive = true;
    await _db.SaveChangesAsync(ct);
}
```

### Champs clés pour le provisioning

| Champ | Description |
|-------|-------------|
| `ExternalId` | Object ID (`oid`) provenant d'Entra — identifiant unique et stable |
| `Source` | `"entra"` pour les employés rattachés, `"visitor_entra"` pour les visiteurs |
| `PasswordHash` | Toujours `null` pour les comptes Entra |
| `EmployeeId` | Lien vers la fiche employé (si l'email correspond) |

---

## 8. API Design

### Vue d'ensemble des endpoints

| Méthode | Route | Auth | Description |
|---------|-------|------|-------------|
| `POST` | `/api/auth/entra-login` | Anonymous | Échange identité Entra → JWT PayZen |
| `GET` | `/api/auth/me` | Bearer | Profil de l'utilisateur connecté |
| `POST` | `/api/auth/logout` | — | Invalidation côté client |
| `GET` | `/api/invitations/validate` | Anonymous | Valide un token d'invitation |
| `POST` | `/api/invitations/accept-via-idp` | Bearer | Accepte une invitation après auth Entra |
| `POST` | `/api/invitations/invite-admin` | Bearer (rôle **Admin Payzen** en base, claim JWT `role`) | Invite un administrateur d'entreprise |
| `POST` | `/api/invitations/invite-employee` | Bearer (Admin/RH) | Invite un employé |

### `POST /api/auth/entra-login`

**Request :**

```http
POST /api/auth/entra-login HTTP/1.1
Host: localhost:5119
Content-Type: application/json

{
  "email": "user@example.com",
  "externalId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Response 200 :**

```json
{
  "Message": "Connexion réussie.",
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "ExpiresAt": "2026-03-24T16:00:00Z",
  "User": {
    "Id": 42,
    "Email": "user@example.com",
    "Username": "user",
    "FirstName": "Ahmed",
    "LastName": "Benali",
    "Roles": ["Employee"],
    "Permissions": ["VIEW_OWN_PAYSLIPS", "REQUEST_LEAVE"],
    "IsCabinetExpert": false,
    "EmployeeCategoryId": 1,
    "Mode": "Presence",
    "companyId": 5
  }
}
```

**Response 401 :**

```json
{
  "Message": "Votre entreprise est désactivée. Contactez l'administrateur."
}
```

### `GET /api/auth/me`

**Request :**

```http
GET /api/auth/me HTTP/1.1
Host: localhost:5119
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Response 200 :**

```json
{
  "Id": 42,
  "Username": "user",
  "Email": "user@example.com",
  "IsActive": true,
  "CreatedAt": "2026-03-20T10:30:00"
}
```

### `GET /api/invitations/validate`

**Request :**

```http
GET /api/invitations/validate?token=abc123def456 HTTP/1.1
Host: localhost:5119
```

**Response 200 :**

```json
{
  "companyName": "Acme Corp",
  "roleName": "Admin",
  "maskedEmail": "a***i@example.com",
  "expiresAt": "2026-03-26T14:00:00Z"
}
```

**Response 404 :**

```json
{
  "Message": "Invitation invalide ou expirée."
}
```

### `POST /api/invitations/accept-via-idp`

**Request :**

```http
POST /api/invitations/accept-via-idp HTTP/1.1
Host: localhost:5119
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "token": "abc123def456"
}
```

**Response 200 :**

```json
{
  "Message": "Invitation acceptée avec succès."
}
```

**Response 400 (email mismatch) :**

```json
{
  "Message": "L'email de votre compte (other@mail.com) ne correspond pas à l'invitation (a***i@example.com).",
  "Code": "EMAIL_MISMATCH",
  "ExpectedEmail": "a***i@example.com",
  "ReceivedEmail": "other@mail.com"
}
```

### `POST /api/invitations/invite-admin`

**Request :**

```http
POST /api/invitations/invite-admin HTTP/1.1
Host: localhost:5119
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "email": "admin@newcompany.com",
  "companyId": 12,
  "roleId": 1
}
```

**Response 200 :**

```json
{
  "Message": "Invitation envoyée avec succès.",
  "Token": "a8b7c6d5e4f3a2b1c0d9e8f7a6b5c4d3"
}
```

### `POST /api/invitations/invite-employee`

**Request :**

```http
POST /api/invitations/invite-employee HTTP/1.1
Host: localhost:5119
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "email": "employee@company.com",
  "companyId": 12,
  "roleId": 4,
  "employeeId": 99
}
```

**Response 200 :**

```json
{
  "Message": "Invitation envoyée avec succès.",
  "Token": "f1e2d3c4b5a6f7e8d9c0b1a2f3e4d5c6"
}
```

---

## 9. Frontend Angular

### 9.1 Configuration MSAL

Le fichier `msal.config.ts` initialise MSAL avec les paramètres Entra :

```typescript
import {
  BrowserCacheLocation,
  IPublicClientApplication,
  PublicClientApplication,
  LogLevel
} from '@azure/msal-browser';
import { environment } from '@environments/environment';

export const msalConfig = {
  auth: {
    clientId: environment.entra.clientId,
    authority: environment.entra.authority,
    redirectUri: environment.entra.redirectUri,
    postLogoutRedirectUri: environment.entra.postLogoutRedirectUri,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: BrowserCacheLocation.LocalStorage,
    storeAuthStateInCookie: false,
  },
  system: {
    allowRedirectInIframe: false,
    loggerOptions: {
      logLevel: LogLevel.Info,
      piiLoggingEnabled: false
    }
  }
};

export const msalInstance: IPublicClientApplication =
  new PublicClientApplication(msalConfig);

let msalInitializationPromise: Promise<void> | null = null;

export function initializeMsal(): Promise<void> {
  if (!msalInitializationPromise) {
    msalInitializationPromise = msalInstance.initialize();
  }
  return msalInitializationPromise;
}
```

Configuration environment :

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5119/api',
  entra: {
    clientId: '0bd1e09a-2b59-4b49-a802-a91f70851e38',
    authority: 'https://payzenhr.ciamlogin.com/payzenhr.onmicrosoft.com',
    redirectUri: 'http://localhost:4200/auth/callback',
    postLogoutRedirectUri: 'http://localhost:4200/login',
  }
};
```

MSAL est initialisé au démarrage via `APP_INITIALIZER` :

```typescript
{
  provide: APP_INITIALIZER,
  multi: true,
  useFactory: () => () => initializeMsal()
}
```

### 9.2 Flux de connexion (pas de page Login dédiée)

Il n'y a **pas de page de login visible** dans le frontend Angular. Les boutons "Se connecter" et "S'inscrire" se trouvent sur la **landing page** (site statique séparé). Le composant `/login` dans Angular est un simple pass-through qui redirige immédiatement vers Entra :

```typescript
@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="redirect-container">
      <div class="spinner"></div>
      <p>Redirection vers la connexion...</p>
    </div>
  `
})
export class LoginComponent implements OnInit {
  private entraService = inject(EntraRedirectService);
  private route = inject(ActivatedRoute);

  async ngOnInit(): Promise<void> {
    await this.entraService.loginWithEntra();
  }
}
```

La route `/login` est protégée par `guestGuard` : si l'utilisateur est déjà authentifié, il est redirigé vers le dashboard au lieu de reboucler vers Entra.

#### Flux complet depuis la landing page

1. L'utilisateur visite la landing page (site statique séparé)
2. Il clique "Se connecter" ou "S'inscrire"
3. La landing page redirige vers `http://localhost:4200/login`
4. Le `LoginComponent` s'initialise et appelle `entraService.loginWithEntra()`
5. MSAL redirige vers Entra External ID (`payzenhr.ciamlogin.com`)
6. Entra affiche la page de connexion brandée (Microsoft, Google, email/mot de passe)
7. Après authentification, Entra redirige vers `/auth/callback`
8. Le `EntraCallbackComponent` traite le retour et échange l'identité Entra contre un JWT PayZen

#### Landing page (`auth.js`)

```javascript
const APP_BASE_URL = 'http://localhost:4200';

function redirectToSignIn() {
  window.location.href = `${APP_BASE_URL}/login`;
}

function redirectToSignUp() {
  window.location.href = `${APP_BASE_URL}/login?mode=signup`;
}
```

### 9.3 Service de redirection Entra

Le service gère les interactions MSAL de façon sécurisée (protection contre les double-clics, gestion des erreurs PKCE) :

```typescript
@Injectable({ providedIn: 'root' })
export class EntraRedirectService {
  private router = inject(Router);
  private static readonly INTERACTION_KEY = 'msal.interaction.status';

  async loginWithMicrosoft(): Promise<void> {
    await this.safeLoginRedirect({
      scopes: ['openid', 'profile', 'email'],
      prompt: 'select_account',
      extraQueryParameters: {
        domain_hint: 'consumers' // comptes personnels uniquement
      }
    });
  }

  async loginWithGoogle(): Promise<void> {
    await this.safeLoginRedirect({
      scopes: ['openid', 'profile', 'email'],
      prompt: 'select_account',
      extraQueryParameters: {
        identity_provider: 'Google'
      }
    });
  }

  async handleRedirectPromise(): Promise<AuthenticationResult | null> {
    try {
      await this.ensureInitialized();
      return await msalInstance.handleRedirectPromise();
    } catch (error: any) {
      this.router.navigate(['/login'], {
        queryParams: { error: 'auth_failed' }
      });
      return null;
    }
  }

  private async safeLoginRedirect(request: any): Promise<void> {
    await this.ensureInitialized();
    await msalInstance.handleRedirectPromise(); // nettoie un état précédent

    if (this.isInteractionInProgress()) return;

    try {
      await msalInstance.loginRedirect(request);
    } catch (error: any) {
      if (error instanceof BrowserAuthError
          && error.errorCode === 'interaction_in_progress') return;
      throw error;
    }
  }
}
```

### 9.4 Gestion des tokens

Le JWT PayZen est stocké dans `localStorage` et attaché à chaque requête par l'intercepteur :

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.includes('/assets/')) return next(req);

  const authService = inject(AuthService);
  const contextService = inject(CompanyContextService);

  const token = authService.getToken();
  const companyId = contextService.companyId();
  const isExpertMode = contextService.isExpertMode();

  // Skip pour les routes auth publiques
  if (req.url.includes('/auth/login')
      || req.url.includes('/auth/register')
      || req.url.includes('/auth/entra-login')) {
    return next(req);
  }

  const headers: Record<string, string> = {};

  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (companyId) headers['X-Company-Id'] = companyId;
  if (isExpertMode) headers['X-Role-Context'] = 'expert';
  else if (companyId) headers['X-Role-Context'] = 'standard';

  if (Object.keys(headers).length > 0) {
    return next(req.clone({ setHeaders: headers }));
  }
  return next(req);
};
```

### 9.5 Route Guards

Le système utilise des guards fonctionnels Angular :

| Guard | Rôle |
|-------|------|
| `authGuard` | Vérifie que l'utilisateur est authentifié |
| `guestGuard` | Redirige les utilisateurs connectés (pages auth) |
| `contextGuard` | Vérifie qu'un contexte entreprise est sélectionné |
| `contextSelectionGuard` | Empêche l'accès si le contexte est déjà choisi |
| `expertModeGuard` | Restreint au mode expert (cabinet comptable) |
| `rhGuard` | Admin, RH, Admin PayZen uniquement |
| `createRoleGuard(roles)` | Factory pour guards basés sur les rôles |

```typescript
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) return true;

  router.navigate(['/login'], {
    queryParams: { returnUrl: state.url }
  });
  return false;
};
```

### 9.6 Routes principales

```typescript
export const routes: Routes = [
  // Auth (pass-through redirect vers Entra)
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'auth/callback', loadComponent: () => import('./features/auth/entra-callback/entra-callback.component').then(m => m.EntraCallbackComponent) },
  { path: 'auth/accept-invite', loadComponent: () => import('./features/auth/accept-invite/accept-invite.component').then(m => m.AcceptInviteComponent) },

  // Post-login
  { path: 'select-context', canActivate: [authGuard, contextSelectionGuard], loadComponent: () => ... },

  // Standard mode (/app/*)
  { path: 'app', component: MainLayout, canActivate: [authGuard, contextGuard], children: [...] },

  // Expert mode (/expert/*)
  { path: 'expert', component: MainLayout, canActivate: [authGuard, contextGuard, expertModeGuard], children: [...] },

  // Fallback
  { path: 'access-denied', loadComponent: () => ... },
  { path: '**', redirectTo: 'login' }
];
```

> **Note :** La route `/login` est protégée par `guestGuard`. Les utilisateurs déjà authentifiés sont redirigés vers leur dashboard. Les utilisateurs non-authentifiés sont redirigés automatiquement vers Entra External ID.

### 9.7 Backoffice Angular

Même principe que le frontend RH : **MSAL.js** + **Authorization Code + PKCE**, callback dédié, puis **`POST /api/auth/entra-login`**. Le backoffice n’accepte la session que si la réponse API contient le rôle **`Admin Payzen`** (sinon erreur explicite).

| Élément | Valeur locale |
|---------|----------------|
| Port `ng serve` | `50171` (`angular.json`) |
| `redirectUri` | `http://localhost:50171/auth/callback` |
| `postLogoutRedirectUri` | `http://localhost:50171/login` |
| `apiUrl` | `http://localhost:5119/api` |

Fichiers clés :

- `src/environments/environment.ts` — `entra` + `apiUrl`
- `src/app/config/msal.config.ts` — instance MSAL + `initializeMsal()`
- `src/app/services/entra-redirect.service.ts` — `loginWithEntra`, `handleRedirectPromise`
- `src/app/services/auth.service.ts` — `loginWithEntra`, filtre rôle Admin Payzen, stockage `auth_token` / `user`
- `src/app/features/auth/login/login.component.ts` — redirection immédiate vers Entra (ou message d’erreur si query `error`)
- `src/app/features/auth/entra-callback/entra-callback.component.ts` — échange email/oid → JWT Payzen
- `src/app/guards/guest.guard.ts` — évite la boucle si déjà connecté
- `src/app/app.config.ts` — `APP_INITIALIZER` → `initializeMsal()`
- `src/app/interceptors/auth.interceptor.ts` — ne pas envoyer de Bearer sur `/auth/entra-login`

Fichier d’exemple de configuration API (à copier vers `appsettings.Development.json`, non versionné) : `backend/Payzen.Api/appsettings.Development.example.json`.

---

## 10. Considérations de Sécurité

### 10.1 Validation JWT (Backend)

Le backend valide les JWT avec les paramètres suivants :

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = config["JwtSettings:Issuer"],
            ValidateAudience         = true,
            ValidAudience            = config["JwtSettings:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero, // pas de tolérance
            NameClaimType            = "unique_name",
            RoleClaimType            = "role"
        };
    });
```

| Paramètre | Valeur | Raison |
|-----------|--------|--------|
| `ValidateIssuer` | `true` | Empêche les JWT d'un autre émetteur |
| `ValidateAudience` | `true` | Empêche les JWT destinés à une autre API |
| `ValidateLifetime` | `true` | Rejette les tokens expirés |
| `ClockSkew` | `TimeSpan.Zero` | Pas de marge — expiration stricte |

### 10.2 PKCE (Proof Key for Code Exchange)

PKCE est activé automatiquement par MSAL.js v3 pour toutes les SPA. Côté backend (si OIDC server-side est utilisé) :

```csharp
options.UsePkce = true;
```

PKCE protège contre :
- L'interception du code d'autorisation
- Les attaques de type "authorization code injection"

### 10.3 Stockage des tokens

| Token | Stockage | Raison |
|-------|----------|--------|
| JWT PayZen | `localStorage` (`payzen_auth_token`) | Persistance entre les onglets |
| Invitation token | `sessionStorage` (`pending_invitation_token`) | Nettoyé automatiquement après usage |
| MSAL cache | `localStorage` | Configuration MSAL (`BrowserCacheLocation.LocalStorage`) |

> **Note :** `localStorage` est vulnérable aux attaques XSS. En production, envisager des cookies `HttpOnly` + `SameSite=Strict`.

### 10.4 Expiration des tokens

| Token | Durée | Configuration |
|-------|-------|---------------|
| JWT PayZen | 2 heures (120 min) | `JwtSettings:ExpiresInMinutes` |
| Invitation | 48 heures | `DateTime.UtcNow.AddHours(48)` |
| Session cookie Entra | Géré par Microsoft | Configurable dans le tenant |

Le frontend vérifie l'expiration à chaque initialisation :

```typescript
private isTokenValid(token: string): boolean {
  const payload = this.decodeToken(token);
  if (!payload?.exp) return true;
  const expirationDate = new Date(payload.exp * 1000);
  return expirationDate.getTime() > Date.now() + 30000; // 30s buffer
}
```

### 10.5 Sécurité des invitations

| Mesure | Détail |
|--------|--------|
| Token unique | `Guid.NewGuid().ToString("N")` — 32 caractères hexadécimaux |
| Expiration | 48h maximum |
| Usage unique | Status passe de `Pending` à `Accepted` |
| Vérification email | L'email du JWT Entra **doit correspondre** à l'email de l'invitation |
| Email masqué | L'API renvoie `a***i@example.com` pour éviter la fuite d'information |

### 10.6 CORS

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:50171",  // Backoffice
                "http://localhost:4200",   // Frontend
                "https://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});
```

### 10.7 Headers personnalisés

L'intercepteur Angular envoie des headers contextuels :

| Header | Valeur | Usage |
|--------|--------|-------|
| `Authorization` | `Bearer {jwt}` | Authentification |
| `X-Company-Id` | `{companyId}` | Contexte entreprise |
| `X-Role-Context` | `expert` / `standard` | Mode cabinet expert |

---

## 11. Notes de Développement Local

### 11.1 Simulation des emails

En développement, les emails d'invitation sont loggés dans la console du backend. Pour les récupérer :

1. Lancer le backend avec `dotnet run` dans `backend/Payzen.Api/`
2. Créer une invitation via l'API ou le backoffice
3. Chercher dans la sortie console :

```
======== EMAIL D'INVITATION (SIMULÉ) ========
  Destinataire : admin@newcompany.com
  Entreprise   : Acme Corp
  Rôle         : Admin
  Lien         : http://localhost:4200/auth/accept-invite?token=a8b7c6d5...
  Expire dans  : 48h
==============================================
```

4. Copier le lien et l'ouvrir dans le navigateur

### 11.2 Debugging des tokens

Pour inspecter un JWT PayZen :

1. Ouvrir la console du navigateur
2. Exécuter :

```javascript
const token = localStorage.getItem('payzen_auth_token');
const payload = JSON.parse(atob(token.split('.')[1]));
console.log(payload);
```

Payload typique :

```json
{
  "sub": "42",
  "email": "user@example.com",
  "unique_name": "user",
  "jti": "guid...",
  "uid": "42",
  "role": "Employee",
  "permission": ["VIEW_OWN_PAYSLIPS", "REQUEST_LEAVE"],
  "exp": 1711296000,
  "iss": "PayzenApi",
  "aud": "PayzenClient"
}
```

Pour inspecter les tokens Entra (MSAL) :

```javascript
// Voir les comptes MSAL en cache
const accounts = JSON.parse(localStorage.getItem('msal.account.keys'));
console.log(accounts);
```

Ou utiliser [jwt.ms](https://jwt.ms) pour décoder visuellement un token.

### 11.3 Problèmes courants

#### Redirect URI mismatch

```
AADSTS50011: The redirect URI 'http://localhost:4200/auth/callback'
specified in the request does not match the redirect URIs configured
for the application
```

**Solution :** Vérifier dans Azure Portal → App Registration → Authentication que l'URI `http://localhost:4200/auth/callback` est bien enregistrée comme **SPA** (pas Web).

#### Invalid audience

```
IDX10214: Audience validation failed. Audiences: 'PayzenClient'.
Did not match: validationParameters.ValidAudience: 'xxx'
```

**Solution :** Vérifier que `JwtSettings:Audience` dans `appsettings.json` correspond à la valeur utilisée par `JwtService`.

#### MSAL interaction_in_progress

```
BrowserAuthError: interaction_in_progress
```

**Solution :** L'application gère déjà cette erreur via `safeLoginRedirect`. Si le problème persiste, vider `sessionStorage` :

```javascript
sessionStorage.removeItem('msal.interaction.status');
```

#### CORS errors

```
Access to XMLHttpRequest has been blocked by CORS policy
```

**Solution :** Vérifier que l'origine du frontend est dans la liste CORS du backend (`Program.cs`).

### 11.4 Configuration `appsettings.json` requise

Un modèle versionné est disponible : copiez `backend/Payzen.Api/appsettings.Development.example.json` vers `appsettings.Development.json` (fichier local non versionné) et renseignez les secrets.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PayzenDb;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "JwtSettings": {
    "Key": "VotreCléSecrèteDe256BitsMinimum!PayZen2026!",
    "Issuer": "PayzenApi",
    "Audience": "PayzenClient",
    "ExpiresInMinutes": 120
  },
  "EntraExternalId": {
    "Instance": "https://payzenhr.ciamlogin.com/",
    "TenantId": "votre-tenant-id",
    "ClientId": "votre-client-id-api",
    "ClientSecret": "votre-client-secret",
    "CallbackPath": "/signin-oidc"
  }
}
```

---

## 12. Guide de Production

### 12.1 Remplacer les URLs localhost

| Composant | Dev | Production |
|-----------|-----|------------|
| Frontend | `http://localhost:4200` | `https://app.payzen.ma` |
| API | `http://localhost:5119` | `https://api.payzen.ma` |
| Backoffice | `http://localhost:50171` | `https://admin.payzen.ma` |
| Redirect URI | `http://localhost:4200/auth/callback` | `https://app.payzen.ma/auth/callback` |

Mettre à jour dans :
- `environment.prod.ts` (frontend)
- `appsettings.Production.json` (backend)
- Azure Portal → App Registration → Authentication (Redirect URIs)

### 12.2 Configurer un domaine personnalisé

1. Dans le tenant Entra External ID → **Custom domain names**
2. Ajouter `auth.payzen.ma`
3. Vérifier via enregistrement DNS TXT
4. Mettre à jour `authority` dans `environment.prod.ts`

### 12.3 HTTPS obligatoire

```csharp
// Program.cs (production)
app.UseHttpsRedirection();
app.UseHsts();

// Cookie policy
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.SameSite = SameSiteMode.Strict;
```

Certificat SSL : utiliser Azure App Service (certificat géré) ou Let's Encrypt.

### 12.4 Ajouter un fournisseur SMTP

Remplacer `ConsoleEmailService` par une implémentation réelle :

```csharp
// Exemple avec Azure Communication Services
public class AzureEmailService : IEmailService
{
    private readonly EmailClient _client;
    private readonly string _senderAddress;

    public AzureEmailService(IConfiguration config)
    {
        _client = new EmailClient(config["AzureCommunication:ConnectionString"]);
        _senderAddress = config["AzureCommunication:SenderAddress"];
    }

    public async Task SendInvitationEmailAsync(
        string email, string companyName, string role, string token, CancellationToken ct)
    {
        var link = $"https://app.payzen.ma/auth/accept-invite?token={token}";

        var emailMessage = new EmailMessage(
            senderAddress: _senderAddress,
            content: new EmailContent("Invitation PayZen")
            {
                Html = $@"
                    <h2>Vous êtes invité à rejoindre {companyName}</h2>
                    <p>Rôle : {role}</p>
                    <a href='{link}'>Accepter l'invitation</a>
                    <p>Ce lien expire dans 48 heures.</p>"
            },
            recipients: new EmailRecipients(
                new List<EmailAddress> { new(email) }));

        await _client.SendAsync(WaitUntil.Completed, emailMessage, ct);
    }
}
```

Enregistrement dans DI :

```csharp
// Production
builder.Services.AddScoped<IEmailService, AzureEmailService>();

// Développement
builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
```

### 12.5 Azure Key Vault pour les secrets

```csharp
// Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://payzen-kv.vault.azure.net/"),
    new DefaultAzureCredential());
```

Secrets à stocker dans Key Vault :

| Secret | Nom Key Vault |
|--------|---------------|
| JWT signing key | `JwtSettings--Key` |
| Entra Client Secret | `EntraExternalId--ClientSecret` |
| SQL Connection String | `ConnectionStrings--DefaultConnection` |
| Azure Communication | `AzureCommunication--ConnectionString` |

### 12.6 Monitoring et logging

```csharp
// Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Structured logging
builder.Logging.AddAzureWebAppDiagnostics();

// Health checks
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddUrlGroup(new Uri("https://payzenhr.ciamlogin.com"), "Entra External ID");
```

### 12.7 Durcissement sécurité

```csharp
// CORS restrictif
policy.WithOrigins("https://app.payzen.ma", "https://admin.payzen.ma")
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowCredentials();

// Security headers (middleware ou Helmet)
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
```

Checklist production :

- [ ] HTTPS partout (frontend, API, backoffice)
- [ ] Cookies `HttpOnly`, `Secure`, `SameSite=Strict`
- [ ] Redirect URIs production dans Azure Portal
- [ ] Key Vault pour tous les secrets
- [ ] Rate limiting sur les endpoints d'invitation
- [ ] Logs structurés (Application Insights)
- [ ] Alertes sur les erreurs 401/403 anormales
- [ ] Rotation régulière de la clé JWT

---

## 13. Pièges Courants

### 13.1 Essayer de gérer les mots de passe

**Erreur :** Stocker ou valider des mots de passe dans PayZen.

**Réalité :** PayZen ne touche **jamais** aux mots de passe. Le champ `PasswordHash` est `null` pour les comptes Entra. La méthode `VerifyPassword` retourne `false` si `PasswordHash` est null :

```csharp
public bool VerifyPassword(string password)
    => string.IsNullOrWhiteSpace(PasswordHash)
        ? false
        : BCrypt.Net.BCrypt.Verify(password, PasswordHash);
```

### 13.2 Redirect URIs mal configurées

| Symptôme | Cause | Solution |
|----------|-------|----------|
| `AADSTS50011` | URI pas enregistrée dans Azure Portal | Ajouter l'URI exacte (protocole + port inclus) |
| Boucle de redirection | URI enregistrée comme "Web" au lieu de "SPA" | Changer la plateforme en SPA |
| CORS error après callback | `redirectUri` dans `environment.ts` ne correspond pas | Aligner toutes les URIs |

Vérification rapide :

```
Azure Portal → App Registration → Authentication → Redirect URIs
```

Doit contenir exactement : `http://localhost:4200/auth/callback` (platform: SPA)

### 13.3 Validation de token invalide

| Problème | Cause | Solution |
|----------|-------|----------|
| JWT rejeté | Mauvais `Issuer` ou `Audience` | Vérifier `appsettings.json` vs `JwtService` |
| Token expiré instantanément | `ClockSkew = Zero` + horloge décalée | Synchroniser l'horloge NTP ou ajouter un `ClockSkew` de 30s |
| Claims manquants | `NameClaimType` / `RoleClaimType` mal configurés | Vérifier la correspondance avec les claims du token |

### 13.4 Confusion Azure AD vs External ID

| Azure AD (Workforce) | Entra External ID (CIAM) |
|----------------------|--------------------------|
| Pour les employés de l'organisation | Pour les clients / utilisateurs externes |
| Tenant `.onmicrosoft.com` | Tenant `.ciamlogin.com` |
| Authority : `login.microsoftonline.com` | Authority : `payzenhr.ciamlogin.com` |
| B2B collaboration | Pas de B2B |

PayZen utilise **External ID (CIAM)** exclusivement. Ne pas confondre les endpoints.

### 13.5 Double enregistrement d'authentification

Le `Program.cs` actuel contient deux appels `AddAuthentication()` :

```csharp
// Premier : Cookie + OpenIdConnect (server-side)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie()
    .AddOpenIdConnect("EntraExternalId", ...);

// Second : JWT Bearer (API REST)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(...);
```

Le **second** écrase le `DefaultAuthenticateScheme` du premier. En pratique, seul le JWT Bearer est actif pour l'API REST. Le bloc Cookie + OIDC est un vestige de la configuration server-side et peut être retiré si non utilisé.

### 13.6 Gestion des erreurs `interaction_in_progress`

MSAL ne permet qu'une seule interaction à la fois. Un double-clic ou un rafraîchissement pendant le redirect provoque cette erreur. La solution est implémentée dans `EntraRedirectService.safeLoginRedirect()` :

1. Vérifier `sessionStorage` pour `msal.interaction.status`
2. Appeler `handleRedirectPromise()` avant chaque `loginRedirect()`
3. Attraper `BrowserAuthError` avec le code `interaction_in_progress`

### 13.7 Email mismatch lors de l'acceptation d'invitation

L'utilisateur se connecte avec un compte Microsoft dont l'email **ne correspond pas** à l'email de l'invitation. Le backend vérifie strictement :

```csharp
if (!string.Equals(invitation.Email, idpEmail, StringComparison.OrdinalIgnoreCase))
    return InvitationAcceptResult.EmailMismatch(invitation.Email, idpEmail);
```

**Conseil UX :** Afficher clairement l'email attendu (masqué) sur la page d'acceptation avant que l'utilisateur ne clique sur "Se connecter".

---

## Annexe — Structure des fichiers auth

```
payzen/
├── frontend/src/
│   ├── environments/
│   │   ├── environment.ts          # Config dev (clientId, authority, redirectUri)
│   │   └── environment.prod.ts     # Config production
│   └── app/
│       ├── core/
│       │   ├── config/
│       │   │   └── msal.config.ts  # Initialisation MSAL
│       │   ├── guards/
│       │   │   └── auth.guard.ts   # authGuard, guestGuard, rhGuard, etc.
│       │   ├── interceptors/
│       │   │   └── auth.interceptor.ts  # Injection Bearer + X-Company-Id
│       │   ├── models/
│       │   │   └── user.model.ts   # User, UserRole, ROLE_PERMISSIONS
│       │   └── services/
│       │       ├── auth.service.ts           # Gestion état auth + JWT
│       │       ├── entra-redirect.service.ts # MSAL redirect (PKCE)
│       │       └── invitation.service.ts     # Validate + Accept invitations
│       └── features/auth/
│           ├── login/                 # Pass-through redirect Entra (spinner)
│           ├── entra-callback/        # Traitement retour Entra
│           ├── accept-invite/         # Acceptation d'invitation
│           ├── context-selection/     # Choix du workspace
│           └── access-denied.ts       # Page 403
│
├── backoffice/src/
│   ├── environments/
│   │   ├── environment.ts             # apiUrl + entra (redirect 50171)
│   │   └── environment.prod.ts
│   └── app/
│       ├── config/
│       │   └── msal.config.ts
│       ├── guards/
│       │   ├── auth.guard.ts
│       │   └── guest.guard.ts
│       ├── interceptors/
│       │   └── auth.interceptor.ts    # Skip Bearer sur /auth/entra-login
│       ├── services/
│       │   ├── auth.service.ts        # loginWithEntra + filtre Admin Payzen
│       │   └── entra-redirect.service.ts
│       └── features/auth/
│           ├── login/
│           └── entra-callback/
│
└── backend/
    ├── Payzen.Api/
    │   ├── Controllers/Auth/
    │   │   ├── AuthController.cs          # /api/auth (login, entra-login, me)
    │   │   └── InvitationController.cs    # /api/invitations
    │   ├── Authorization/
    │   │   └── HasPermissionAttribute.cs
    │   ├── appsettings.Development.example.json  # Modèle JwtSettings, Email, ConnectionStrings
    │   └── Program.cs                     # Auth config (JWT Bearer + CORS)
    │
    ├── Payzen.Application/
    │   ├── DTOs/Auth/
    │   │   ├── AuthDtos.cs                # LoginRequest, EntraLoginRequest, etc.
    │   │   └── InvitationDtos.cs          # InviteAdmin, AcceptViaIdp, etc.
    │   └── Interfaces/
    │       ├── IAuthService.cs
    │       ├── IJwtService.cs
    │       └── IInvitationService.cs
    │
    ├── Payzen.Domain/
    │   ├── Entities/Auth/
    │   │   ├── Users.cs                   # ExternalId, Source, PasswordHash nullable
    │   │   ├── Roles.cs
    │   │   ├── Invitation.cs
    │   │   ├── UsersRoles.cs
    │   │   └── RolesPermissions.cs
    │   └── Enums/Auth.cs                  # InvitationStatus
    │
    └── Payzen.Infrastructure/
        └── Services/Auth/
            ├── AuthService.cs             # Login + Entra login + provisioning
            ├── JwtService.cs              # Génération + validation JWT
            └── InvitationService.cs       # CRUD invitations + acceptation
```
