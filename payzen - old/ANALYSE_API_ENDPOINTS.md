# Analyse des API Endpoints - Frontend & Backoffice vs Backend

**Date d'analyse :** Mars 2026  
**Projet :** PayZen

---

## 📊 Résumé Exécutif

| Application | Endpoints Utilisés | Endpoints Backend Total | Couverture |
|-------------|-------------------|------------------------|------------|
| **Frontend** | ~85 | ~397 | ~21% |
| **Backoffice** | ~55 | ~397 | ~14% |
| **Total Utilisés** | ~110 (uniques) | ~397 | ~28% |
| **Endpoints NON Utilisés** | **~287** | - | ~72% |

---

## 🎯 Endpoints Backend Disponibles (par Route)

### Routes Backend Complètes (68 contrôleurs)

| Route | Domaine | # Endpoints |
|-------|---------|-------------|
| `api/auth` | Authentification | ~5 |
| `api/permissions` | Système | ~5 |
| `api/roles` | Système | ~5 |
| `api/roles-permissions` | Système | ~5 |
| `api/users` | Système | ~6 |
| `api/users-roles` | Système | ~8 |
| `api/companies` | Entreprise | ~10 |
| `api/companydocuments` | Entreprise | ~6 |
| `api/departements` | Entreprise | ~5 |
| `api/job-positions` | Entreprise | ~5 |
| `api/dashboard` | Dashboard | ~3 |
| `api/dashboard/employees` | Dashboard | ~1 |
| `api/dashboard/expert` | Dashboard | ~1 |
| `api/dashboard/hr` | Dashboard | ~2 |
| `api/absences` | Employé | ~14 |
| `api/employee-addresses` | Employé | ~6 |
| `api/employee-attendance-break` | Employé | ~7 |
| `api/employee-attendance` | Employé | ~8 |
| `api/employee-categories` | Employé | ~5 |
| `api/employees/{id}/children` | Employé | ~5 |
| `api/employee-contracts` | Employé | ~6 |
| `api/employee` | Employé | ~25 |
| `api/employee-documents` | Employé | ~6 |
| `api/employee-overtimes` | Employé | ~7 |
| `api/employee-salaries` | Employé | ~6 |
| `api/employee-salary-components` | Employé | ~5 |
| `api/employees/{id}/spouse` | Employé | ~4 |
| `api/salary-package-assignments` | Employé | ~8 |
| `api/events` | Événements | ~1 |
| `api/leave-audit-logs` | Congés | ~4 |
| `api/leave-balances` | Congés | ~10 |
| `api/leave-carryover-agreements` | Congés | ~5 |
| `api/leave-request-approval-history` | Congés | ~2 |
| `api/leave-request-attachments` | Congés | ~5 |
| `api/leave-requests` | Congés | ~12 |
| `api/leave-request-exemptions` | Congés | ~5 |
| `api/leave-types` | Congés | ~5 |
| `api/leave-type-legal-rules` | Congés | ~5 |
| `api/leave-type-policies` | Congés | ~5 |
| `api/claudesimulation` | Paie/LLM | ~4 |
| `api/payroll` | Paie | ~6 |
| `api/payroll/exports` | Paie | ~3 |
| `api/payslip` | Paie | ~1 |
| `api/salary-preview` | Paie | ~5 |
| `api/payroll/anciennete-rate-sets` | Paie/Réf | ~11 |
| `api/payroll/authorities` | Paie/Réf | ~3 |
| `api/business-sectors` | Paie/Réf | ~5 |
| `api/payroll/element-categories` | Paie/Réf | ~3 |
| `api/payroll/element-rules` | Paie/Réf | ~10+ |
| `api/payroll/eligibility-criteria` | Paie/Réf | ~5 |
| `api/payroll/legal-parameters` | Paie/Réf | ~5 |
| `api/payroll/referentiel-elements` | Paie/Réf | ~10+ |
| `api/education-levels` | Référentiel | ~5 |
| `api/genders` | Référentiel | ~5 |
| `api/legal-contract-types` | Référentiel | ~5 |
| `api/marital-statuses` | Référentiel | ~5 |
| `api/overtime-rate-rules` | Référentiel | ~6 |
| `api/state-employment-programs` | Référentiel | ~5 |
| `api/statuses` | Référentiel | ~5 |
| `api/cities` | Système | ~6 |
| `api/contract-types` | Système | ~6 |
| `api/countries` | Système | ~5 |
| `api/holidays` | Système | ~7 |
| `api/pay-components` | Système | ~9 |
| `api/salary-packages` | Système | ~13 |
| `api/working-calendar` | Système | ~6 |

