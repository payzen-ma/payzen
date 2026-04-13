import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { Role } from '../../../models/role.model';
import { User } from '../../../models/user.model';
import { RoleService } from '../../../services/role.service';
import { UserService } from '../../../services/user.service';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './users.component.html'
})
export class UsersComponent implements OnInit {
  users: User[] = [];
  filteredUsers: User[] = [];
  roles: Role[] = [];
  searchTerm = '';
  isLoading = false;
  error: string | null = null;
  // Notifications
  notifications: { id: number; type: 'error' | 'success' | 'info'; message: string }[] = [];
  private nextNotificationId = 1;

  // Modal state
  selectedUser: User | null = null;
  // Roles management (multi-role)
  showRolesModal = false;
  rolesSelection: number[] = [];
  rolesLoading = false;

  constructor(
    private userService: UserService,
    private roleService: RoleService
  ) { }

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

  showNotification(message: string, type: 'error' | 'success' | 'info' = 'info') {
    const id = this.nextNotificationId++;
    this.notifications.push({ id, type, message });
    setTimeout(() => this.dismissNotification(id), 5000);
  }

  dismissNotification(id: number) {
    this.notifications = this.notifications.filter(n => n.id !== id);
  }

  ngOnInit() {
    this.loadUsers();
    this.loadRoles();
  }

  loadUsers() {
    this.isLoading = true;
    this.error = null;
    this.userService.getAllUsers().subscribe({
      next: (data) => {
        this.users = data;
        this.filteredUsers = data;
        this.enrichUsersWithRoles();
        this.isLoading = false;
      },
      error: (err) => {
        this.error = 'Erreur lors du chargement des utilisateurs';
        this.isLoading = false;
      }
    });
  }

  loadRoles() {
    this.roleService.getAllRoles().subscribe({
      next: (data) => {
        this.roles = data;
        this.enrichUsersWithRoles();
      },
      error: (err) => {
        this.showNotification('Erreur lors du chargement des rôles', 'error');
      }
    });
  }

  private enrichUsersWithRoles() {
    if (!this.users || !this.roles) return;
    const map = new Map<number, string>();
    for (const r of this.roles) {
      if (r && typeof r.id === 'number') map.set(r.id, r.name);
    }

    this.users = this.users.map(u => ({
      ...u,
      role: u.role || (u.roleId ? map.get(u.roleId) || u.role : u.role)
    }));
    this.filteredUsers = this.filteredUsers.map(u => ({
      ...u,
      role: u.role || (u.roleId ? map.get(u.roleId) || u.role : u.role)
    }));
  }

  onSearch() {
    if (!this.searchTerm.trim()) {
      this.filteredUsers = this.users;
      return;
    }

    const term = this.searchTerm.toLowerCase();
    this.filteredUsers = this.users.filter(user =>
      user.firstName.toLowerCase().includes(term) ||
      user.lastName.toLowerCase().includes(term) ||
      user.email.toLowerCase().includes(term) ||
      user.role.toLowerCase().includes(term)
    );
  }

  // Open multi-role management modal
  openRolesModal(user: User) {
    if (!user) {
      this.showNotification('Utilisateur invalide', 'error');
      return;
    }

    const userId = (user as any).id ?? (user as any).Id ?? (user as any).userId ?? undefined;
    if (userId == null) {
      this.showNotification('Impossible de charger les rôles : identifiant utilisateur manquant', 'error');
      return;
    }

    this.selectedUser = user;
    this.rolesSelection = [];
    this.rolesLoading = true;
    this.showRolesModal = true;

    // load assigned roles for user (defensive mapping)
    this.userService.getUserRoles(userId).subscribe({
      next: (data) => {
        try {
          this.rolesSelection = (data || []).map((r: any) => r.Id ?? r.id ?? r.RoleId ?? r.roleId).filter((v: any) => v != null);
        } catch (e) {
          this.rolesSelection = [];
        }
        this.rolesLoading = false;
      },
      error: (err) => {
        this.showNotification(this.extractApiError(err) || 'Erreur lors du chargement des rôles utilisateur', 'error');
        this.rolesLoading = false;
      }
    });
  }

  closeRolesModal() {
    this.showRolesModal = false;
    this.selectedUser = null;
    this.rolesSelection = [];
    this.rolesLoading = false;
  }

  toggleRoleSelection(roleId: number) {
    const idx = this.rolesSelection.indexOf(roleId);
    if (idx > -1) this.rolesSelection.splice(idx, 1);
    else this.rolesSelection.push(roleId);
  }

  saveRolesForUser() {
    if (!this.selectedUser) return;

    const userId = (this.selectedUser as any).id ?? (this.selectedUser as any).Id ?? (this.selectedUser as any).userId ?? undefined;
    if (userId == null) {
      this.showNotification('Impossible de sauvegarder les rôles : identifiant utilisateur manquant', 'error');
      return;
    }

    // Prefer replacing all roles so unchecked roles are removed on the server
    this.userService.replaceRoles(userId, this.rolesSelection).subscribe({
      next: () => {
        const names = this.roles.filter(r => this.rolesSelection.includes(r.id)).map(r => r.name);
        if (this.selectedUser) {
          this.selectedUser.role = names.join(', ');
          this.selectedUser.roleId = this.rolesSelection.length ? this.rolesSelection[0] : undefined;
        }
        this.showNotification('Rôles mis à jour', 'success');
        this.closeRolesModal();
      },
      error: (err) => {
        // Fallback: try assignRoles (some backends may only support additive assign)
        this.userService.assignRoles(userId, this.rolesSelection).subscribe({
          next: () => {
            const names = this.roles.filter(r => this.rolesSelection.includes(r.id)).map(r => r.name);
            if (this.selectedUser) {
              this.selectedUser.role = names.join(', ');
              this.selectedUser.roleId = this.rolesSelection.length ? this.rolesSelection[0] : undefined;
            }
            this.showNotification('Rôles mis à jour (fallback)', 'success');
            this.closeRolesModal();
          },
          error: (err2) => {
            this.showNotification(this.extractApiError(err2) || 'Erreur lors de la mise à jour des rôles', 'error');
          }
        });
      }
    });
  }

  toggleStatus(user: User) {
    const newStatus = user.status === 'active' ? 'inactive' : 'active';
    this.userService.changeStatus(user.id, newStatus).subscribe({
      next: () => {
        user.status = newStatus;
      },
      error: (err) => {
        const msg = this.extractApiError(err) || 'Erreur lors du changement de statut';
        this.showNotification(msg, 'error');
      }
    });
  }

  getStatusClass(status: string): string {
    return status === 'active'
      ? 'bg-green-100 text-green-800'
      : 'bg-gray-100 text-gray-800';
  }

  getStatusLabel(status: string): string {
    return status === 'active' ? 'Actif' : 'Inactif';
  }
}
