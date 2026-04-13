import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { Permission } from '../../../models/role.model';
import { PermissionService } from '../../../services/permission.service';
import { ConfirmService } from '../../../shared/confirm/confirm.service';
import { AddPermissionModalComponent } from './add-permission-modal.component';

@Component({
  selector: 'app-permissions',
  standalone: true,
  imports: [CommonModule, AddPermissionModalComponent],
  templateUrl: './permissions.component.html'
})
export class PermissionsComponent implements OnInit {
  private permissionService = inject(PermissionService);
  private confirm = inject(ConfirmService);

  permissions: Permission[] = [];
  permissionsByResource: Record<string, Permission[]> = {};
  isLoading = false;
  // modal / edit state
  showModal = false;
  selectedPermission: Permission | null = null;
  addError = '';
  // notifications
  notifications: { id: number; type: 'error' | 'success' | 'info'; message: string }[] = [];
  private nextNotificationId = 1;

  ngOnInit(): void {
    this.loadPermissions();
  }

  onAddPermission(): void {
    this.selectedPermission = null;
    this.showModal = true;
    this.addError = '';
  }

  handleAddPermission(permission: Permission): void {
    this.addError = '';

    this.permissionService.createPermission(permission).subscribe({
      next: () => {
        this.showModal = false;
        this.showNotification('Permission ajoutée', 'success');
        this.loadPermissions();
      },
      error: (err) => {
        this.addError = "Erreur lors de l'ajout de la permission.";
        this.showNotification(this.extractApiError(err) || 'Erreur lors de l\'ajout de la permission', 'error');
      }
    });
  }

  handleCancelAdd(): void {
    this.showModal = false;
  }

  onEditPermission(permission: Permission): void {
    this.selectedPermission = permission;
    this.showModal = true;
  }

  async onDeletePermission(permission: Permission): Promise<void> {
    if (!permission.id) return;
    const ok = await this.confirm.confirm('Confirmer la suppression de cette permission ?');
    if (!ok) return;
    this.permissionService.deletePermission(permission.id).subscribe({
      next: () => {
        this.showNotification('Permission supprimée', 'success');
        this.loadPermissions();
      },
      error: (err) => {
        this.showNotification(this.extractApiError(err) || 'Erreur lors de la suppression', 'error');
      }
    });
  }

  handleUpdatePermission(payload: any): void {
    const id = payload.id;
    const body = { name: payload.name, description: payload.description, resource: payload.resource, action: payload.action };
    this.permissionService.updatePermission(id, body).subscribe({
      next: () => {
        this.showModal = false;
        this.showNotification('Permission mise à jour', 'success');
        this.loadPermissions();
      },
      error: (err) => {
        this.showNotification(this.extractApiError(err) || 'Erreur lors de la mise à jour', 'error');
      }
    });
  }

  showNotification(message: string, type: 'error' | 'success' | 'info' = 'info') {
    const id = this.nextNotificationId++;
    this.notifications.push({ id, type, message });
    setTimeout(() => this.dismissNotification(id), 5000);
  }

  private extractApiError(err: any): string {
    try {
      const raw = err?.error ?? err;
      if (!raw) return err?.message ?? 'Erreur inconnue';
      if (typeof raw === 'string') return raw;
      if (typeof raw === 'object') {
        return raw.Message ?? raw.message ?? raw.detail ?? JSON.stringify(raw);
      }
      return String(raw);
    } catch {
      return 'Erreur inconnue';
    }
  }

  dismissNotification(id: number) {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }

  loadPermissions(): void {
    this.isLoading = true;

    this.permissionService.getAllPermissions().subscribe({
      next: (permissions: Permission[]) => {
        this.permissions = permissions;
        this.groupPermissionsByResource();
        this.isLoading = false;
      },
      error: (error) => {
        this.showNotification('Erreur lors du chargement des permissions', 'error');
        this.isLoading = false;
      }
    });
  }

  private groupPermissionsByResource(): void {
    this.permissionsByResource = this.permissions.reduce(
      (acc: Record<string, Permission[]>, permission) => {

        const resource = permission.resource ?? 'UNKNOWN';

        if (!acc[resource]) {
          acc[resource] = [];
        }

        acc[resource].push(permission);
        return acc;
      },
      {}
    );
  }

  getResourceKeys(): string[] {
    return Object.keys(this.permissionsByResource);
  }
}
