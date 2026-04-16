import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { Header } from '@app/shared/components/header/header';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyMembership } from '@app/core/models/membership.model';
import { UserRole } from '@app/core/models/user.model';

/** localStorage key prefix for employee mode preference */
const EMPLOYEE_MODE_PREF_PREFIX = 'payzen_employee_mode_';

@Component({
  selector: 'app-context-selection',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    CardModule,
    ButtonModule,
    TagModule,
    ProgressSpinnerModule,
    SkeletonModule,
    TooltipModule,
    Header
  ],
  templateUrl: './context-selection.html',
  styleUrl: './context-selection.css'
})
export class ContextSelectionPage implements OnInit {
  private readonly contextService = inject(CompanyContextService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  // Signals
  readonly isLoading = signal<boolean>(true);
  readonly selectedCard = signal<string | null>(null);
  readonly isNavigating = signal<boolean>(false);

  // Computed from context service
  readonly memberships = this.contextService.memberships;
  readonly currentUser = this.authService.currentUser;

  // Computed display values
  readonly userName = computed(() => {
    const user = this.currentUser();
    if (user) {
      return `${user.firstName} ${user.lastName}`.trim() || user.email;
    }
    return '';
  });

  readonly hasMemberships = computed(() => this.memberships().length > 0);
  readonly singleMembership = computed(() => this.memberships().length === 1);

  ngOnInit(): void {
    // Simulate loading or wait for data
    setTimeout(() => {
      this.isLoading.set(false);
      
      // Auto-select if only one membership
      if (this.singleMembership()) {
        this.selectMembership(this.memberships()[0]);
      }
    }, 300);
  }

  /**
   * Handle membership card selection
   */
  selectMembership(membership: CompanyMembership): void {
    this.selectedCard.set(this.getMembershipId(membership));
    this.isNavigating.set(true);

    // If an employee picks standard mode, save their preference so the
    // context-selection page won't reappear on subsequent logins.
    const user = this.currentUser();
    if (user && user.role === UserRole.EMPLOYEE && !membership.isExpertMode) {
      localStorage.setItem(`${EMPLOYEE_MODE_PREF_PREFIX}${user.id}`, 'standard');
    }

    // Small delay for visual feedback
    setTimeout(() => {
      this.contextService.selectContext(membership, true);
    }, 400);
  }

  /**
   * Get role display label
   */
  getRoleLabel(role: string): string {
    const user = this.currentUser();
    const userRoles = Array.isArray(user?.roles)
      ? user.roles.map(r => String(r).toLowerCase())
      : [];
    const normalizedRole = role.toLowerCase();

    // Si l'utilisateur possède aussi le rôle RH, on priorise l'affichage RH
    // (sauf en contexte cabinet qui doit rester Expert comptable).
    if (normalizedRole === 'admin' && userRoles.includes(UserRole.RH)) {
      return 'Ressource humain';
    }

    const roleLabels: Record<string, string> = {
      admin: 'Administrator',
      rh: 'Ressource humain',
      manager: 'Manager',
      employee: 'Employee',
      cabinet: 'Cabinet Expert',
      admin_payzen: 'PayZen Admin'
    };
    return roleLabels[normalizedRole] || role;
  }

  /**
   * Get appropriate icon for role
   */
  getRoleIcon(role: string): string {
    const roleIcons: Record<string, string> = {
      admin: 'pi-crown',
      rh: 'pi-users',
      manager: 'pi-briefcase',
      employee: 'pi-user',
      cabinet: 'pi-building-columns',
      admin_payzen: 'pi-shield'
    };
    return roleIcons[role.toLowerCase()] || 'pi-user';
  }

  /**
   * Handle logout
   */
  logout(): void {
    this.contextService.clearAll();
    this.authService.logout();
  }

  /**
   * Track by function for memberships list
   */
  trackByMembershipId(index: number, membership: CompanyMembership): string {
    return this.getMembershipId(membership);
  }

  /**
   * Generate unique ID for membership card
   */
  getMembershipId(membership: CompanyMembership): string {
    return `${membership.companyId}_${membership.isExpertMode}`;
  }
}
