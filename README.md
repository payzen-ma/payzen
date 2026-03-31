# Payzen - Plateforme RH & Paie

Payzen est une plateforme de gestion RH et paie composee de 4 briques principales :
- une application metier RH/employe (`frontend`)
- une application d'administration plateforme (`backoffice`)
- une API backend .NET en architecture en couches (`backend`)
- une landing page publique statique/PHP (`landingpage`)

## Architecture du repository

```text
payzen/
├── backend/          # API ASP.NET Core + EF Core + SQL Server
├── frontend/         # Application Angular RH / employe
├── backoffice/       # Application Angular admin plateforme
├── landingpage/      # Site public (HTML/CSS/JS/PHP)
├── docs/             # Documentation technique transverse
├── documentation/    # Documentation complementaire
└── payzen - old/     # Ancien code/archive (hors parcours principal)
```

## Fonctionnalites couvertes

- Authentification Entra External ID + JWT interne Payzen
- Gestion entreprises, employes, roles et permissions
- Gestion conges, absences, heures supplementaires
- Paie: calcul, simulation, exports
- Dashboard RH et suivi d'activite
- Backoffice super-admin (tenants, referentiels, audit, configuration)

## Stack technique

| Couche | Technologies |
|---|---|
| Frontend RH | Angular 20, PrimeNG, Tailwind |
| Backoffice | Angular 20, Tailwind |
| Backend API | ASP.NET Core `net10.0`, EF Core, JWT |
| Base de donnees | SQL Server |
| Authentification | Microsoft Entra External ID + MSAL |
| Documents / export | IronPDF, ClosedXML |

## Prerequis

- [Node.js](https://nodejs.org/) >= 20
- npm >= 10
- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/sql-server) (LocalDB, Express ou Standard)

## Demarrage local rapide

### 1) Backend API

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project Payzen.Api
```

API locale: `http://localhost:5119` (selon profil de lancement)

> Config locale: copier `backend/Payzen.Api/appsettings.Development.example.json` vers `backend/Payzen.Api/appsettings.Development.json`, puis renseigner les valeurs sensibles.

### 2) Frontend RH

```bash
cd frontend
npm install
npm start
```

Frontend local: `http://localhost:4200`

### 3) Backoffice

```bash
cd backoffice
npm install
npm start
```

Backoffice local: `http://localhost:50171`

### 4) Landing page (optionnel)

La landing page se trouve dans `landingpage` (`index.html` / `index.php`).
Tu peux la servir avec un serveur web local (IIS, Apache, Nginx, PHP built-in server, etc.).

## Scripts utiles

### Frontend (`frontend`)
- `npm start` : serveur dev
- `npm run build` : build production
- `npm test` : tests unitaires

### Backoffice (`backoffice`)
- `npm start` : serveur dev (port 50171)
- `npm run build` : build production
- `npm test` : tests unitaires

### Backend (`backend`)
- `dotnet run --project Payzen.Api` : lance l'API
- `dotnet test` : execute les tests
- `dotnet ef database update --project Payzen.Infrastructure --startup-project Payzen.Api` : applique les migrations

## Configuration & securite

- Ne pas commiter de secrets (`appsettings.Development.json`, cles JWT, secrets Entra, chaines SQL sensibles).
- Utiliser User Secrets .NET, variables d'environnement, ou un coffre de secrets.
- Verifier les `redirectUri` Entra pour `frontend` et `backoffice`.

## Documentation

- [README Backend](./backend/README.md)
- [README Frontend](./frontend/README.md)
- [README Backoffice](./backoffice/README.md)
- [Auth Entra External ID](./docs/AUTH_SYSTEM.md)
- [Audit Backend](./backend/AUDIT_BACKEND.md)

## Notes

- Le dossier `payzen - old` contient des elements historiques et n'est pas le chemin principal de developpement.
- Ce README decrit le socle actif du projet a la date courante.

## Licence

Projet prive - Tous droits reserves.