---

## ✅ Endpoints UTILISÉS par le Frontend

### Authentification & Utilisateurs
| Endpoint | Méthode | Service Frontend |
|----------|---------|------------------|
| `api/auth/login` | POST | ✅ AuthService |
| `api/auth/logout` | POST | ✅ AuthService |
| `api/users` | GET/POST/PUT/DELETE | ✅ UserService |
| `api/users-roles` | GET/POST | ✅ PermissionManagementService |
| `api/users-roles/user/{userId}` | GET | ✅ PermissionManagementService |
| `api/users-roles/role/{roleId}` | GET | ✅ PermissionManagementService |
| `api/users-roles/bulk-assign` | POST | ✅ PermissionManagementService |
| `api/users-roles/replace` | PUT | ✅ PermissionManagementService |
| `api/roles` | GET/POST/PUT/DELETE | ✅ PermissionManagementService |
| `api/permissions` | GET/POST/PUT/DELETE | ✅ PermissionManagementService |
| `api/roles-permissions/role/{roleId}` | GET | ✅ PermissionManagementService |
| `api/roles-permissions/permission/{id}` | GET | ✅ PermissionManagementService |
| `api/roles-permissions` | POST/DELETE | ✅ PermissionManagementService |

### Entreprise
| Endpoint | Méthode | Service Frontend |
|----------|---------|------------------|
| `api/companies/{id}` | GET/PATCH | ✅ CompanyService |
| `api/companies/managedby/{id}` | GET | ✅ CompanyService |
| `api/companies/create-by-expert` | POST | ✅ CompanyService |
| `api/companies/{id}/history` | GET | ✅ CompanyService |
| `api/company/logo` | POST | ✅ CompanyService |
| `api/cities` | GET | ✅ CompanyService, EmployeeService |
| `api/countries` | GET/POST | ✅ EmployeeService |
| `api/departements` | GET/POST/PUT/DELETE | ✅ DepartmentService |
| `api/departements/company/{id}` | GET | ✅ DepartmentService |
| `api/job-positions` | GET | ✅ JobPositionService |
| `api/companydocuments` | GET/POST/PUT/DELETE | ✅ CompanyDocumentService |
| `api/companydocuments/company/{id}` | GET | ✅ CompanyDocumentService |
| `api/companydocuments/{id}/download` | GET | ✅ CompanyDocumentService |

### Dashboard
| Endpoint | Méthode | Service Frontend |
|----------|---------|------------------|
| `api/dashboard/employees` | GET | ✅ DashboardService |
| `api/dashboard/expert/summary` | GET | ✅ DashboardService |
| `api/dashboard/hr` | GET | ✅ DashboardHrRepository |
| `api/dashboard/hr/raw` | GET | ✅ DashboardHrRepository |

