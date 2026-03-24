import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MultiSelectModule } from 'primeng/multiselect';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { PermissionManagementService } from '@app/core/services/permission-management.service';
import { RoleEntity, RoleCreateDto, RoleUpdateDto, PermissionEntity } from '@app/core/models/permission-management.model';

@Component({
  selector: 'app-roles-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
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
          {{ 'permissions.roles.title' | translate }}
        </h3>
        <button 
          pButton 
          icon="pi pi-plus" 
          [label]="'permissions.roles.create' | translate"
          (click)="openCreateDialog()">
        </button>
      </div>

      <!-- Roles Table -->
      <p-table 
        [value]="roles()" 
        [loading]="isLoading()"
        [paginator]="true" 
        [rows]="10"
        styleClass="p-datatable-sm">
        
        <ng-template pTemplate="header">
          <tr>
            <th style="width: 20%">{{ 'permissions.roles.name' | translate }}</th>
            <th style="width: 35%">{{ 'permissions.roles.description' | translate }}</th>
            <th style="width: 25%">{{ 'permissions.roles.permissions' | translate }}</th>
            <th style="width: 20%">{{ 'common.actions' | translate }}</th>
          </tr>
        </ng-template>

        <ng-template pTemplate="body" let-role>
          <tr>
            <td>
              <span class="font-semibold text-gray-900">{{ role.name }}</span>
            </td>
            <td>{{ role.description }}</td>
            <td>
              <button 
                pButton 
                icon="pi pi-shield" 
                [label]="(role.permissions?.length || 0) + ' permissions'"
                class="p-button-text p-button-sm"
                (click)="manageRolePermissions(role)">
              </button>
            </td>
            <td>
              <div class="flex gap-2">
                <button 
                  pButton 
                  icon="pi pi-pencil" 
                  class="p-button-text p-button-sm"
                  (click)="openEditDialog(role)">
                </button>
                <button 
                  pButton 
                  icon="pi pi-trash" 
                  class="p-button-text p-button-danger p-button-sm"
                  (click)="deleteRole(role)">
                </button>
              </div>
            </td>
          </tr>
        </ng-template>
      </p-table>

      <!-- Create/Edit Role Dialog -->
      <p-dialog 
        [(visible)]="showDialog" 
        [header]="dialogTitle()"
        [modal]="true"
        [style]="{width: '500px'}">
        <div class="flex flex-col gap-4 py-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              {{ 'permissions.roles.name' | translate }}
            </label>
            <input 
              pInputText 
              [(ngModel)]="currentRole.Name"
              class="w-full" 
              [placeholder]="'Admin'"/>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              {{ 'permissions.roles.description' | translate }}
            </label>
            <textarea 
              pInputTextarea 
              [(ngModel)]="currentRole.Description"
              class="w-full" 
              rows="3"
              [placeholder]="'permissions.roles.descriptionPlaceholder' | translate">
            </textarea>
          </div>
        </div>

        <ng-template pTemplate="footer">
          <button 
            pButton 
            icon="pi pi-times" 
            [label]="'common.cancel' | translate"
            class="p-button-text"
            (click)="showDialog = false">
          </button>
          <button 
            pButton 
            icon="pi pi-check" 
            [label]="'common.save' | translate"
            [loading]="isSaving"
            (click)="saveRole()">
          </button>
        </ng-template>
      </p-dialog>

      <!-- Manage Permissions Dialog -->
      <p-dialog 
        [(visible)]="showPermissionsDialog" 
        [header]="'permissions.roles.managePermissions' | translate"
        [modal]="true"
        [style]="{width: '600px'}">
        <div class="py-4">
          <h4 class="font-semibold text-gray-900 mb-4">
            {{ selectedRole()?.name }}
          </h4>
          
          <p-multiSelect
            [options]="allPermissions()"
            [(ngModel)]="selectedPermissionIds"
            optionLabel="name"
            optionValue="id"
            [placeholder]="'permissions.roles.selectPermissions' | translate"
            [filter]="true"
            display="chip"
            styleClass="w-full">
          </p-multiSelect>

          <div class="mt-4 text-sm text-gray-600">
            {{ selectedPermissionIds.length }} {{ 'permissions.roles.permissionsSelected' | translate }}
          </div>
        </div>

        <ng-template pTemplate="footer">
          <button 
            pButton 
            icon="pi pi-times" 
            [label]="'common.cancel' | translate"
            class="p-button-text"
            (click)="showPermissionsDialog = false">
          </button>
          <button 
            pButton 
            icon="pi pi-check" 
            [label]="'common.save' | translate"
            [loading]="isSavingPermissions"
            (click)="saveRolePermissions()">
          </button>
        </ng-template>
      </p-dialog>

      <p-toast />
    </div>
  `
})
export class RolesTabComponent implements OnInit {
  private permissionService = inject(PermissionManagementService);
  private messageService = inject(MessageService);

  roles = signal<RoleEntity[]>([]);
  allPermissions = signal<PermissionEntity[]>([]);
  isLoading = signal(false);
  showDialog = false;
  showPermissionsDialog = false;
  isSaving = false;
  isSavingPermissions = false;
  isEditMode = false;
  currentRoleId?: number;
  selectedRole = signal<RoleEntity | null>(null);
  selectedPermissionIds: number[] = [];

  currentRole: RoleCreateDto = {
    Name: '',
    Description: ''
  };

  dialogTitle = signal('permissions.roles.create');

  ngOnInit(): void {
    this.loadRoles();
    this.loadAllPermissions();
  }

  loadRoles(): void {
    this.isLoading.set(true);
    this.permissionService.getAllRoles().subscribe({
      next: (roles) => {
        this.roles.set(roles);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load roles', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load roles'
        });
        this.isLoading.set(false);
      }
    });
  }

  loadAllPermissions(): void {
    this.permissionService.getAllPermissions().subscribe({
      next: (permissions) => {
        this.allPermissions.set(permissions);
      },
      error: (err) => {
        console.error('Failed to load permissions', err);
      }
    });
  }

  openCreateDialog(): void {
    this.isEditMode = false;
    this.currentRole = { Name: '', Description: '' };
    this.dialogTitle.set('permissions.roles.create');
    this.showDialog = true;
  }

  openEditDialog(role: RoleEntity): void {
    this.isEditMode = true;
    this.currentRoleId = role.id;
    this.currentRole = {
      Name: role.name,
      Description: role.description
    };
    this.dialogTitle.set('permissions.roles.edit');
    this.showDialog = true;
  }

  saveRole(): void {
    if (!this.currentRole.Name || !this.currentRole.Description) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please fill all fields'
      });
      return;
    }

    this.isSaving = true;

    if (this.isEditMode && this.currentRoleId) {
      const updateDto: RoleUpdateDto = {
        Name: this.currentRole.Name,
        Description: this.currentRole.Description
      };
      this.permissionService.updateRole(this.currentRoleId, updateDto).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Role updated successfully'
          });
          this.showDialog = false;
          this.isSaving = false;
          this.loadRoles();
        },
        error: (err) => {
          console.error('Failed to update role', err);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to update role'
          });
          this.isSaving = false;
        }
      });
    } else {
      this.permissionService.createRole(this.currentRole).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Role created successfully'
          });
          this.showDialog = false;
          this.isSaving = false;
          this.loadRoles();
        },
        error: (err) => {
          console.error('Failed to create role', err);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to create role'
          });
          this.isSaving = false;
        }
      });
    }
  }

  deleteRole(role: RoleEntity): void {
    if (!confirm(`Delete role "${role.name}"?`)) {
      return;
    }

    this.permissionService.deleteRole(role.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Role deleted successfully'
        });
        this.loadRoles();
      },
      error: (err) => {
        console.error('Failed to delete role', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to delete role'
        });
      }
    });
  }

  manageRolePermissions(role: RoleEntity): void {
    this.selectedRole.set(role);
    
    // Load current role permissions
    this.permissionService.getRolePermissions(role.id).subscribe({
      next: (permissions) => {
        this.selectedPermissionIds = permissions.map(p => p.id);
        this.showPermissionsDialog = true;
      },
      error: (err) => {
        console.error('Failed to load role permissions', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load role permissions'
        });
      }
    });
  }

  saveRolePermissions(): void {
    const role = this.selectedRole();
    if (!role) return;

    this.isSavingPermissions = true;

    // Get current permissions
    this.permissionService.getRolePermissions(role.id).subscribe({
      next: (currentPermissions) => {
        const currentIds = currentPermissions.map(p => p.id);
        const toAdd = this.selectedPermissionIds.filter(id => !currentIds.includes(id));
        const toRemove = currentIds.filter(id => !this.selectedPermissionIds.includes(id));

        // Add new permissions
        const addOperations = toAdd.map(permissionId =>
          this.permissionService.assignPermissionToRole({ RoleId: role.id, PermissionId: permissionId })
        );

        // Remove old permissions
        const removeOperations = toRemove.map(permissionId =>
          this.permissionService.removePermissionFromRole({ RoleId: role.id, PermissionId: permissionId })
        );

        // Execute all operations
        Promise.all([...addOperations, ...removeOperations]).then(() => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Role permissions updated successfully'
          });
          this.showPermissionsDialog = false;
          this.isSavingPermissions = false;
          this.loadRoles();
        }).catch((err) => {
          console.error('Failed to update role permissions', err);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to update role permissions'
          });
          this.isSavingPermissions = false;
        });
      },
      error: (err) => {
        console.error('Failed to get current permissions', err);
        this.isSavingPermissions = false;
      }
    });
  }
}
