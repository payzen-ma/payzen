# Payzen - Documentation technique

Ce document decrit l'architecture technique du socle Payzen (frontend, backoffice, backend, landing page), les choix de conception, les flux applicatifs, et les conventions de maintenance.

## Vue d'ensemble technique

Payzen est un monorepo organise autour de 4 surfaces applicatives :
- `frontend` : application metier RH/employe (Angular)
- `backoffice` : application d'administration plateforme (Angular)
- `backend` : API REST et logique metier (ASP.NET Core + EF Core)
- `landingpage` : point d'entree public (site statique/PHP)

Le coeur metier est centralise dans `backend`, tandis que `frontend` et `backoffice` consomment les memes endpoints API avec des contraintes de roles distinctes.

## Structure du repository

```text
payzen/
├── backend/
│   ├── Payzen.Api/              # Transport HTTP, DI, middleware, auth
│   ├── Payzen.Application/      # Contrats applicatifs (DTOs, interfaces, validators)
│   ├── Payzen.Domain/           # Entites et enums metier
│   ├── Payzen.Infrastructure/   # Services concrets, EF Core, integrations
│   └── Payzen.Tests/            # Tests unitaires/integration
├── frontend/                    # UI RH/employe (Angular 20)
├── backoffice/                  # UI admin plateforme (Angular 20)
├── landingpage/                 # Site public
├── docs/                        # Documentation transverse (auth, etc.)
├── documentation/               # Documents techniques complementaires
└── payzen - old/                # Historique/archive hors scope actif
```

## Architecture backend (couches)

Architecture logique :
1. `Payzen.Api` : controllers, pipeline HTTP, securite, orchestration
2. `Payzen.Application` : contrats de services, DTOs, validation
3. `Payzen.Domain` : modele metier (entites, enums, invariants)
4. `Payzen.Infrastructure` : persistence, implementations de services, integrations externes

Regle de dependance :
- `Api` -> `Application` + `Infrastructure`
- `Infrastructure` -> `Application` + `Domain`
- `Application` -> `Domain`
- `Domain` ne depend pas des couches superieures

Objectif : isoler le modele metier des details de transport HTTP et des details techniques (SQL, PDF, email, etc.).

## Domaines metier principaux

- Authentification, roles et permissions
- Entreprises et contexte multi-tenant
- Employes (profil, contrat, affectation)
- Conges, absences, heures supplementaires
- Paie (calcul, simulation, export)
- Evenements, audit, tableaux de bord

## Flux applicatif de reference

Flux type d'une requete :
1. Le client appelle un endpoint dans `Payzen.Api`.
2. Le pipeline applique auth, autorisation, validation et gestion d'erreurs.
3. Le controller mappe vers un DTO applicatif (`Payzen.Application`).
4. Une interface de service est invoquee.
5. L'implementation `Payzen.Infrastructure` execute la logique metier et persistence.
6. Le resultat est renvoye au controller puis au client.

Ce decouplage simplifie les tests, limite l'impact des changements et stabilise les contrats API.

## Authentification et autorisation

Le systeme s'appuie sur :
- Microsoft Entra External ID (login identite)
- MSAL cote applications Angular
- JWT interne Payzen pour securiser l'API

Principes :
- aucune gestion de mot de passe applicative pour les comptes Entra
- claims de roles/permissions portes par le JWT Payzen
- controle d'acces fin via policies et attributs d'autorisation

Documentation detaillee : `docs/AUTH_SYSTEM.md`.

## Persistence et donnees

`Payzen.Infrastructure` contient :
- `AppDbContext`
- configurations EF Core
- migrations SQL Server
- services de persistence et projections

Conventions techniques notables :
- soft delete via champs de suppression logique
- timestamps d'audit (`CreatedAt`, `UpdatedAt`)
- mapping centralise via Fluent API

## Frontend et backoffice (architecture UI)

Les deux applications Angular suivent une structure feature-first :
- `core`/`config` : services singleton, config, guards, interceptors
- `features` : modules fonctionnels par domaine
- `shared` : composants et utilitaires reutilisables

Responsabilites :
- `frontend` : operations RH, paie, experience employe
- `backoffice` : administration plateforme, tenants, referentiels, supervision

Les deux UIs consomment l'API commune avec une separation stricte par roles et contexte.

## Integrations techniques

- Auth federée : Microsoft Entra External ID
- Generation documents : IronPDF
- Exports tabulaires : ClosedXML
- SQL Server via EF Core

## Qualite et maintenance

Pratiques recommandees :
- controllers minces, logique metier dans les services
- evolution des DTOs/validators synchronisee avec les endpoints
- interfaces applicatives stables avant implementation infrastructure
- couverture de tests sur les composants critiques

Ressources techniques :
- `backend/README.md`
- `frontend/README.md`
- `backoffice/README.md`
- `backend/AUDIT_BACKEND.md`
- `docs/AUTH_SYSTEM.md`

## Perimetre actif

Le dossier `payzen - old` est un historique et ne fait pas partie du parcours de developpement courant.

## Licence

Projet prive - Tous droits reserves.
