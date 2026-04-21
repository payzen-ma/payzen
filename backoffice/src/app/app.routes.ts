import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { CompaniesComponent } from './features/company/companies/companies.component';
import { CreateCompanyComponent } from './features/company/create-company/create-company.component';
import { EditCompanyComponent } from './features/company/edit-company/edit-company.component';
import { ViewCompanyComponent } from './features/company/view-company/view-company.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { EventLogComponent } from './features/event-log/event-log.component';
import { HolidaysComponent } from './features/holidays/holidays/holidays.component';
import { PayrollReferentielComponent } from './features/payroll-referentiel/payroll-referentiel.component';
import { PermissionsComponent } from './features/permissions/permissions/permissions.component';
import { ReferentielComponent } from './features/referentiel/referentiel.component';
import { RolesComponent } from './features/roles/roles/roles.component';
import { SalaryPackagesComponent } from './features/salary-packages/salary-packages.component';
import { UsersComponent } from './features/users/users/users.component';
import { authGuard } from './guards/auth.guard';
import { guestGuard } from './guards/guest.guard';
import { AdminLayoutComponent } from './layouts/admin-layout/admin-layout.component';

export const routes: Routes = [
    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    {
        path: 'login',
        canActivate: [guestGuard],
        component: LoginComponent,
    },
    {
        path: 'auth/callback',
        loadComponent: () =>
            import('./features/auth/entra-callback/entra-callback.component').then(
                (m) => m.EntraCallbackComponent
            ),
    },
    {
        path: 'dashboard',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: DashboardComponent }
        ]
    },
    {
        path: 'companies',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: CompaniesComponent }
        ]
    },
    {
        path: 'create-company',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: CreateCompanyComponent }
        ]
    },
    {
        path: 'view-company/:id',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: ViewCompanyComponent }
        ]
    },
    {
        path: 'edit-company/:id',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: EditCompanyComponent }
        ]
    },
    {
        path: 'users',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: UsersComponent }
        ]
    },
    {
        path: 'roles',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: RolesComponent }
        ]
    },
    {
        path: 'permissions',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: PermissionsComponent }
        ]
    },
    {
        path: 'referentiel',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: ReferentielComponent }
        ]
    },
    {
        path: 'holidays',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: HolidaysComponent }
        ]
    },
    {
        path: 'event-log',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: EventLogComponent }
        ]
    },
    {
        path: 'payroll-referentiel',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: PayrollReferentielComponent }
        ]
    },
    {
        path: 'package-salary',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', component: SalaryPackagesComponent }
        ]
    }
];
