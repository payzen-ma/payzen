import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RoleService } from '../../../services/role.service';
import { PermissionService } from '../../../services/permission.service';
import { Role, Permission, RoleCreateRequest, RoleUpdateRequest, RoleListResponse } from '../../../models/role.model';
import { UserService } from '../../../services/user.service';
import { ConfirmService } from '../../../shared/confirm/confirm.service';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './roles.component.html'
})
export class RolesComponent implements OnInit {
  roles: Role[] = [];
  permissions: Permission[] = [];
  permissionsByResource: { [resource: string]: Permission[] } = {};
  
  isLoading = false;
  error: string | null = null;
  
  // Modal state
  showModal = false;
  isEditMode = false;
  currentRole: Role | null = null;
  
  // Form data
  formData = {
    name: '',
    description: '',
    selectedPermissions: [] as number[]
  };
  // Selected resource in modal (null = none / not selected)
  selectedResource: string | null = null;

  // Users drawer state
  showUsersDrawer = false;
  selectedRoleForUsers: Role | null = null;
  usersForRole: any[] = [];
  usersLoading = false;
  usersError: string | null = null;

  // Notifications (toasts)
  notifications: { id: number; type: 'error' | 'success' | 'info'; message: string }[] = [];
  private nextNotificationId = 1;

  constructor(
    private roleService: RoleService,
    private permissionService: PermissionService,
    private userService: UserService
    ,
    private confirmService: ConfirmService
  ) {}

  ngOnInit() {
    this.loadRoles();
    this.loadPermissions();
  }

  changeUserRole(user: any) {
    if (!user || user.selectedRoleId == null) return;
    user._updating = true;
    this.userService.assignRole({ userId: user.id, roleId: user.selectedRoleId }).subscribe({
      next: () => {
        user.roleId = user.selectedRoleId;
        // update roleName from known roles list
        const r = this.roles.find(x => x.id === user.selectedRoleId);
        user.roleName = r ? r.name : user.roleName;
        user._updating = false;
        this.showNotification('Rôle de l\'utilisateur mis à jour', 'success');
        // refresh role counts
        this.loadRoles();
      },
      error: (err) => {
        user._updating = false;
        const msg = this.extractApiError(err) || 'Erreur lors de la mise à jour du rôle de l\'utilisateur';
        this.showNotification(msg, 'error');
      }
    });
  }

  private extractApiError(err: any): string {
    try {
      if (!err) return 'Erreur inconnue';

      const status = err.status;
      const statusText = err.statusText ? `${err.status} ${err.statusText}` : (status ? `${status}` : '');
      const raw = err.error ?? err;

      const findMessage = (value: any): string | null => {
        if (!value && value !== 0) return null;
        if (typeof value === 'string') {
          const s = value.trim();
          if (!s) return null;
          if (s.startsWith('<')) return 'Réponse HTML inattendue depuis l\'API (vérifier le backend / proxy)';
          return s;
        }
        if (typeof value === 'object') {
          // common keys (case-insensitive)
          const keys = ['message', 'Message', 'error', 'Error', 'detail', 'Detail', 'title', 'Title'];
          for (const k of keys) {
            if (value[k] && typeof value[k] === 'string' && value[k].trim()) return value[k].trim();
          }
          // nested error.Message style
          if (value.error && typeof value.error === 'object') {
            const nested = findMessage(value.error);
            if (nested) return nested;
          }
          // arrays of errors
          if (Array.isArray(value.errors) && value.errors.length) {
            const joined = value.errors.map((x: any) => (typeof x === 'string' ? x : (x.message ?? x.Message ?? JSON.stringify(x)))).join('; ');
            return joined;
          }
          // check any inner string property
          for (const v of Object.values(value)) {
            if (typeof v === 'string' && v.trim()) return v.trim();
            if (typeof v === 'object') {
              const nested = findMessage(v);
              if (nested) return nested;
            }
          }
        }
        return null;
      };

      const found = findMessage(raw) || findMessage(err) || null;
      if (found) return found;

      if (err.message && typeof err.message === 'string') return err.message;
      return 'Erreur inconnue';
    } catch (ex) {
      return 'Erreur inconnue';
    }
  }

