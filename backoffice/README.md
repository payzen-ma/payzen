# Payzen Backoffice

Interface d'administration système de la plateforme Payzen, réservée aux super-administrateurs. Permet la gestion globale des entreprises clientes, des utilisateurs, des rôles, du référentiel de paie et des paramètres système. Développée avec **Angular 20**.

---

## Stack technique

| Outil | Version |
|---|---|
| Angular | ^20.3.0 |
| TailwindCSS | ^4.x (via PostCSS) |
| RxJS | ~7.8.0 |
| Angular CLI | ^20.3.13 |

---

## Prérequis

- Node.js >= 20
- npm >= 10
- Angular CLI >= 20

```bash
npm install -g @angular/cli
```

---

## Installation

```bash
cd backoffice
npm install
```

---

## Démarrage

```bash
npm start
# ou
ng serve
```

Application disponible sur `http://localhost:4200`.

> **Note :** Si le frontend tourne déjà sur le port 4200, configurer un port différent dans `angular.json` :
> ```json
> "serve": { "options": { "port": 4300 } }
> ```

---

## Scripts disponibles

| Commande | Description |
|---|---|
| `npm start` | Lance le serveur de développement |
| `npm run build` | Build de production |
| `npm run watch` | Build en mode watch (développement) |
| `npm test` | Lance les tests unitaires (Karma) |

---

## Structure du projet

```
src/
├── app/
│   ├── config/            # Configuration globale de l'app
│   ├── features/          # Modules fonctionnels
│   │   ├── auth/          # Authentification super-admin
│   │   ├── dashboard/     # Tableau de bord système
│   │   ├── users/         # Gestion des utilisateurs
│   │   ├── roles/         # Gestion des rôles
│   │   ├── permissions/   # Gestion des permissions
│   │   ├── company/       # Gestion des entreprises clientes
│   │   ├── payroll-referentiel/ # Référentiel de paie (éléments, règles)
│   │   ├── referentiel/   # Référentiels généraux
│   │   ├── salary-packages/ # Packages salariaux
│   │   ├── holidays/      # Gestion des jours fériés
│   │   ├── settings/      # Paramètres système
│   │   ├── analytics/     # Analytiques & statistiques
│   │   ├── audit-logs/    # Journaux d'audit
│   │   └── event-log/     # Journal des événements
│   ├── guards/            # Guards d'authentification et d'autorisation
│   ├── i18n/              # Internationalisation
│   ├── interceptors/      # Intercepteurs HTTP (auth, erreurs)
│   ├── layouts/           # Layouts globaux
│   ├── models/            # Interfaces et modèles TypeScript
│   ├── pages/             # Pages standalone (404, etc.)
│   ├── services/          # Services HTTP
│   └── shared/            # Composants réutilisables
└── environments/
    ├── environment.ts         # Variables de développement
    └── environment.prod.ts    # Variables de production
```

---

## Fonctionnalités principales

| Module | Description |
|---|---|
| **Utilisateurs** | CRUD complet, assignation de rôles |
| **Rôles & Permissions** | Création de rôles avec permissions granulaires |
| **Entreprises** | Gestion des tenants / sociétés clientes |
| **Référentiel paie** | Éléments de paie, règles DSL, convergence |
| **Packages salariaux** | Définition et gestion des grilles salariales |
| **Audit Logs** | Traçabilité complète des actions |
| **Analytiques** | Statistiques d'usage et de performance |
| **Paramètres système** | Configuration globale de la plateforme |

---

## Configuration des environnements

`src/environments/environment.ts` :
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000/api'
};
```

---

## Build de production

```bash
npm run build
```

Les fichiers compilés sont générés dans `dist/`.

---

## Tests

```bash
npm test
```

Les tests utilisent **Karma** + **Jasmine**.

---

## Accès

Le backoffice est réservé aux **super-administrateurs** Payzen. L'accès nécessite des credentials spécifiques distincts des comptes entreprise.
