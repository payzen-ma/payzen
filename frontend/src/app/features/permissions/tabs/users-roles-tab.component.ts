import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MultiSelectModule } from 'primeng/multiselect';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { PermissionManagementService } from '@app/core/services/permission-management.service';
import { RoleEntity, UserForRoleAssignment } from '@app/core/models/permission-management.model';
import { environment } from '@environments/environment';

@Component({
  selector: 'app-users-roles-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    DialogModule,
    MultiSelectModule,
    ToastModule,
    TagModule
  ],
  providers: [MessageService],
  template: `
    <div class="p-4">
      <!-- Toolbar -->
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-semibold text-gray-900">
          {{ 'permissions.userRoles.title' | translate }}
        </h3>
      </div>

      <!-- Users Table -->
      <p-table 
        [value]="users()" 
        [loading]="isLoading()"
        [paginator]="true" 
        [rows]="10"
        styleClass="p-datatable-sm">
        
        <ng-template pTemplate="header">
          <tr>
            <th style="width: 20%">{{ 'permissions.userRoles.username' | translate }}</th>
            <th style="width: 25%">{{ 'permissions.userRoles.email' | translate }}</th>
            <th style="width: 25%">{{ 'permissions.userRoles.name' | translate }}</th>
            <th style="width: 20%">{{ 'permissions.userRoles.roles' | translate }}</th>
            <th style="width: 10%">{{ 'common.actions' | translate }}</th>
          </tr>
        </ng-template>

        <ng-template pTemplate="body" let-user>
          <tr>
            <td>
              <span class="font-mono text-sm text-blue-600">{{ user.username }}</span>
            </td>
            <td>{{ user.email }}</td>
            <td>{{ (user.firstName || '') + ' ' + (user.lastName || '') }}</td>
            <td>
              <div class="flex flex-wrap gap-1">
                @for (role of user.roles; track role.id) {
                  <p-tag [value]="role.name" severity="info" styleClass="text-xs" />
                }
                @if (!user.roles || user.roles.length === 0) {
                  <span class="text-gray-400 text-sm">{{ 'permissions.userRoles.noRoles' | translate }}</span>
                }
              </div>
            </td>
            <td>
              <button 
                pButton 
                icon="pi pi-user-edit" 
                class="p-button-text p-button-sm"
                (click)="manageUserRoles(user)">
              </button>
            </td>
          </tr>
        </ng-template>
      </p-table>

      <!-- Manage User Roles Dialog -->
      <p-dialog 
        [(visible)]="showRolesDialog" 
        [header]="'permissions.userRoles.manageRoles' | translate"
        [modal]="true"
        [style]="{width: '600px'}">
        <div class="py-4">
          <h4 class="font-semibold text-gray-900 mb-2">
            {{ selectedUser()?.username }}
          </h4>
          <p class="text-sm text-gray-600 mb-4">
            {{ selectedUser()?.email }}
          </p>
          
          <p-multiSelect
            [options]="allRoles()"
            [(ngModel)]="selectedRoleIds"
            optionLabel="name"
            optionValue="id"
            [placeholder]="'permissions.userRoles.selectRoles' | translate"
            [filter]="true"
            display="chip"
            styleClass="w-full">
          </p-multiSelect>

          <div class="mt-4 text-sm text-gray-600">
            {{ selectedRoleIds.length }} {{ 'permissions.userRoles.rolesSelected' | translate }}
          </div>
        </div>

        <ng-template pTemplate="footer">
          <button 
            pButton 
            icon="pi pi-times" 
            [label]="'common.cancel' | translate"
            class="p-button-text"
            (click)="showRolesDialog = false">
          </button>
          <button 
            pButton 
            icon="pi pi-check" 
            [label]="'common.save' | translate"
            [loading]="isSavingRoles"
            (click)="saveUserRoles()">
          </button>
        </ng-template>
      </p-dialog>

      <p-toast />
    </div>
  `
})
export class UsersRolesTabComponent implements OnInit {
  private permissionService = inject(PermissionManagementService);
  private messageService = inject(MessageService);
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}`;

  users = signal<UserForRoleAssignment[]>([]);
  allRoles = signal<RoleEntity[]>([]);
  isLoading = signal(false);
  showRolesDialog = false;
  isSavingRoles = false;
  selectedUser = signal<UserForRoleAssignment | null>(null);
  selectedRoleIds: number[] = [];

  ngOnInit(): void {
    this.loadUsers();
    this.loadAllRoles();
  }

  loadUsers(): void {
    this.isLoading.set(true);
    // Fetch users from /api/users endpoint
    this.http.get<any[]>(`${this.apiUrl}/users`).subscribe({
      next: (usersData) => {
        // For each user, fetch their roles
        const usersWithRoles = usersData.map(async (userData) => {
          const roles = await this.permissionService.getUserRoles(userData.id).toPromise();
          return {
            id: userData.id,
            username: userData.username,
            email: userData.email,
            firstName: userData.firstName,
            lastName: userData.lastName,
            roles: roles || []
          };
        });

        Promise.all(usersWithRoles).then((users) => {
          this.users.set(users);
          this.isLoading.set(false);
        });
      },
      error: (err) => {
        console.error('Failed to load users', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load users'
        });
        this.isLoading.set(false);
      }
    });
  }

  loadAllRoles(): void {
    this.permissionService.getAllRoles().subscribe({
      next: (roles) => {
        this.allRoles.set(roles);
      },
      error: (err) => {
        console.error('Failed to load roles', err);
      }
    });
  }

  manageUserRoles(user: UserForRoleAssignment): void {
    this.selectedUser.set(user);
    this.selectedRoleIds = user.roles.map(r => r.id);
    this.showRolesDialog = true;
  }

  saveUserRoles(): void {
    const user = this.selectedUser();
    if (!user) return;

    this.isSavingRoles = true;

    // Use replace operation to update all roles at once
    this.permissionService.replaceUserRoles({
      UserId: user.id,
      RoleIds: this.selectedRoleIds
    }).subscribe({
      next: (response) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: response.message
        });
        this.showRolesDialog = false;
        this.isSavingRoles = false;
        this.loadUsers();
      },
      error: (err) => {
        console.error('Failed to update user roles', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to update user roles'
        });
        this.isSavingRoles = false;
      }
    });
  }
}