### Employé
| Endpoint | Méthode | Service Frontend |
|----------|---------|------------------|
| `api/employee/summary` | GET | ✅ EmployeeService |
| `api/employee/me` | GET | ✅ EmployeeController (profile) |
| `api/employee/current` | GET | ✅ EmployeeService |
| `api/employee/company/{id}` | GET | ✅ EmployeeService, UserService |
| `api/employee/{id}` | GET/PUT/PATCH/DELETE | ✅ EmployeeService |
| `api/employee-contracts/employee/{id}` | GET | ✅ EmployeeService |
| `api/employee-salaries/employee/{id}` | GET | ✅ EmployeeService |
| `api/employee-salary-components` | POST/PUT/DELETE | ✅ EmployeeService |
| `api/employee-salary-components/salary/{id}` | GET | ✅ EmployeeService |
| `api/employee/{id}/documents` | POST | ✅ EmployeeService |
| `api/employees/{id}/spouse` | POST/PUT | ✅ EmployeeService |
| `api/employees/{id}/children` | POST/PUT | ✅ EmployeeService |
| `api/employee-categories` | GET | ✅ EmployeeCategoryService |
| `api/employee-attendance/employee/{id}` | GET | ✅ Attendance Component |
| `api/employee-attendance/check-in` | POST | ✅ Attendance Component |
| `api/employee-attendance/check-out` | POST | ✅ Attendance Component |
| `api/employee-overtimes` | GET/POST/PUT/DELETE | ✅ OvertimeService |
| `api/salary-package-assignments/employee/{id}` | GET | ✅ EmployeeService |
| `api/statuses` | GET | ✅ EmployeeService |

### Congés
| Endpoint | Méthode | Service Frontend |
|----------|---------|------------------|
| `api/leave-types` | GET/POST/PUT/DELETE | ✅ LeaveService |
| `api/leave-type-policies` | GET | ⚠️ Partiellement utilisé |
| `api/leave-type-legal-rules` | GET/POST/PUT/DELETE | ✅ LeaveTypeLegalRuleService |
| `api/leave-requests` | GET/POST/PUT/DELETE | ✅ LeaveRequestService |
| `api/leave-requests/{id}` | GET/PUT/DELETE | ✅ LeaveRequestService |
| `api/leave-requests/employee/{id}` | GET | ✅ LeaveRequestService |
| `api/leave-requests/pending-approval` | GET | ✅ LeaveRequestService |
| `api/leave-requests/create-for-employee/{id}` | POST | ✅ LeaveRequestService |
| `api/leave-requests/{id}/submit` | POST | ✅ LeaveRequestService |
| `api/leave-requests/{id}/approve` | POST | ✅ LeaveRequestService |
| `api/leave-requests/{id}/reject` | POST | ✅ LeaveRequestService |
| `api/leave-requests/{id}/cancel` | POST | ✅ LeaveRequestService |
| `api/leave-requests/{id}/renounce` | POST | ✅ LeaveRequestService |

### Paie
| Endpoint | Méthode | Service Frontend |
|----------|---------|------------------|
| `api/payroll/calculate` | POST | ✅ PayrollService |
| `api/payroll/results` | GET | ✅ PayrollService |
| `api/payroll/results/{id}` | GET/DELETE | ✅ PayrollService |
| `api/payroll/recalculate/{id}` | POST | ✅ PayrollService |
| `api/payslip/employee/{id}/period/{y}/{m}` | GET | ✅ PayrollService |
| `api/payroll/exports/journal/{c}/{y}/{m}` | GET | ✅ PayrollExportService |
| `api/payroll/exports/cnss/{c}/{y}/{m}` | GET | ✅ PayrollExportService |
| `api/payroll/exports/ir/{c}/{y}/{m}` | GET | ✅ PayrollExportService |
| `api/claudesimulation/simulate` | POST | ✅ SalarySimulationService |
| `api/salary-packages` | GET/POST/PUT/DELETE | ✅ SalaryPackageService |
| `api/salary-packages/{id}/clone` | POST | ✅ SalaryPackageService |
| `api/salary-packages/{id}/publish` | POST | ✅ SalaryPackageService |
| `api/pay-components` | GET | ✅ SalaryPackageService |

### Référentiels
| Endpoint | Méthode | Service Frontend |
|----------|---------|------------------|
| `api/genders` | GET/POST/PUT/DELETE | ✅ ReferenceDataService |
| `api/marital-statuses` | GET/POST/PUT/DELETE | ✅ ReferenceDataService |
| `api/education-levels` | GET/POST/PUT/DELETE | ✅ ReferenceDataService |
| `api/legal-contract-types` | GET | ✅ LegalContractTypeService |
| `api/state-employment-programs` | GET | ✅ StateEmploymentProgramService |
| `api/working-calendar` | GET/POST/PUT/DELETE | ✅ WorkingCalendarService |
| `api/working-calendar/company/{id}` | GET | ✅ WorkingCalendarService |

