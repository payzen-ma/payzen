import { Routes } from '@angular/router';
import { MainLayout } from './layouts/main-layout/main-layout';
import { AuthLayout } from './layouts/auth-layout/auth-layout';
import { Dashboard } from './features/dashboard/dashboard';
import { EmployeesPage } from './features/employees/employees';
import { EmployeeProfile } from './features/employees/profile/employee-profile';
import { EmployeeCreatePage } from './features/employees/create/employee-create';
import { LoginPage } from './features/auth/login/login';
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
    component: AuthLayout,
    canActivate: [guestGuard],
    children: [
      {
        path: '',
        component: LoginPage
      }
    ]
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
      // Salary Packages (Expert Mode)
      {
        path: 'salary-packages',
        loadComponent: () => 
          import('./features/salary-packages/salary-packages')
            .then(m => m.SalaryPackagesPage),
        data: { expertMode: true },
        title: 'Packages de Rémunération - PayZen'
      },
      {
        path: 'salary-packages/create',
        loadComponent: () =>
          import('./features/salary-packages/components/salary-package-create/salary-package-create')
            .then(m => m.SalaryPackageCreateComponent),
        data: { expertMode: true },
        title: 'Nouveau Package - PayZen'
      },
      {
        path: 'salary-packages/:id',
        loadComponent: () => 
          import('./features/salary-packages/components/salary-package-view/salary-package-view')
            .then(m => m.SalaryPackageViewComponent),
        data: { expertMode: true },
        title: 'Détails Package - PayZen'
      },
      {
        path: 'salary-packages/:id/edit',
        loadComponent: () =>
          import('./features/salary-packages/components/salary-package-create/salary-package-create')
            .then(m => m.SalaryPackageCreateComponent),
        data: { expertMode: true },
        title: 'Modifier Package - PayZen'
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
