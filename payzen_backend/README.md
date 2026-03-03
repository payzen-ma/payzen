# 📘 API PayZen - Documentation Complète

## 🚀 Introduction

API REST pour la gestion de la paie (PayZen) développée avec **.NET 9** et **SQL Server**.

### 🔧 Technologies utilisées
- **.NET 9** (C# 13.0)
- **ASP.NET Core Web API**
- **Entity Framework Core** (ORM)
- **JWT Authentication** (Bearer Token)
- **BCrypt** (Hachage des mots de passe)
- **SQL Server** (Base de données)
- **Swagger/OpenAPI** (Documentation interactive)

---

## 📋 Table des matières

1. [Configuration](#-configuration)
2. [Authentification](#-authentification)
3. [Endpoints](#-endpoints)
   - [Auth](#1-authentication)
   - [Users](#2-users---gestion-des-utilisateurs)
   - [Roles](#3-roles---gestion-des-rôles)
   - [Permissions](#4-permissions---gestion-des-permissions)
   - [Roles-Permissions](#5-roles-permissions---liaison-rôles--permissions)
   - [Users-Roles](#6-users-roles---liaison-utilisateurs--rôles)
   - [Companies](#7-companies---gestion-des-sociétés)
   - [Employees](#8-employees---gestion-des-employés)
4. [Modèles de données](#-modèles-de-données)
5. [Permissions système](#-permissions-système)
6. [Codes d'erreur](#-codes-derreur)

---

## 🔧 Configuration

### Base URL
http://localhost:5119

### Headers requis
Content-Type: application/json <br>
Authorization: Bearer {token}<br>
### Variables d'environnement (appsettings.json)

```json
{ "ConnectionStrings": 
	{
		"DefaultConnection": "Server=...;Database=PayZenDB;..."
	},
		"JwtSettings":
	{
		"Key": "votre-clé-secrète-super-sécurisée",
				"Issuer": "PayzenApi",
				"Audience": "PayzenApp",
				"ExpiresInMinutes": 120
	}
}
```

---

## 🔐 Authentification

L'API utilise **JWT (JSON Web Tokens)** pour l'authentification.

### Workflow
1. **Login** : `POST /api/auth/login` → Retourne un token JWT
2. **Utilisation** : Ajouter le token dans le header `Authorization: Bearer {token}`
3. **Expiration** : 120 minutes par défaut
4. **Logout** : `POST /api/auth/logout` (côté client : supprimer le token)

---

## 📚 Endpoints

### 1. Authentication
#### 🔑 Login

POST /api/auth/login

```
Content-Type: application/json
{
	"Email": "admin@payzen.com",
	"Password": "12345678"
}
```

**Réponse (200 OK)**
```json
{
	"message": "Authentification réussie",
	"token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
	"expiresAt": "2025-01-28T14:30:00Z",
	"user": 
	{
		"id": 1,
		"email": "admin@payzen.com",
		"username": "admin",
		"firstName": "Admin",
		"lastName": "PayZen",
		"roles": ["Admin"],
		"permissions": 
			["READ_USERS",
			"CREATE_COMPANY",
			.....]
	}
}
```

**Erreurs possibles**
- `400 Bad Request` : Données invalides
- `401 Unauthorized` : Email ou mot de passe incorrect

---

#### 🚪 Logout

POST /api/auth/logout<br>
Authorization: Bearer {token}<br>
**Réponse (200 OK)**

```json
{ "message": "Déconnexion réussie. Veuillez supprimer le token côté client." }
```

---

### 2. Users - Gestion des utilisateurs

#### 📋 Récupérer tous les utilisateurs

GET /api/users<br>
Authorization: Bearer {token}<br>
**Permission requise** : `READ_USERS`

**Réponse (200 OK)**

```json
[
	{
		"id": 1,
		"username": "admin",
		"email": "admin@payzen.com",
		"isActive": true,
		"createdAt": "2025-01-15T10:30:00"
	}
]
```

---

#### 🔍 Récupérer un utilisateur par ID

GET /api/users/{id}<br>
Authorization: Bearer {token}<br>

**Permission requise** : `VIEW_USERS`

**Réponse (200 OK)**

```json
{
	"id": 1,
	"username": "admin",
	"email": "admin@payzen.com",
	"isActive": true,
	"createdAt": "2025-01-15T10:30:00"
}
```

**Erreurs possibles**
- `404 Not Found` : Utilisateur non trouvé

---

#### ➕ Créer un utilisateur

POST /api/users <br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"Username": "john.doe",
	"Email": "john.doe@payzen.com",
	"Password": "SecurePass123!",
	"IsActive": true
}
```

**Permission requise** : `CREATE_USERS`

**Réponse (201 Created)**
```json
{
	"id": 5,
	"username": "john.doe",
	"email": "john.doe@payzen.com",
	"isActive": true,
	"createdAt": "2025-01-27T14:30:00"
}
```

**Erreurs possibles**
- `400 Bad Request` : Validation échouée
- `409 Conflict` : Email ou username déjà utilisé

---

#### ✏️ Mettre à jour un utilisateur
PUT /api/users/{id}<br>
Authorization: Bearer {token}<br>
Content-Type: application/json

```json
{
	"Email": "newemail@payzen.com",
	"Password": "NewPassword123!",
	"IsActive": false
}
```

**Permission requise** : `EDIT_USERS`

**Note** : Tous les champs sont optionnels (mise à jour partielle)

---

#### 🗑️ Supprimer un utilisateur (soft delete)

DELETE /api/users/{id}<br>
Authorization: Bearer {token}<br>

**Permission requise** : `DELETE_USERS`

**Réponse (204 No Content)**

**Erreurs possibles**
- `404 Not Found` : Utilisateur non trouvé

---

### 3. Roles - Gestion des rôles

#### 📋 Récupérer tous les rôles
GET /api/roles<br>
Authorization: Bearer {token}<br>

**Réponse (200 OK)**

```json
[
	{
		"id": 1,
		"name": "Admin",
		"description": "Administrateur système",
		"createdAt": "2025-01-15T10:00:00" 
	}
]
```

---

#### 🔍 Récupérer un rôle par ID

GET /api/roles/{id}<br>
Authorization: Bearer {token}<br>

---

#### ➕ Créer un rôle

POST /api/roles<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>
```json
{
	"Name": "Manager",
	"Description": "Gestionnaire avec droits limités"
}
```

**Réponse (201 Created)**

---

#### ✏️ Mettre à jour un rôle

PUT /api/roles/{id}<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>
```json
{
	"Name": "Super Admin",
	"Description": "Super administrateur"
}
```

---

#### 🗑️ Supprimer un rôle (soft delete)

DELETE /api/roles/{id}<br>
Authorization: Bearer {token}<br>

**Note** : Ne peut pas supprimer un rôle assigné à des utilisateurs

---

### 4. Permissions - Gestion des permissions

#### 📋 Récupérer toutes les permissions

GET /api/permissions<br>
Authorization: Bearer {token}<br>

**Réponse (200 OK)**

```json
[
	{
		"id": 1,
		"name": "READ_USERS",
		"description": "Lire les informations des utilisateurs",
		"createdAt": "2025-01-15T10:00:00"
	}
]
```

---

#### 🔍 Récupérer une permission par ID

GET /api/permissions/{id}<br>
Authorization: Bearer {token}<br>

---

#### ➕ Créer une permission

POST /api/permissions<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"Name": "CREATE_EMPLOYEES",
	"Description": "Permet de créer des employés"
}
```

**Réponse (201 Created)**

---

#### ✏️ Mettre à jour une permission

PUT /api/permissions/{id}<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"Description": "Nouvelle description de la permission"
}
```

---

#### 🗑️ Supprimer une permission (soft delete)

DELETE /api/permissions/{id}<br>
Authorization: Bearer {token}<br>

---

### 5. Roles-Permissions - Liaison Rôles ↔ Permissions

#### 📋 Récupérer les permissions d'un rôle

GET /api/roles-permissions/role/{roleId}<br>
Authorization: Bearer {token}<br>

**Réponse (200 OK)**

```json
[
	{
		"id": 1,
		"name": "READ_USERS",
		"description": "Lire les utilisateurs",
		"createdAt": "2025-01-15T10:00:00"
	}
]
```

---

#### ➕ Assigner une permission à un rôle

POST /api/roles-permissions <br>
Authorization: Bearer {token} <br>
Content-Type: application/json <br>

```json
{
	"RoleId": 1,
	"PermissionId": 5
}
```

**Réponse (200 OK)**

```json
{ "message": "Permission assignée avec succès" }
```

---

#### 🗑️ Retirer une permission d'un rôle
DELETE /api/roles-permissions<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"RoleId": 1,
	"PermissionId": 5
}
```

**Réponse (204 No Content)**

---

### 6. Users-Roles - Liaison Utilisateurs ↔ Rôles

#### 📋 Récupérer les rôles d'un utilisateur
GET /api/users-roles/user/{userId} <br>
Authorization: Bearer {token}<br>

**Réponse (200 OK)**

```json
[
	{
		"id": 1,
		"name": "Admin",
		"description": "Administrateur système",
		"createdAt": "2025-01-15T10:00:00"
	}
]
```
---

#### 📋 Récupérer les utilisateurs ayant un rôle
GET /api/users-roles/role/{roleId}<br>
Authorization: Bearer {token}<br>

---

#### ➕ Assigner un rôle à un utilisateur
POST /api/users-roles<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"UserId": 5,
	"RoleId": 2
}
```

**Réponse (200 OK)**
```json
{ "message": "Rôle assigné avec succès" }
```

---

#### ➕ Assigner plusieurs rôles à un utilisateur
POST /api/users-roles/bulk-assign<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"UserId": 5,
	"RoleIds": [1, 2, 3]
}
```

**Réponse (200 OK)**

```json
{
	"message": "Rôles assignés avec succès",
	"assigned": 2,
	"reactivated": 1,
	"skipped": 0
}
```

---

#### 🔄 Remplacer tous les rôles d'un utilisateur
PUT /api/users-roles/replace<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"UserId": 5,
	"RoleIds": [2, 3]
}
```

**Réponse (200 OK)**

```json
{
	"message": "Rôles remplacés avec succès",
	"removed": 3,
	"assigned": 2,
	"reactivated": 0
}
```

---

#### 🗑️ Retirer un rôle d'un utilisateur
DELETE /api/users-roles<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"UserId": 5,
	"RoleId": 2
}
```

**Réponse (204 No Content)**

---

### 7. Companies - Gestion des sociétés

#### 📋 Récupérer toutes les sociétés
GET /api/companies<br>
Authorization: Bearer {token}<br>

**Permission requise** : `READ_COMPANIES`

**Réponse (200 OK)**

```json
[
	{
		"id": 1,
		"companyName": "TechMaroc SARL",
		"companyAddress": "123 Boulevard Mohammed V",
		"cityId": 1,
		"countryId": 1,
		"iceNumber": "001234567890123",
		"cnssNumber": "1234567",
		"ifNumber": "12345678",
		"rcNumber": "123456",
		"ribNumber": "123456789012345678901234",
		"phoneNumber": 522123456,
		"email": "contact@techmaroc.ma",
		"managedByCompanyId": null,
		"managedByCompanyName": null,
		"isCabinetExpert": false,
		"createdAt": "2025-01-20T10:00:00"
	}
]
```

---

#### 🔍 Récupérer une société par ID
GET /api/companies/{id}<br>
Authorization: Bearer {token}<br>

**Permission requise** : `VIEW_COMPANY`

---

#### 🏢 Récupérer les sociétés gérées par un cabinet
GET /api/companies/managed-by/{managedByCompanyId}<br>
Authorization: Bearer {token}<br>

**Permission requise** : `VIEW_MANAGED_COMPANIES`

---

#### 🏛️ Récupérer tous les cabinets d'experts
GET /api/companies/cabinets-experts<br>
Authorization: Bearer {token}<br>

**Permission requise** : `VIEW_CABINET_EXPERTS`

---

#### ➕ Créer une société
POST /api/companies<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"CompanyName": "TechMaroc Solutions SARL",
	"CompanyAddress": "123 Boulevard Mohammed V",
	"CityId": 1,
	"CountryId": 1,
	"IceNumber": "001234567890123",
	"CnssNumber": "1234567",
	"IfNumber": "12345678",
	"RcNumber": "123456",
	"RibNumber": "123456789012345678901234",
	"PhoneNumber": 522123456,
	"Email": "contact@techmaroc.ma",
	"ManagedByCompanyId": null,
	"IsCabinetExpert": false
}
```

**Permission requise** : `CREATE_COMPANY`

**Réponse (201 Created)**

**Erreurs possibles**
- `409 Conflict` : ICE ou Email déjà utilisé
- `404 Not Found` : Société gérante non trouvée (si `ManagedByCompanyId` fourni)

---

#### ✏️ Mettre à jour une société
PUT /api/companies/{id}<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"CompanyName": "TechMaroc Updated",
	"Email": "new.contact@techmaroc.ma",
	"PhoneNumber": 522999888
}
```

**Permission requise** : `EDIT_COMPANY`

---

#### 🗑️ Supprimer une société (soft delete)
DELETE /api/companies/{id}<br>
Authorization: Bearer {token}<br>

**Permission requise** : `DELETE_COMPANY`

**Erreurs possibles**
- `400 Bad Request` : Société a des employés ou gère d'autres sociétés

---

### 8. Employees - Gestion des employés

#### 📋 Récupérer tous les employés
GET /api/employees<br>
Authorization: Bearer {token}<br>

**Permission requise** : `READ_EMPLOYEES`

**Réponse (200 OK)**

```json
[
	{
		"id": 1,
		"firstName": "Mohammed",
		"lastName": "Benali",
		"cinNumber": "AB123456",
		"dateOfBirth": "1990-05-15T00:00:00",
		"phone": 612345678,
		"email": "mohammed.benali@company.com",
		"companyId": 1,
		"companyName": "TechMaroc SARL",
		"managerId": null,
		"managerName": null,
		"statusId": 1,
		"genderId": 1,
		"nationalityId": 1,
		"educationLevelId": 3, 
		"maritalStatusId": 2, 
		"createdAt": "2025-01-25T10:00:00" 
	}
]
```

---

#### 🔍 Récupérer un employé par ID
GET /api/employees/{id} <br>
Authorization: Bearer {token}<br>

**Permission requise** : `VIEW_EMPLOYEE`

---

#### 🏢 Récupérer les employés d'une société
GET /api/employees/company/{companyId}<br>
Authorization: Bearer {token}<br>

**Permission requise** : `VIEW_COMPANY_EMPLOYEES`

---

#### 👥 Récupérer les subordonnés d'un manager
GET /api/employees/manager/{managerId}/subordinates<br>
Authorization: Bearer {token}<br>

**Permission requise** : `VIEW_SUBORDINATES`

---

#### ➕ Créer un employé (avec compte utilisateur automatique)
POST /api/employees <br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"FirstName": "Mohammed",
	"LastName": "Benali",
	"CinNumber": "AB123456",
	"DateOfBirth": "1990-05-15",
	"Phone": 612345678,
	"Email": "mohammed.benali@company.com", 
	"CompanyId": 1, 
	"ManagerId": null, 
	"StatusId": 1, 
	"GenderId": 1, 
	"NationalityId": 1, 
	"EducationLevelId": 3, 
	"MaritalStatusId": 2, 
	"Password": "CustomPass123!",
	"CreateUserAccount": true 
}
```

**Permission requise** : `CREATE_EMPLOYEE`

**Réponse (201 Created)**

```json
{
	"employee": 
		{
			"id": 1, 
			"firstName": "Mohammed", 
			"lastName": "Benali", 
			"email": "mohammed.benali@company.com",
			"companyName": "TechMaroc SARL", 
			... 
		}, 
	"userAccount": 
		{
			"username": "mohammed.benali",
			"email": "mohammed.benali@company.com",
			"temporaryPassword": "AB12cd34!@Xy", 
			"message": "Un compte utilisateur a été créé. Le mot de passe temporaire doit être changé lors de la première connexion."
		}
}
```

**Champs requis** :
- `FirstName`, `LastName`, `CinNumber`, `DateOfBirth`, `Phone`, `Email`, `CompanyId`

**Champs optionnels** :
- `ManagerId`, `StatusId`, `GenderId`, `NationalityId`, `EducationLevelId`, `MaritalStatusId`
- `Password` : Si non fourni, un mot de passe temporaire est généré automatiquement
- `CreateUserAccount` : `true` par défaut (créer un compte utilisateur)

**Erreurs possibles**
- `409 Conflict` : CIN ou Email déjà utilisé
- `404 Not Found` : Société ou Manager non trouvé

---

#### ✏️ Mettre à jour un employé
PUT /api/employees/{id}<br>
Authorization: Bearer {token}<br>
Content-Type: application/json<br>

```json
{
	"Email": "new.email@company.com",
	"Phone": 699999999,
	"ManagerId": 3,
	"StatusId": 2
}
```

**Permission requise** : `EDIT_EMPLOYEE`

---

#### 🗑️ Supprimer un employé (soft delete)
DELETE /api/employees/{id} <br>
Authorization: Bearer {token}<br>

**Permission requise** : `DELETE_EMPLOYEE`

**Erreurs possibles**
- `400 Bad Request` : Employé est manager d'autres employés ou lié à un compte utilisateur

---

## 📊 Modèles de données

### User
```json
{
	"id": 1, 
	"employeeId": 5, 
	"username": "john.doe", 
	"email": "john.doe@company.com", 
	"passwordHash": "...", 
	"isActive": true,
	"createdAt": "2025-01-15T10:00:00", 
	"createdBy": 1, 
	"updatedAt": null,
	"updatedBy": null, 
	"deletedAt": null, 
	"deletedBy": null 
}
```

### Role
```json
{
	"id": 1,
	"name": "Admin",
	"description": "Administrateur système",
	"createdAt": "2025-01-15T10:00:00", 
	"createdBy": 1 
}
```

### Permission
```json
{ 
	"id": 1, 
	"name": "READ_USERS",
	"description": "Lire les utilisateurs", 
	"createdAt": "2025-01-15T10:00:00", 
	"createdBy": 1
}
```

### Company
```json
{ 
	"id": 1,
	"companyName": "TechMaroc SARL",
	"companyAddress": "123 Boulevard Mohammed V",
	"iceNumber": "001234567890123", 
	"cnssNumber": "1234567",
	"ifNumber": "12345678",
	"rcNumber": "123456",
	"ribNumber": "123456789012345678901234",
	"phoneNumber": 522123456,
	"email": "contact@techmaroc.ma",
	"managedByCompanyId": null,
	"isCabinetExpert": false, 
	"createdAt": "2025-01-20T10:00:00",
	"createdBy": 1
}
```

### Employee
```json
{ 
	"id": 1, 
	"firstName": "Mohammed",
	"lastName": "Benali",
	"cinNumber": "AB123456",
	"dateOfBirth": "1990-05-15",
	"phone": 612345678, 
	"email": "mohammed.benali@company.com",
	"companyId": 1, 
	"managerId": null,
	"statusId": 1,
	"genderId": 1,
	"createdAt": "2025-01-25T10:00:00", 
	"createdBy": 1
}
````

---

## 🔐 Permissions système

### Users
- `READ_USERS` : Lister les utilisateurs
- `VIEW_USERS` : Voir les détails d'un utilisateur
- `CREATE_USERS` : Créer un utilisateur
- `EDIT_USERS` : Modifier un utilisateur
- `DELETE_USERS` : Supprimer un utilisateur

### Roles
- `READ_ROLES` : Lister les rôles
- `VIEW_ROLE` : Voir les détails d'un rôle
- `CREATE_ROLE` : Créer un rôle
- `EDIT_ROLE` : Modifier un rôle
- `DELETE_ROLE` : Supprimer un rôle
- `ASSIGN_ROLES` : Assigner des rôles aux utilisateurs
- `REVOKE_ROLES` : Retirer des rôles aux utilisateurs

### Permissions
- `READ_PERMISSIONS` : Lister les permissions
- `MANAGE_PERMISSIONS` : Gérer les permissions

### Companies
- `READ_COMPANIES` : Lister les sociétés
- `VIEW_COMPANY` : Voir les détails d'une société
- `CREATE_COMPANY` : Créer une société
- `EDIT_COMPANY` : Modifier une société
- `DELETE_COMPANY` : Supprimer une société
- `VIEW_MANAGED_COMPANIES` : Voir les sociétés gérées
- `VIEW_CABINET_EXPERTS` : Voir les cabinets d'experts
- `MANAGE_COMPANY_HIERARCHY` : Gérer la hiérarchie des sociétés

### Employees
- `READ_EMPLOYEES` : Lister les employés
- `VIEW_EMPLOYEE` : Voir les détails d'un employé
- `CREATE_EMPLOYEE` : Créer un employé
- `EDIT_EMPLOYEE` : Modifier un employé
- `DELETE_EMPLOYEE` : Supprimer un employé
- `VIEW_COMPANY_EMPLOYEES` : Voir les employés d'une société
- `VIEW_SUBORDINATES` : Voir les subordonnés
- `MANAGE_EMPLOYEE_MANAGER` : Gérer les managers

---

## ⚠️ Codes d'erreur

| Code					       | Description				 |
|------------------------------|-----------------------------|
| `200 OK`					   | Requête réussie			 |
| `201 Created`				   | Ressource créée avec succès |
| `204 No Content`			   | Suppression réussie         |
| `400 Bad Request`			   | Données invalides			 |
| `401 Unauthorized`		   | Non authentifié			 |
| `403 Forbidden`			   | Permission refusée			 |
| `404 Not Found`			   | Ressource non trouvée		 |
| `409 Conflict`               | Conflit (doublon)			 |
| `500 Internal Server Error`  | Erreur serveur				 |

---

## 📝 Exemples de workflows

### Workflow 1 : Créer un système complet
1. **Login** → Obtenir le token
2. **Créer des permissions** → `POST /api/permissions`
3. **Créer des rôles** → `POST /api/roles`
4. **Assigner permissions aux rôles** → `POST /api/roles-permissions`
5. **Créer des utilisateurs** → `POST /api/users`
6. **Assigner rôles aux utilisateurs** → `POST /api/users-roles`

### Workflow 2 : Créer une entreprise avec employés
1. **Login** → `POST /api/auth/login`
2. **Créer une société** → `POST /api/companies`
3. **Créer le CEO** → `POST /api/employees` (sans `ManagerId`)
4. **Créer des directeurs** → `POST /api/employees` (avec `ManagerId` du CEO)
5. **Créer des employés** → `POST /api/employees` (avec `ManagerId` des directeurs)
6. **Consulter la hiérarchie** → `GET /api/employees/manager/{managerId}/subordinates`

---

## 🛠️ Développement

### Prérequis
- **.NET 9 SDK**
- **SQL Server** (LocalDB ou instance complète)
- **Visual Studio 2022** ou **VS Code**

### Installation
Cloner le dépôt
git clone https://github.com/mo-shab/payzen_backend.git cd payzen_backend
Restaurer les packages
dotnet restore
Mettre à jour la base de données
dotnet ef database update
Lancer l'application
dotnet run

### Tests
Utilisez le fichier `payzen_backend.http` avec l'extension **REST Client** (VS Code) ou **HTTP Client** (Rider/Visual Studio).

---

## 📞 Support

Pour toute question ou problème :
- **GitHub** : [mo-shab/payzen_backend](https://github.com/mo-shab/payzen_backend)
- **Email** : support@payzen.com

---

## 📄 Licence

© 2025 PayZen. Tous droits réservés.