  showNotification(message: string, type: 'error' | 'success' | 'info' = 'error') {
    const id = this.nextNotificationId++;
    this.notifications.push({ id, type, message });
    // Auto-dismiss after 5s
    setTimeout(() => this.dismissNotification(id), 5000);
  }

  dismissNotification(id: number) {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }

  openUsersDrawer(role: Role) {
    this.selectedRoleForUsers = role;
    this.showUsersDrawer = true;
    this.usersForRole = [];
    this.usersLoading = true;
    this.usersError = null;

    console.debug('[Roles] openUsersDrawer for role', role?.id, role?.name);
    this.fetchUsersForRole(role);
  }

  private fetchUsersForRole(role: Role) {
    this.usersLoading = true;
    this.usersError = null;
    this.roleService.getUsersByRole(role.id).subscribe({
      next: (resp) => {
        console.debug('[Roles] raw users response for role', role.id, resp);
        let usersArray: any[] = [];
        if (!resp) {
          usersArray = [];
        } else if (Array.isArray(resp)) {
          usersArray = resp as any[];
        } else if (Array.isArray((resp as any).Users)) {
          usersArray = (resp as any).Users;
        } else if (Array.isArray((resp as any).users)) {
          usersArray = (resp as any).users;
        } else if (Array.isArray((resp as any).data)) {
          usersArray = (resp as any).data;
        } else if (Array.isArray((resp as any).items)) {
          usersArray = (resp as any).items;
        } else {
          usersArray = [];
        }

        console.debug('[Roles] extracted users array length', (usersArray || []).length);
        this.usersForRole = usersArray.map(u => ({
          id: u.UserId ?? u.id,
          username: u.Username ?? u.username,
          fullName: (u.EmployeeFirstName && u.EmployeeLastName) ? `${u.EmployeeFirstName} ${u.EmployeeLastName}` : (u.FullName ?? u.fullName ?? u.name ?? u.Username ?? ''),
              email: u.Email ?? u.email,
          employeeId: u.EmployeeId ?? null,
          companyId: u.CompanyId ?? null,
              companyName: u.CompanyName ?? u.companyName ?? null,
              // role info
              roleId: u.RoleId ?? u.roleId ?? (u.Role?.Id ?? u.Role?.id) ?? null,
              roleName: u.RoleName ?? u.roleName ?? (u.Role?.Name ?? u.Role?.name) ?? null,
              selectedRoleId: u.RoleId ?? u.roleId ?? (u.Role?.Id ?? u.Role?.id) ?? null,
              _updating: false,
          assignedAt: u.AssignedAt ?? u.assignedAt
        }));

        // Update role object reference so card shows updated count
        (role as any).Users = usersArray;
        role.userCount = Array.isArray(usersArray) ? usersArray.length : (role.userCount ?? 0);

        this.usersLoading = false;
      },
      error: (err) => {
        console.error('Error loading users for role', err);
        this.usersError = this.extractApiError(err) || 'Erreur lors du chargement des utilisateurs';
        this.showNotification(this.usersError, 'error');
        this.usersLoading = false;
      }
    });
  }

  // Click wrapper to help debug template click binding
  onOpenUsers(role: Role) {
    console.debug('[Roles] onOpenUsers clicked for', role?.id, role?.name);
    // show a small mock immediately so we can see visual feedback
    this.selectedRoleForUsers = role;
    this.showUsersDrawer = true;
    this.usersError = null;
    this.usersLoading = false;
    this.usersForRole = [
      { id: 0, username: 'mock.user', fullName: 'Utilisateur Mock', email: 'mock@example.com' }
    ];
    // then fetch real users in background and replace
    this.fetchUsersForRole(role);
  }

  closeUsersDrawer() {
    this.showUsersDrawer = false;
    this.selectedRoleForUsers = null;
    this.usersForRole = [];
    this.usersError = null;
    this.usersLoading = false;
  }

