import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { UserRole } from '@app/core/models/user.model';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { TranslateModule } from '@ngx-translate/core';
import { CardModule } from 'primeng/card';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslateModule, CardModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class ProfileComponent {
  private readonly authService = inject(AuthService);
  private readonly contextService = inject(CompanyContextService);

  readonly user = this.authService.currentUser;
  readonly userRoleLabel = computed(() => {
    const user = this.user();
    const roles = Array.isArray(user?.roles)
      ? user.roles.map(r => String(r).toLowerCase())
      : [];
    const contextRole = (this.contextService.role() ?? '').toLowerCase();
    const isExpertContext = this.contextService.isExpertMode() || contextRole === UserRole.CABINET;
    if (isExpertContext) {
      return 'Expert comptable';
    }
    const hasRhRole = roles.includes(UserRole.RH);
    const role = hasRhRole ? UserRole.RH : user?.role;
    const roleLabels: Record<string, string> = {
      [UserRole.ADMIN]: 'Admin',
      [UserRole.RH]: 'Ressource humain',
      [UserRole.MANAGER]: 'Manager',
      [UserRole.CEO]: 'CEO',
      [UserRole.EMPLOYEE]: 'Employe',
      [UserRole.CABINET]: 'Expert comptable',
      [UserRole.ADMIN_PAYZEN]: 'Admin Payzen'
    };
    return role ? (roleLabels[role] ?? role) : '-';
  });

  readonly fullName = computed(() => {
    const u = this.user();
    const first = (u?.firstName ?? '').trim();
    const last = (u?.lastName ?? '').trim();
    const full = `${first} ${last}`.trim();
    return full || u?.username || '-';
  });

  readonly workspaceLabel = computed(() => {
    if (this.contextService.isExpertMode()) {
      return this.contextService.isClientView() ? 'Expert - vue client' : 'Expert - cabinet';
    }
    return 'Standard';
  });

  readonly initials = computed(() => {
    const u = this.user();
    const first = (u?.firstName ?? '').trim();
    const last = (u?.lastName ?? '').trim();
    const fromNames = `${first.charAt(0)}${last.charAt(0)}`.trim();
    if (fromNames) return fromNames.toUpperCase();
    const username = (u?.username ?? '').trim();
    return username ? username.slice(0, 2).toUpperCase() : 'U';
  });

  readonly profileStrength = computed(() => {
    const u = this.user();
    let score = 20;
    if (u?.firstName) score += 20;
    if (u?.lastName) score += 20;
    if (u?.email) score += 20;
    if (u?.companyName) score += 20;
    return Math.min(score, 100);
  });
}