---

## ✅ Endpoints UTILISÉS par le Backoffice

### Authentification & Admin
| Endpoint | Unique au Backoffice |
|----------|---------------------|
| `api/auth/login` | Non (partagé) |
| `api/auth/logout` | Non (partagé) |
| `api/companies` | ✅ Liste complète |
| `api/companies/search` | ✅ Recherche avancée |
| `api/companies/cabinets-experts` | ✅ Unique |
| `api/companies/form-data` | ✅ Unique |
| `api/dashboard/summary` | ✅ Backoffice uniquement |
| `api/metrics/usage` | ✅ Backoffice uniquement |
| `api/metrics/revenue` | ✅ Backoffice uniquement |
| `api/events` | ✅ Logs événements |
| `api/permissions/by-resource` | ✅ Unique |
| `api/roles/{roleId}/users` | ✅ Unique |
| `api/roles-permissions/bulk-assign` | ✅ Admin |
| `api/employee/{id}/status` | PATCH ✅ |
| `api/holidays` | GET/POST/PUT/DELETE ✅ |
| `api/holidays/check` | ✅ Vérification |
| `api/holidays/types` | ✅ Types enum |

### Référentiels (Admin complet)
| Endpoint | CRUD Complet |
|----------|--------------|
| `api/genders` | ✅ |
| `api/marital-statuses` | ✅ |
| `api/education-levels` | ✅ |
| `api/statuses` | ✅ |
| `api/legal-contract-types` | ✅ |
| `api/leave-types` | ✅ |
| `api/leave-type-legal-rules` | ✅ |
| `api/state-employment-programs` | ✅ |
| `api/payroll/*` | ✅ Réf. paie |

---

## ❌ Endpoints NON UTILISÉS (~287 endpoints)

### 🔴 Domaine ABSENCES (Non utilisé ~14 endpoints)
| Endpoint | Commentaire |
|----------|-------------|
| `api/absences` | **Entièrement non utilisé** |
| `api/absences/stats` | Statistiques absences |
| `api/absences/{id}` | CRUD individuel |
| `api/absences/{id}/submit` | Workflow |
| `api/absences/{id}/decision` | Décision |
| `api/absences/{id}/approve` | Approbation |
| `api/absences/{id}/reject` | Rejet |
| `api/absences/{id}/cancel` | Annulation |
| `api/absences/types` | Types d'absence |

**Impact :** Fonctionnalité gestion des absences complètement absente du frontend

### 🔴 Domaine LEAVE (Partiellement non utilisé)
| Endpoint | Commentaire |
|----------|-------------|
| `api/leave-balances` | **Non utilisé** - Soldes congés |
| `api/leave-balances/employee/{id}/year/{y}` | Solde annuel |
| `api/leave-balances/employee/{id}/year/{y}/month/{m}` | Solde mensuel |
| `api/leave-balances/summary/{id}` | Résumé |
| `api/leave-balances/recalculate/{id}/{y}/{m}` | Recalcul |
| `api/leave-audit-logs` | **Non utilisé** - Audit |
| `api/leave-audit-logs/by-employee/{id}` | Par employé |
| `api/leave-audit-logs/by-request/{id}` | Par demande |
| `api/leave-carryover-agreements` | **Non utilisé** - Report |
| `api/leave-request-approval-history` | **Non utilisé** - Historique |
| `api/leave-request-attachments` | **Non utilisé** - Pièces jointes |
| `api/leave-request-exemptions` | **Non utilisé** - Exemptions |
| `api/leave-type-policies` | **Non utilisé** - Politiques |

**Impact :** Gestion avancée des congés (soldes, reports, audit) non disponible

### 🔴 Domaine EMPLOYEE (Partiellement non utilisé)
| Endpoint | Commentaire |
|----------|-------------|
| `api/employee-addresses` | CRUD adresses - **Non utilisé directement** |
| `api/employee-attendance-break` | Pauses pointage - **Non utilisé** |
| `api/employee-attendance-break/total-break-time` | Temps pause |
| `api/employee-documents` | Route directe - **Non utilisé** |
| Messages spécifiques | Diverses routes orphelines |

