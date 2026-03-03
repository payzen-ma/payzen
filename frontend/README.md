# Payzen Frontend

Interface utilisateur principale de la plateforme Payzen, destinée aux responsables RH, gestionnaires de paie et employés. Développée avec **Angular 20** et **PrimeNG**.

---

## Stack technique

| Outil | Version |
|---|---|
| Angular | ^20.3.0 |
| PrimeNG | ^20.4.0 |
| TailwindCSS | ^4.x (via PostCSS) |
| ngx-translate | ^17.0.0 |
| Chart.js | ^4.5.1 |
| RxJS | ~7.8.0 |

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
cd frontend
npm install
```

---

## Démarrage

```bash
# Serveur de développement
npm start
# ou
ng serve
```

Application disponible sur `http://localhost:4200`.

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
│   ├── core/              # Services singleton, guards, interceptors
│   ├── features/          # Modules fonctionnels
│   │   ├── auth/          # Connexion, inscription
│   │   ├── dashboard/     # Tableau de bord RH
│   │   ├── employees/     # Gestion des employés
│   │   ├── payroll/       # Fiches de paie, calculs
│   │   ├── leave/         # Demandes de congés
│   │   ├── overtime/      # Heures supplémentaires
│   │   ├── holidays/      # Jours fériés, calendrier
│   │   ├── reports/       # Rapports et exports
│   │   ├── salary-packages/ # Packages salariaux
│   │   ├── permissions/   # Gestion des permissions
│   │   ├── profile/       # Profil utilisateur
│   │   ├── company/       # Paramètres entreprise
│   │   ├── cabinet/       # Espace cabinet comptable
│   │   └── expert/        # Mode expert / simulation
│   ├── layouts/           # Layouts globaux (shell, auth)
│   ├── shared/            # Composants, pipes, directives réutilisables
│   └── app.routes.ts      # Routing principal
├── assets/
│   ├── i18n/              # Fichiers de traduction (FR, EN, AR)
│   ├── styles/            # Styles globaux
│   └── themes/            # Thèmes PrimeNG personnalisés
└── environments/
    ├── environment.ts         # Variables de développement
    └── environment.prod.ts    # Variables de production
```

---

## Internationalisation (i18n)

L'application supporte le multilingue via `@ngx-translate`. Les fichiers de traduction sont dans `src/assets/i18n/`.

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

## Thème & Design

- Design system basé sur **PrimeNG** avec thème personnalisé.
- Utilitaires CSS via **TailwindCSS v4**.
- Variables CSS documentées dans [`CSS_VARIABLES_GUIDE.md`](./CSS_VARIABLES_GUIDE.md).
- Intégration API Dashboard documentée dans [`DASHBOARD_API_INTEGRATION.md`](./DASHBOARD_API_INTEGRATION.md).
