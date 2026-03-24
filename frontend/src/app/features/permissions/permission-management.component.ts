import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { UsersRolesTabComponent } from './tabs/users-roles-tab.component';

@Component({
  selector: 'app-permission-management',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    UsersRolesTabComponent
  ],
  template: `
    <div class="min-h-screen">
      <!-- Page Header -->
      <div class="max-w-7xl mx-auto px-6 pt-4">
        <div class="flex items-center gap-3 mb-6">
          <div class="bg-(--primary)/10 text-(--primary) p-3 rounded-(--rads-lg)">
            <i class="pi pi-user-edit text-2xl"></i>
          </div>
          <div>
            <h1 class="heading">
              {{ 'permissions.userManagement.title' | translate }}
            </h1>
            <p class="subheading mt-1">
              {{ 'permissions.userManagement.subtitle' | translate }}
            </p>
          </div>
        </div>
      </div>

      <!-- Main Content -->
      <div class="max-w-7xl mx-auto px-6 py-6">
        <div class="card">
          <app-users-roles-tab />
        </div>
      </div>
    </div>
  `
})
export class PermissionManagementComponent {}
