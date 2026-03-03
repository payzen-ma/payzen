import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { PermissionManagementService } from '@app/core/services/permission-management.service';
import { PermissionEntity, PermissionCreateDto, PermissionUpdateDto } from '@app/core/models/permission-management.model';

@Component({
  selector: 'app-permissions-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    ToastModule
  ],
  providers: [MessageService],
  template: `
    <div class="p-4">
      <!-- Toolbar -->
      <div class="flex justify-between items-center mb-4">
        <h3 class="text-lg font-semibold text-gray-900">
          {{ 'permissions.permissions.title' | translate }}
        </h3>
        <button 
          pButton 
          icon="pi pi-plus" 
          [label]="'permissions.permissions.create' | translate"
          (click)="openCreateDialog()">
        </button>
      </div>

      <!-- Permissions Table -->
      <p-table 
        [value]="permissions()" 
        [loading]="isLoading()"
        [paginator]="true" 
        [rows]="10"
        styleClass="p-datatable-sm">
        
        <ng-template pTemplate="header">
          <tr>
            <th style="width: 25%">{{ 'permissions.permissions.name' | translate }}</th>
            <th style="width: 55%">{{ 'permissions.permissions.description' | translate }}</th>
            <th style="width: 20%">{{ 'common.actions' | translate }}</th>
          </tr>
        </ng-template>

        <ng-template pTemplate="body" let-permission>
          <tr>
            <td>
              <span class="font-mono text-sm text-blue-600">{{ permission.name }}</span>
            </td>
            <td>{{ permission.description }}</td>
            <td>
              <div class="flex gap-2">
                <button 
                  pButton 
                  icon="pi pi-pencil" 
                  class="p-button-text p-button-sm"
                  (click)="openEditDialog(permission)">
                </button>
                <button 
                  pButton 
                  icon="pi pi-trash" 
                  class="p-button-text p-button-danger p-button-sm"
                  (click)="deletePermission(permission)">
                </button>
              </div>
            </td>
          </tr>
        </ng-template>
      </p-table>

      <!-- Create/Edit Dialog -->
      <p-dialog 
        [(visible)]="showDialog" 
        [header]="dialogTitle()"
        [modal]="true"
        [style]="{width: '500px'}">
        <div class="flex flex-col gap-4 py-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              {{ 'permissions.permissions.name' | translate }}
            </label>
            <input 
              pInputText 
              [(ngModel)]="currentPermission.Name"
              [disabled]="isEditMode"
              class="w-full" 
              [placeholder]="'READ_USERS'"/>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              {{ 'permissions.permissions.description' | translate }}
            </label>
            <textarea 
              pInputTextarea 
              [(ngModel)]="currentPermission.Description"
              class="w-full" 
              rows="3"
              [placeholder]="'permissions.permissions.descriptionPlaceholder' | translate">
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
            (click)="savePermission()">
          </button>
        </ng-template>
      </p-dialog>

      <p-toast />
    </div>
  `
})
export class PermissionsTabComponent implements OnInit {
  private permissionService = inject(PermissionManagementService);
  private messageService = inject(MessageService);

  permissions = signal<PermissionEntity[]>([]);
  isLoading = signal(false);
  showDialog = false;
  isSaving = false;
  isEditMode = false;
  currentPermissionId?: number;

  currentPermission: PermissionCreateDto = {
    Name: '',
    Description: ''
  };

  dialogTitle = signal('permissions.permissions.create');

  ngOnInit(): void {
    this.loadPermissions();
  }

  loadPermissions(): void {
    this.isLoading.set(true);
    this.permissionService.getAllPermissions().subscribe({
      next: (permissions) => {
        this.permissions.set(permissions);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load permissions', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load permissions'
        });
        this.isLoading.set(false);
      }
    });
  }

  openCreateDialog(): void {
    this.isEditMode = false;
    this.currentPermission = { Name: '', Description: '' };
    this.dialogTitle.set('permissions.permissions.create');
    this.showDialog = true;
  }

  openEditDialog(permission: PermissionEntity): void {
    this.isEditMode = true;
    this.currentPermissionId = permission.id;
    this.currentPermission = {
      Name: permission.name,
      Description: permission.description
    };
    this.dialogTitle.set('permissions.permissions.edit');
    this.showDialog = true;
  }

  savePermission(): void {
    if (!this.currentPermission.Name || !this.currentPermission.Description) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please fill all fields'
      });
      return;
    }

    this.isSaving = true;

    if (this.isEditMode && this.currentPermissionId) {
      const updateDto: PermissionUpdateDto = {
        Description: this.currentPermission.Description
      };
      this.permissionService.updatePermission(this.currentPermissionId, updateDto).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Permission updated successfully'
          });
          this.showDialog = false;
          this.isSaving = false;
          this.loadPermissions();
        },
        error: (err) => {
          console.error('Failed to update permission', err);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to update permission'
          });
          this.isSaving = false;
        }
      });
    } else {
      this.permissionService.createPermission(this.currentPermission).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Permission created successfully'
          });
          this.showDialog = false;
          this.isSaving = false;
          this.loadPermissions();
        },
        error: (err) => {
          console.error('Failed to create permission', err);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to create permission'
          });
          this.isSaving = false;
        }
      });
    }
  }

  deletePermission(permission: PermissionEntity): void {
    if (!confirm(`Delete permission "${permission.name}"?`)) {
      return;
    }

    this.permissionService.deletePermission(permission.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Permission deleted successfully'
        });
        this.loadPermissions();
      },
      error: (err) => {
        console.error('Failed to delete permission', err);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to delete permission'
        });
      }
    });
  }
}