### 🔴 Domaine PAYROLL (Partiellement non utilisé)
| Endpoint | Commentaire |
|----------|-------------|
| `api/payroll/stats` | **Non utilisé** - Statistiques paie |
| `api/salary-preview/*` | **Non utilisé** - Preview composants |
| `api/salary-preview/seniority-bonus` | Prime ancienneté |
| `api/salary-preview/cnss` | Calcul CNSS |
| `api/salary-preview/ir` | Calcul IR |
| `api/salary-preview/professional-expenses` | Frais pro |
| `api/claudesimulation/simulate-quick` | **Non utilisé** |
| `api/claudesimulation/simulate-stream` | **Non utilisé** - Streaming |
| `api/claudesimulation/rules` | **Non utilisé** |
| `api/salary-packages/templates` | **Non utilisé** |
| `api/salary-packages/{id}/new-version` | Versioning |
| `api/salary-packages/{id}/deprecate` | Dépréciation |
| `api/salary-packages/{id}/duplicate` | Duplication |
| `api/pay-components/effective` | Composants effectifs |
| `api/pay-components/code/{code}` | Par code |
| `api/pay-components/{id}/new-version` | Versioning |
| `api/pay-components/{id}/deactivate` | Désactivation |

### 🔴 Domaine PAYROLL REFERENTIEL (Peu utilisé)
| Endpoint | Commentaire |
|----------|-------------|
| `api/payroll/anciennete-rate-sets` | **Non utilisé frontend** |
| `api/payroll/anciennete-rate-sets/current` | Taux courants |
| `api/payroll/anciennete-rate-sets/company/{id}` | Par entreprise |
| `api/payroll/anciennete-rate-sets/company/{id}/customize` | Personnalisation |
| `api/payroll/anciennete-rate-sets/validate` | Validation |
| `api/payroll/anciennete-rate-sets/{id}/calculate` | Calcul |
| `api/payroll/authorities` | **Non utilisé** |
| `api/payroll/element-categories` | **Backoffice uniquement** |
| `api/payroll/element-rules` | **Backoffice uniquement** |
| `api/payroll/eligibility-criteria` | **Non utilisé** |
| `api/payroll/legal-parameters` | **Non utilisé** |
| `api/payroll/referentiel-elements` | **Backoffice uniquement** |
| `api/business-sectors` | **Backoffice uniquement** |

### 🔴 Domaine SYSTÈME (Non utilisé)
| Endpoint | Commentaire |
|----------|-------------|
| `api/contract-types` | **Non utilisé** - Types contrat système |
| `api/contract-types/by-company/{id}` | Par entreprise |
| `api/overtime-rate-rules` | **Non utilisé** - Règles heures sup |
| `api/overtime-rate-rules/categories` | Catégories |

### 🟡 Endpoints Backoffice Non Existants
| Endpoint Appelé | Status Backend |
|-----------------|----------------|
| `api/metrics/usage` | ❌ N'existe PAS |
| `api/metrics/revenue` | ❌ N'existe PAS |
| `api/company/search` | ❌ N'existe PAS |
| `api/company/cabinets-experts` | ❌ N'existe PAS |

---

## 📊 Analyse par Domaine

| Domaine | Backend | Frontend | Backoffice | Non Utilisé |
|---------|---------|----------|------------|-------------|
| Auth | 29 | 25 | 20 | ~4 |
| Company | 26 | 18 | 15 | ~8 |
| Dashboard | 7 | 4 | 3 | ~2 |
| Employee | 95 | 35 | 10 | **~50** |
| Leave | 53 | 15 | 8 | **~30** |
| Payroll | 65 | 15 | 20 | **~30** |
| Référentiel | 50 | 12 | 25 | **~13** |
| Système | 52 | 10 | 12 | **~30** |
| **TOTAL** | ~397 | ~110 | ~85 | **~170** |

---

## 🎯 Recommandations

