import { Routes } from '@angular/router';
import { MainLayout } from './layouts/main-layout/main-layout';
import { AuthLayout } from './layouts/auth-layout/auth-layout';
import { Dashboard } from './features/dashboard/dashboard';
import { EmployeesPage } from './features/employees/employees';
import { EmployeeProfile } from './features/employees/profile/employee-profile';
import { EmployeeCreatePage } from './features/employees/create/employee-create';
import { LoginComponent } from './features/auth/login/login.component';
import { 
  authGuard, 
  guestGuard, 
  rhGuard, 
  contextGuard, 
  contextSelectionGuard,
  expertModeGuard,
  standardModeGuard,
  viewPresenceGuard,
  viewAbsenceGuard
} from '@app/core/guards/auth.guard';
import { unsavedChangesGuard } from '@app/core/guards/unsaved-changes.guard';

export const routes: Routes = [
  // ============================================
  // AUTH ROUTES (Public)
  // ============================================
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login.component')
      .then(m => m.LoginComponent)
  },
  {
    path: 'auth/callback',
    loadComponent: () => import('./features/auth/entra-callback/entra-callback.component')
      .then(m => m.EntraCallbackComponent)
  },
  {
    path: 'auth/accept-invite',
    loadComponent: () => import('./features/auth/accept-invite/accept-invite.component')
      .then(m => m.AcceptInviteComponent)
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard')
      .then(m => m.Dashboard),
    canActivate: [authGuard]
  },
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },

  // ============================================
  // CONTEXT SELECTION (Post-login, pre-dashboard)
  // ============================================
  {
    path: 'select-context',
    canActivate: [authGuard, contextSelectionGuard],
    loadComponent: () => 
      import('./features/auth/context-selection/context-selection')
        .then(m => m.ContextSelectionPage),
    title: 'Select Workspace - PayZen'
  },

  // ============================================
  // STANDARD MODE ROUTES (/app/*)
  // ============================================
  {
    path: 'app',
    component: MainLayout,
    canActivate: [authGuard, contextGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        component: Dashboard
      },
      {
        path: 'employee/dashboard',
        loadComponent: () => import('./features/employees/dashboard/employee-dashboard.component').then(m => m.EmployeeDashboardComponent),
        title: 'Mon Espace - PayZen'
      },
      {
        path: 'company',
        loadComponent: () => import('./features/company/company.component').then(m => m.CompanyComponent),
        canActivate: [rhGuard]
      },
      {
        path: 'employees',
        component: EmployeesPage,
        canActivate: [rhGuard]
      },
      {
        path: 'attendance',
        loadComponent: () => import('./features/employees/attendance/attendance').then(m => m.AttendancePage),
        canActivate: [viewPresenceGuard]
      },
      {
        path: 'reports/attendance',
        loadComponent: () => import('./features/reports/attendance-report/attendance-report').then(m => m.AttendanceReportPage),
        canActivate: []
      },
      {
        path: 'absences',
        loadComponent: () => import('./features/employees/absences/employee-absences').then(m => m.EmployeeAbsencesComponent),
        canActivate: [viewAbsenceGuard]
      },
      {
        path: 'absences/team',
        loadComponent: () => import('./features/employees/absences/team-absences/team-absences').then(m => m.TeamAbsencesComponent),
        canActivate: [authGuard]
      },
      {
        path: 'absences/hr',
        loadComponent: () => import('./features/employees/absences/hr-absences/hr-absences').then(m => m.HrAbsencesComponent),
        canActivate: [rhGuard]
      },
      {
        path: 'absences/employee/:id',
        loadComponent: () => import('./features/employees/absences/employee-absence-detail/employee-absence-detail').then(m => m.EmployeeAbsenceDetailComponent),
        canActivate: [rhGuard]
      },
      {
        path: 'overtime',
        loadComponent: () => import('./features/overtime/overtime').then(m => m.OvertimeComponent),
        canActivate: [authGuard]
      },
      {
        path: 'overtime-management',
        loadComponent: () => import('./features/overtime/overtime-management/overtime-management').then(m => m.OvertimeManagementComponent),
        canActivate: [rhGuard]
      },
      {
        path: 'overtime/hr',
        loadComponent: () => import('./features/overtime/hr-employees/hr-employees').then(m => m.HrEmployeesComponent),
        canActivate: [rhGuard]
      },
      {
        path: 'overtime/detail/:id',
        loadComponent: () => import('./features/overtime/overtime-detail/overtime-detail').then(m => m.OvertimeDetailComponent),
        canActivate: [rhGuard]
      },
      {
        path: 'employees/create',
        component: EmployeeCreatePage,
        canActivate: [rhGuard]
      },
      {
        path: 'employees/:id',
        component: EmployeeProfile,
        canActivate: [rhGuard],
        canDeactivate: [unsavedChangesGuard]
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile').then(m => m.ProfileComponent)
      },
      {
        path: 'permissions',
        loadComponent: () => 
          import('./features/permissions/permission-management.component')
            .then(m => m.PermissionManagementComponent),
        canActivate: [rhGuard],
        title: 'Permission Management - PayZen'
      },
      // Salary Packages
      {
        path: 'salary-packages',
        loadComponent: () => 
          import('./features/salary-packages/salary-packages')
            .then(m => m.SalaryPackagesPage),
        canActivate: [rhGuard],
        title: 'Packages de Rémunération - PayZen'
      },
      {
        path: 'salary-packages/create',
        loadComponent: () =>
          import('./features/salary-packages/components/salary-package-create/salary-package-create')
            .then(m => m.SalaryPackageCreateComponent),
        canActivate: [rhGuard],
        title: 'Nouveau Package - PayZen'
      },
      {
        path: 'salary-packages/:id',
        loadComponent: () => 
          import('./features/salary-packages/components/salary-package-view/salary-package-view')
            .then(m => m.SalaryPackageViewComponent),
        canActivate: [rhGuard],
        title: 'Détails Package - PayZen'
      },
      {
        path: 'salary-packages/:id/edit',
        loadComponent: () =>
          import('./features/salary-packages/components/salary-package-create/salary-package-create')
            .then(m => m.SalaryPackageCreateComponent),
        canActivate: [rhGuard],
        title: 'Modifier Package - PayZen'
      },
      // Leave Management Routes
      {
        path: 'leave',
        children: [
          {
            path: '',
            loadComponent: () => import('./features/leave/leave.component').then(m => m.LeaveComponent),
            canActivate: [rhGuard]
          },
          {
            path: 'types',
            loadComponent: () => import('./features/leave/leave-types/leave-types').then(m => m.LeaveTypesPage),
            canActivate: [rhGuard]
          },
          {
            path: 'types/create',
            loadComponent: () => import('./features/leave/leave-types/leave-type-form/leave-type-form').then(m => m.LeaveTypeFormPage),
            canActivate: [rhGuard]
          },
          {
            path: 'types/:id',
            loadComponent: () => import('./features/leave/leave-types/leave-type-detail/leave-type-detail').then(m => m.LeaveTypeDetailPage),
            canActivate: [rhGuard]
          },
          {
            path: 'types/:id/edit',
            loadComponent: () => import('./features/leave/leave-types/leave-type-form/leave-type-form').then(m => m.LeaveTypeFormPage),
            canActivate: [rhGuard]
          },
          {
            path: 'policies',
            loadComponent: () => import('./features/leave/leave-policies/leave-policies').then(m => m.LeavePoliciesPage),
            canActivate: [rhGuard]
          },
          {
            path: 'policies/create',
            loadComponent: () => import('./features/leave/leave-policies/leave-policy-form/leave-policy-form').then(m => m.LeavePolicyFormPage),
            canActivate: [rhGuard]
          },
          {
            path: 'policies/configure/:leaveTypeId',
            loadComponent: () => import('./features/leave/leave-policies/leave-policy-form/leave-policy-form').then(m => m.LeavePolicyFormPage),
            canActivate: [rhGuard]
          },
          {
            path: 'policies/:id',
            loadComponent: () => import('./features/leave/leave-policies/leave-policy-form/leave-policy-form').then(m => m.LeavePolicyFormPage),
            canActivate: [rhGuard]
          },
          {
            path: 'legal-rules',
            loadComponent: () => import('./features/leave/leave-legal-rules/leave-legal-rules').then(m => m.LeaveLegalRulesPage),
            canActivate: [rhGuard]
          }
        ]
      },

      // Employee Leave Requests
      {
        path: 'my-leave-requests',
        loadComponent: () => import('./features/leave/leave-requests/leave-requests.component').then(m => m.LeaveRequestsComponent),
        title: 'Mes Demandes de Congé - PayZen'
      },

      // HR Leave Management
      {
        path: 'hr-leave-management',
        loadComponent: () => import('./features/leave/hr-leave-management/hr-leave-management.component').then(m => m.HrLeaveManagementComponent),
        title: 'Gestion des Congés RH - PayZen'
      },

      // Payroll - Bulletins de Paie
      {
        path: 'payroll/bulletin',
        loadComponent: () => import('./features/payroll/bulletin/bulletin.component').then(m => m.BulletinComponent),
        canActivate: [rhGuard],
        title: 'Bulletins de Paie - PayZen'
      },

      // Payroll - Import de pointage (heures de travail)
      {
        path: 'payroll/pointage-import',
        loadComponent: () => import('./features/payroll/pointage-import/pointage-import.component').then(m => m.PointageImportComponent),
        canActivate: [rhGuard],
        title: 'Import de Pointage - PayZen'
      },

      // Payroll - Liste des pointages
      {
        path: 'payroll/pointages',
        loadComponent: () => import('./features/payroll/pointage-list/pointage-list.component').then(m => m.PointageListComponent),
        canActivate: [rhGuard],
        title: 'Pointages - PayZen'
      },

      // Payroll - Fiche de Paie (accessible à tous les employés)
      {
        path: 'payroll/payslip',
        loadComponent: () => import('./features/payroll/payslip/payslip.component').then(m => m.PayslipComponent),
        canActivate: [authGuard],
        title: 'Ma Fiche de Paie - PayZen'
      },

      // Payroll - Exports (Journal, CNSS, IR)
      {
        path: 'payroll/exports',
        loadComponent: () => import('./features/payroll/exports/payroll-exports.component').then(m => m.PayrollExportsComponent),
        canActivate: [rhGuard],
        title: 'Exports de Paie - PayZen'
      },

      // Payroll - Simulation de Paie
      {
        path: 'payroll/simulation',
        loadComponent: () => import('./features/payroll/simulation/simulation.component').then(m => m.SimulationComponent),
        canActivate: [authGuard],
        title: 'Simulateur de Paie - PayZen'
      }
    ]
  },

  // ============================================
  // CABINET ROUTES (/cabinet/*)
  // ============================================
  {
    path: 'cabinet',
    component: MainLayout,
    canActivate: [authGuard, contextGuard, expertModeGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => 
          import('./features/cabinet/portfolio/cabinet-dashboard')
            .then(m => m.CabinetDashboard),
        title: 'Cabinet Dashboard - PayZen'
      },
      {
        path: 'permissions',
        loadComponent: () => 
          import('./features/cabinet/permissions/cabinet-permissions')
            .then(m => m.CabinetPermissionsComponent),
        title: 'Cabinet Permissions - PayZen'
      },
      {
        path: 'audit-log',
        loadComponent: () => 
          import('./features/cabinet/audit-log/cabinet-audit-log')
            .then(m => m.CabinetAuditLogComponent),
        title: 'Audit Log - PayZen'
      }
    ]
  },

  // ============================================
  // EXPERT MODE ROUTES (/expert/*)
  // ============================================
  {
    path: 'expert',
    component: MainLayout,
    canActivate: [authGuard, contextGuard, expertModeGuard],
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      },
      {
        path: 'dashboard',
        loadComponent: () => 
          import('./features/expert/dashboard/expert-dashboard')
            .then(m => m.ExpertDashboard),
        title: 'Expert Dashboard - PayZen'
      },
      {
        path: 'client-view',
        component: Dashboard,
        data: { expertMode: true }
      },
      {
        path: 'company',
        loadComponent: () => import('./features/company/company.component').then(m => m.CompanyComponent),
        data: { expertMode: true }
      },
      {
        path: 'employees',
        component: EmployeesPage,
        data: { expertMode: true }
      },
      {
        path: 'absences',
        loadComponent: () => import('./features/employees/absences/employee-absences').then(m => m.EmployeeAbsencesComponent),
        data: { expertMode: true }
      },
      {
        path: 'absences/team',
        loadComponent: () => import('./features/employees/absences/team-absences/team-absences').then(m => m.TeamAbsencesComponent),
        data: { expertMode: true }
      },
      {
        path: 'absences/hr',
        loadComponent: () => import('./features/employees/absences/hr-absences/hr-absences').then(m => m.HrAbsencesComponent),
        data: { expertMode: true }
      },
      {
        path: 'absences/employee/:id',
        loadComponent: () => import('./features/employees/absences/employee-absence-detail/employee-absence-detail').then(m => m.EmployeeAbsenceDetailComponent),
        data: { expertMode: true }
      },
      {
        path: 'employees/create',
        component: EmployeeCreatePage,
        data: { expertMode: true }
      },
      {
        path: 'employees/:id',
        component: EmployeeProfile,
        canDeactivate: [unsavedChangesGuard],
        data: { expertMode: true }
      },
      {
        path: 'payroll/generate',
        component: Dashboard, // Placeholder
        data: { expertMode: true }
      },
      {
        path: 'reports',
        component: Dashboard, // Placeholder
        data: { expertMode: true }
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile').then(m => m.ProfileComponent)
      },

      // Payroll - Bulletins de Paie (Expert Mode)
      {
        path: 'payroll/bulletin',
        loadComponent: () => import('./features/payroll/bulletin/bulletin.component').then(m => m.BulletinComponent),
        data: { expertMode: true },
        title: 'Bulletins de Paie - PayZen'
      },

      // Payroll - Fiche de Paie (Expert Mode)
      {
        path: 'payroll/payslip',
        loadComponent: () => import('./features/payroll/payslip/payslip.component').then(m => m.PayslipComponent),
        data: { expertMode: true },
        title: 'Fiche de Paie - PayZen'
      },

      // Payroll - Exports (Expert Mode)
      {
        path: 'payroll/exports',
        loadComponent: () => import('./features/payroll/exports/payroll-exports.component').then(m => m.PayrollExportsComponent),
        data: { expertMode: true },
        title: 'Exports de Paie - PayZen'
      },

      // Payroll - Simulation de Paie (Expert Mode)
      {
        path: 'payroll/simulation',
        loadComponent: () => import('./features/payroll/simulation/simulation.component').then(m => m.SimulationComponent),
        data: { expertMode: true },
        title: 'Simulateur de Paie - PayZen'
      },

      // Leave Management (Expert Mode)
      {
        path: 'leave',
        children: [
          {
            path: '',
            loadComponent: () => import('./features/leave/leave.component').then(m => m.LeaveComponent),
            data: { expertMode: true }
          },
          {
            path: 'types',
            loadComponent: () => import('./features/leave/leave-types/leave-types').then(m => m.LeaveTypesPage),
            data: { expertMode: true }
          },
          {
            path: 'types/create',
            loadComponent: () => import('./features/leave/leave-types/leave-type-form/leave-type-form').then(m => m.LeaveTypeFormPage),
            data: { expertMode: true }
          },
          {
            path: 'types/:id',
            loadComponent: () => import('./features/leave/leave-types/leave-type-detail/leave-type-detail').then(m => m.LeaveTypeDetailPage),
            data: { expertMode: true }
          },
          {
            path: 'types/:id/edit',
            loadComponent: () => import('./features/leave/leave-types/leave-type-form/leave-type-form').then(m => m.LeaveTypeFormPage),
            data: { expertMode: true }
          },
          {
            path: 'policies',
            loadComponent: () => import('./features/leave/leave-policies/leave-policies').then(m => m.LeavePoliciesPage),
            data: { expertMode: true }
          },
          {
            path: 'policies/create',
            loadComponent: () => import('./features/leave/leave-policies/leave-policy-form/leave-policy-form').then(m => m.LeavePolicyFormPage),
            data: { expertMode: true }
          },
          {
            path: 'policies/configure/:leaveTypeId',
            loadComponent: () => import('./features/leave/leave-policies/leave-policy-form/leave-policy-form').then(m => m.LeavePolicyFormPage),
            data: { expertMode: true }
          },
          {
            path: 'policies/:id',
            loadComponent: () => import('./features/leave/leave-policies/leave-policy-form/leave-policy-form').then(m => m.LeavePolicyFormPage),
            data: { expertMode: true }
          },
          {
            path: 'legal-rules',
            loadComponent: () => import('./features/leave/leave-legal-rules/leave-legal-rules').then(m => m.LeaveLegalRulesPage),
            data: { expertMode: true }
          }
        ]
      },

      // Salary Packages (Expert Mode)
      {
        path: 'salary-packages',
        loadComponent: () => import('./features/salary-packages/salary-packages').then(m => m.SalaryPackagesPage),
        data: { expertMode: true },
        title: 'Packages de Rémunération - PayZen'
      },
      {
        path: 'salary-packages/create',
        loadComponent: () => import('./features/salary-packages/components/salary-package-create/salary-package-create').then(m => m.SalaryPackageCreateComponent),
        data: { expertMode: true },
        title: 'Nouveau Package - PayZen'
      },
      {
        path: 'salary-packages/:id',
        loadComponent: () => import('./features/salary-packages/components/salary-package-view/salary-package-view').then(m => m.SalaryPackageViewComponent),
        data: { expertMode: true },
        title: 'Détails Package - PayZen'
      },
      {
        path: 'salary-packages/:id/edit',
        loadComponent: () => import('./features/salary-packages/components/salary-package-create/salary-package-create').then(m => m.SalaryPackageCreateComponent),
        data: { expertMode: true },
        title: 'Modifier Package - PayZen'
      },

      // Overtime (Expert Mode)
      {
        path: 'overtime-management',
        loadComponent: () => import('./features/overtime/overtime-management/overtime-management').then(m => m.OvertimeManagementComponent),
        data: { expertMode: true }
      },
      {
        path: 'overtime/hr',
        loadComponent: () => import('./features/overtime/hr-employees/hr-employees').then(m => m.HrEmployeesComponent),
        data: { expertMode: true }
      },
      {
        path: 'overtime/detail/:id',
        loadComponent: () => import('./features/overtime/overtime-detail/overtime-detail').then(m => m.OvertimeDetailComponent),
        data: { expertMode: true }
      },

      // HR Leave Management (Expert Mode)
      {
        path: 'hr-leave-management',
        loadComponent: () => import('./features/leave/hr-leave-management/hr-leave-management.component').then(m => m.HrLeaveManagementComponent),
        data: { expertMode: true },
        title: 'Gestion des Congés RH - PayZen'
      },

      // Permissions (Expert Mode)
      {
        path: 'permissions',
        loadComponent: () => import('./features/permissions/permission-management.component').then(m => m.PermissionManagementComponent),
        data: { expertMode: true },
        title: 'Permission Management - PayZen'
      },

      // Reports (Expert Mode)
      {
        path: 'reports/attendance',
        loadComponent: () => import('./features/reports/attendance-report/attendance-report').then(m => m.AttendanceReportPage),
        data: { expertMode: true }
      }
    ]
  },

  // ============================================
  // LEGACY ROUTES (Backwards compatibility)
  // Redirect old routes to new structure
  // ============================================
  {
    path: 'dashboard',
    redirectTo: '/app/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'company',
    redirectTo: '/app/company',
    pathMatch: 'full'
  },
  {
    path: 'employees',
    redirectTo: '/app/employees',
    pathMatch: 'full'
  },
  {
    path: 'profile',
    redirectTo: '/app/profile',
    pathMatch: 'full'
  },



  // ============================================
  // FALLBACK
  // ============================================
  {
    path: 'access-denied',
    loadComponent: () => import('./features/auth/access-denied').then(m => m.AccessDeniedComponent)
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
