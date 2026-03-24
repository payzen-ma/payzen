import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { DashboardComponent } from './features/dashboard/dashboard.component';
import { CreateCompanyComponent } from './features/company/create-company/create-company.component';
import { CompaniesComponent } from './features/company/companies/companies.component';
import { ViewCompanyComponent } from './features/company/view-company/view-company.component';
import { EditCompanyComponent } from './features/company/edit-company/edit-company.component';
import { UsersComponent } from './features/users/users/users.component';
import { RolesComponent } from './features/roles/roles/roles.component';
import { PermissionsComponent } from './features/permissions/permissions/permissions.component';
import { EventLogComponent } from './features/event-log/event-log.component';
import { ReferentielComponent } from './features/referentiel/referentiel.component';
import { HolidaysComponent } from './features/holidays/holidays/holidays.component';
import { PayrollReferentielComponent } from './features/payroll-referentiel/payroll-referentiel.component';
import { AdminLayoutComponent } from './layouts/admin-layout/admin-layout.component';
import { authGuard } from './guards/auth.guard';
import { SalaryPackagesComponent } from './features/salary-packages/salary-packages.component';

export const routes: Routes = [
    {path: '', redirectTo: 'dashboard', pathMatch: 'full' },
    {path: 'login', component: LoginComponent },
    {
        path: 'dashboard',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            {path: '', component: DashboardComponent}
        ]
    },
    {
        path: 'companies',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            {path: '', component: CompaniesComponent}
        ]
    },
    {
        path: 'create-company',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            {path: '', component: CreateCompanyComponent}
        ]
    },
    {
        path: 'view-company/:id',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            {path: '', component: ViewCompanyComponent}
        ]
    },
    {
        path: 'edit-company/:id',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            {path: '', component: EditCompanyComponent}
        ]
    },
    {
        path: 'users',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            {path: '', component: UsersComponent}
        ]
    },
    {
        path: 'roles',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            {path: '', component: RolesComponent}
        ]
    },
    {
        path: 'permissions',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
            {path: '', component: PermissionsComponent}
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