### 1. Endpoints à SUPPRIMER (Dead Code Backend)
Ces endpoints n'ont aucun appel frontend/backoffice et semblent non nécessaires :
- `api/metrics/*` (appelé mais inexistant)
- `api/company/search`, `api/company/cabinets-experts` (appelé mais inexistant)

### 2. Fonctionnalités à IMPLÉMENTER côté Frontend
| Endpoint | Valeur Métier |
|----------|---------------|
| `api/absences/*` | **HAUTE** - Gestion absences complète |
| `api/leave-balances/*` | **HAUTE** - Soldes congés visibles |
| `api/leave-audit-logs/*` | MOYENNE - Traçabilité |
| `api/salary-preview/*` | **HAUTE** - Simulation en temps réel |
| `api/overtime-rate-rules/*` | MOYENNE - Config heures sup |
| `api/employee-addresses/*` | BASSE - Gestion adresses |

### 3. Endpoints à CRÉER côté Backend
| Endpoint Manquant | Application |
|-------------------|-------------|
| `api/metrics/usage` | Backoffice |
| `api/metrics/revenue` | Backoffice |
| `api/companies/search` | Backoffice |
| `api/companies/cabinets-experts` | Backoffice |

### 4. Cohérence API
- Standardiser les routes (`api/employee-*` vs `api/employees/{id}/*`)
- Documenter OpenAPI pour tous les endpoints
- Ajouter versioning `/api/v1/`

---

## 📈 Score d'Utilisation API

| Métrique | Valeur |
|----------|--------|
| Endpoints Backend | 397 |
| Endpoints Utilisés (Total) | ~110 |
| **Taux d'utilisation** | **~28%** |
| Endpoints Orphelins | ~287 |
| Endpoints Appelés mais Inexistants | 4 |

---

## 🏗️ Structure Frontend

```
payzen/frontend/src/app/
├── core/
│   ├── guards/
│   ├── interceptors/
│   │   └── auth.interceptor.ts
│   ├── models/
│   ├── services/              # ~35 services
│   │   ├── auth.service.ts       → api/auth/*
│   │   ├── company.service.ts    → api/companies/*
│   │   ├── dashboard.service.ts  → api/dashboard/*
│   │   ├── employee.service.ts   → api/employee/*
│   │   ├── leave.service.ts      → api/leave-types/*
│   │   ├── leave-request.service.ts → api/leave-requests/*
│   │   ├── payroll.service.ts    → api/payroll/*
│   │   ├── permission-management.service.ts → api/roles/*, api/permissions/*
│   │   ├── reference-data.service.ts → api/genders/*, api/marital-statuses/*
│   │   ├── salary-package.service.ts → api/salary-packages/*
│   │   ├── user.service.ts       → api/users/*
│   │   └── working-calendar.service.ts → api/working-calendar/*
│   └── utils/
├── features/
│   ├── dashboard/
│   ├── employees/
│   ├── leave/
│   ├── payroll/
│   ├── permissions/
│   ├── reports/
│   └── salary-packages/
├── layouts/
└── shared/
```

## 🏗️ Structure Backoffice

```
payzen/backoffice/src/app/
├── config/
├── features/
├── guards/
├── i18n/
├── interceptors/
├── layouts/
├── models/
├── pages/
├── services/              # ~15 services
│   ├── auth.service.ts       → api/auth/*
│   ├── company.service.ts    → api/companies/*
│   ├── dashboard.service.ts  → api/dashboard/summary
│   ├── event-log.service.ts  → api/events/*
│   ├── gender.service.ts     → api/genders/*
│   ├── holiday.service.ts    → api/holidays/*
│   ├── permission.service.ts → api/permissions/*
│   ├── referentiel.service.ts → Tous les référentiels
│   ├── role.service.ts       → api/roles/*
│   ├── salary-package.service.ts → api/salary-packages/*
│   ├── user.service.ts       → api/employee/* (backoffice = admin users)
│   └── payroll-referentiel/  → api/payroll/*
└── shared/
```

---

*Rapport généré automatiquement - Mars 2026*
