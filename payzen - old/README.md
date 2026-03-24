# Payzen — Plateforme de Gestion RH & Paie

Payzen est une solution complète de gestion des ressources humaines et de la paie, pensée pour les entreprises marocaines. Elle couvre l'ensemble du cycle RH : onboarding des entreprises, gestion des employés, calcul de la paie (avec moteur de règles DSL), gestion des congés, des absences et des heures supplémentaires.

---

## Architecture du projet

```
payzen/
├── frontend/          # Application Angular — Interface employé & RH
├── backoffice/        # Application Angular — Interface administrateur système
├── payzen_backend/    # API ASP.NET Core 9 — Logique métier & base de données
└── landingpage/       # Landing page PHP/HTML publique
```

---

## Modules fonctionnels

| Domaine | Description |
|---|---|
| **Authentification** | JWT, rôles & permissions granulaires |
| **Gestion des entreprises** | Onboarding, paramétrage, multi-entités |
| **Gestion des employés** | Profils, contrats, catégories, postes |
| **Paie** | Moteur de calcul DSL, fiches de paie, exports Excel |
| **Congés & Absences** | Demandes, soldes, calendrier de travail |
| **Heures supplémentaires** | Suivi et valorisation |
| **Tableau de bord RH** | Indicateurs, statistiques en temps réel |
| **Référentiel** | Éléments de paie, règles de convergence |
| **IA (LLM)** | Assistance au calcul via Claude / Gemini |

---

## Stack technique

| Couche | Technologie |
|---|---|
| Frontend & Backoffice | Angular 20, PrimeNG, TailwindCSS |
| Backend | ASP.NET Core 9, Entity Framework Core 9 |
| Base de données | SQL Server |
| Authentification | JWT Bearer |
| IA / LLM | Anthropic Claude, Google Gemini |
| Exports | ClosedXML (Excel) |

---

## Prérequis

- [Node.js](https://nodejs.org/) >= 20
- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/fr-fr/sql-server/) (Express ou supérieur)
- [Angular CLI](https://angular.io/cli) >= 20

---

## Démarrage rapide

### 1. Backend

```bash
cd payzen_backend/payzen_backend
# Configurer la chaîne de connexion dans appsettings.json
dotnet run
```

L'API sera disponible sur `https://localhost:7xxx` et `http://localhost:5xxx`.

### 2. Frontend (interface RH / employé)

```bash
cd frontend
npm install
npm start
```

Application disponible sur `http://localhost:4200`.

### 3. Backoffice (interface administrateur)

```bash
cd backoffice
npm install
npm start
```

Application disponible sur `http://localhost:4200` (port configurable dans `angular.json`).

---

## Sous-projets

- [Frontend — Guide complet](./frontend/README.md)
- [Backend — Guide complet](./payzen_backend/README.md)
- [Backoffice — Guide complet](./backoffice/README.md)

---

## Licence

Projet privé — Tous droits réservés.
