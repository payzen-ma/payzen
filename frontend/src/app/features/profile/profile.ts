import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { CardModule } from 'primeng/card';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { UserRole } from '@app/core/models/user.model';

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
    const role = this.user()?.role;
    const roleLabels: Record<string, string> = {
      [UserRole.ADMIN]: 'Admin',
      [UserRole.RH]: 'RH',
      [UserRole.MANAGER]: 'Manager',
      [UserRole.CEO]: 'CEO',
      [UserRole.EMPLOYEE]: 'Employe',
      [UserRole.CABINET]: 'Cabinet',
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
}