  loadRoles() {
    this.isLoading = true;
    this.error = null;
    this.roleService.getAllRoles().subscribe({
      next: (data) => {
        this.roles = data;
        // For each role, fetch users list to compute accurate userCount and cache Users array
        this.roles.forEach(r => {
          // fetch users for the role (existing behavior)
          this.roleService.getUsersByRole(r.id).subscribe({
            next: (resp) => {
              const users = (resp && (resp.Users ?? resp.users)) || (Array.isArray(resp) ? resp : []);
              (r as any).Users = users;
              console.log("Users for role", r.id, users);
              r.userCount = Array.isArray(users) ? users.length : (r.userCount ?? 0);
            },
            error: (err) => {
              console.debug('[Roles] could not fetch users for role', r.id, err);
              this.showNotification(`Erreur chargement utilisateurs (rôle ${r.id}): ${this.extractApiError(err) || 'Erreur réseau'}`, 'error');
            }
          });

          // fetch permissions assigned to the role so they persist on refresh
          this.roleService.getRolePermissions(r.id).subscribe({
            next: (perms) => {
              try {
                const mapped = (perms || []).map((p: any) => ({
                  id: p.Id ?? p.id,
                  name: p.Name ?? p.name,
                  description: p.Description ?? p.description,
                  action: p.Action ?? p.action
                }));
                r.permissions = mapped;
              } catch (ex) {
                console.debug('[Roles] could not map permissions for role', r.id, ex);
                const mapErr = this.extractApiError(ex) || String(ex);
                this.showNotification(`Erreur mappage permissions (rôle ${r.id}): ${mapErr}`, 'error');
              }
            },
            error: (err) => {
              console.debug('[Roles] could not fetch permissions for role', r.id, err);
              this.showNotification(`Erreur chargement permissions (rôle ${r.id}): ${this.extractApiError(err) || 'Erreur réseau'}`, 'error');
            }
          });
        });
        console.log('[Roles] loaded roles total:', this.roles.length);
        this.roles.forEach(r => {
          console.log('[Roles] role:', { id: r.id, name: r.name, userCount: r.userCount, UsersLength: r.Users?.length ?? r.users?.length ?? 0 });
        });
        this.isLoading = false;
      },
      error: (err) => {
        console.log('Error loading roles:', err);
        this.error = this.extractApiError(err) || 'Erreur lors du chargement des rôles';
        this.showNotification(this.error ?? 'Erreur lors du chargement des rôles', 'error');
        this.isLoading = false;
      }
    });
  }

  loadPermissions() {
    this.permissionService.getAllPermissions().subscribe({
      next: (data) => {
        this.permissions = data;
        // Group by resource
        this.permissionsByResource = data.reduce((acc, p) => {
          const key = p.resource && p.resource.trim() ? p.resource : 'global';
          if (!acc[key]) acc[key] = [];
          acc[key].push(p);
          return acc;
        }, {} as { [resource: string]: Permission[] });
      },
      error: (err) => {
        console.error('Error loading permissions:', err);
        this.error = this.extractApiError(err) || this.error;
        this.showNotification(this.error ?? 'Erreur lors du chargement des permissions', 'error');
      }
    });
  }

  openCreateModal() {
    this.isEditMode = false;
    this.currentRole = null;
    this.formData = {
      name: '',
      description: '',
      selectedPermissions: []
    };
    // default selected resource to first available
    const keys = this.getResourceKeys();
    this.selectedResource = keys.length ? keys[0] : null;
    this.showModal = true;
  }

  openEditModal(role: Role) {
    this.isEditMode = true;
    this.currentRole = role;
    this.formData = {
      name: role.name,
      description: role.description || '',
      selectedPermissions: []
    };
    // prefill selected permissions from server to ensure we reflect persistent assignments
    this.roleService.getRolePermissions(role.id).subscribe({
      next: (perms) => {
        try {
          const ids = (perms || []).map((p: any) => p.Id ?? p.id);
          this.formData.selectedPermissions = ids;
          // also populate role.permissions for display consistency
          (this.currentRole as any).permissions = (perms || []).map((p: any) => ({ id: p.Id ?? p.id, name: p.Name ?? p.name, action: p.Action ?? p.action, description: p.Description ?? p.description }));
        } catch (e) {
          console.error('Error mapping role permissions', e);
          this.showNotification(this.extractApiError(e) || 'Erreur lors du mapping des permissions', 'error');
        }
      },
      error: (err) => {
        console.error('Error fetching role permissions:', err);
        this.showNotification(this.extractApiError(err) || 'Erreur lors du chargement des permissions du rôle', 'error');
      }
    });

    const keys = this.getResourceKeys();
    this.selectedResource = keys.length ? keys[0] : null;
    this.showModal = true;
  }

  onSelectResource(resource: string | null) {
    this.selectedResource = resource;
  }

  closeModal() {
    this.showModal = false;
    this.currentRole = null;
    this.formData = { name: '', description: '', selectedPermissions: [] };
  }

  togglePermission(permissionId: number) {
    const index = this.formData.selectedPermissions.indexOf(permissionId);
    if (index > -1) {
      this.formData.selectedPermissions.splice(index, 1);
    } else {
      this.formData.selectedPermissions.push(permissionId);
    }
  }

  isPermissionSelected(permissionId: number): boolean {
    return this.formData.selectedPermissions.includes(permissionId);
  }

  saveRole() {
    if (!this.formData.name.trim()) {
      this.showNotification('Le nom du rôle est requis', 'error');
      return;
    }

    const request: RoleCreateRequest | RoleUpdateRequest = {
      name: this.formData.name,
      description: this.formData.description,
      permissionIds: this.formData.selectedPermissions
    };

    if (this.isEditMode && this.currentRole) {
      // ensure at least one permission selected (server validates this)
      if (!this.formData.selectedPermissions || this.formData.selectedPermissions.length === 0) {
        this.showNotification('Au moins une permission doit être spécifiée', 'error');
        return;
      }
      this.roleService.updateRole(this.currentRole.id, request).subscribe({
        next: (updatedRole) => {
          // after updating role metadata, ensure permissions are assigned
          this.roleService.assignPermissions(this.currentRole!.id, this.formData.selectedPermissions).subscribe({
            next: () => {
              this.loadRoles();
              this.closeModal();
              this.showNotification('Rôle mis à jour', 'success');
            },
            error: (permErr) => {
              console.error('Error assigning permissions after role update:', permErr);
              const msg = this.extractApiError(permErr) || 'Erreur lors de l\'attribution des permissions';
              this.showNotification(msg, 'error');
            }
          });
        },
        error: (err) => {
          console.error('Error updating role:', err);
          const msg = this.extractApiError(err) || 'Erreur lors de la mise à jour du rôle';
          this.showNotification(msg, 'error');
        }
      });
    } else {
      this.roleService.createRole(request).subscribe({
        next: (createdRole) => {
          const rid = (createdRole as any)?.id ?? (createdRole as any)?.Id ?? (createdRole as any)?.RoleId ?? null;
          if (rid) {
            this.roleService.assignPermissions(rid, this.formData.selectedPermissions).subscribe({
              next: () => {
                this.loadRoles();
                this.closeModal();
                this.showNotification('Rôle créé', 'success');
              },
              error: (permErr) => {
                console.error('Error assigning permissions after role create:', permErr);
                const msg = this.extractApiError(permErr) || 'Erreur lors de l\'attribution des permissions';
                this.showNotification(msg, 'error');
              }
            });
          } else {
            // fallback: no id returned, just refresh and notify
            this.loadRoles();
            this.closeModal();
            this.showNotification('Rôle créé', 'success');
          }
        },
        error: (err) => {
          console.error('Error creating role:', err);
          const msg = this.extractApiError(err) || 'Erreur lors de la création du rôle';
          this.showNotification(msg, 'error');
        }
      });
    }
  }

  async deleteRole(role: Role) {
    const ok = await this.confirmService.confirm(`Êtes-vous sûr de vouloir supprimer le rôle "${role.name}" ?`);
    if (!ok) return;

    this.roleService.deleteRole(role.id).subscribe({
      next: () => {
        this.loadRoles();
        this.showNotification('Rôle supprimé', 'success');
      },
      error: (err) => {
        console.error('Error deleting role:', err);
        const msg = this.extractApiError(err) || 'Erreur lors de la suppression du rôle';
        this.showNotification(msg, 'error');
      }
    });
  }

  getResourceKeys(): string[] {
    return Object.keys(this.permissionsByResource).sort((a, b) => a.localeCompare(b));
  }
}
