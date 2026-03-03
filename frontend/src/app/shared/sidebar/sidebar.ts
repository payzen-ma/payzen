import { Component, signal, input, computed, output, effect, inject } from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { AvatarModule } from 'primeng/avatar';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { DialogModule } from 'primeng/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { TooltipModule } from 'primeng/tooltip';
import { SidebarGroupComponent } from './sidebar-group/sidebar-group.component';
import { SidebarGroupLabelComponent } from './sidebar-group/sidebar-group-label.component';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { UserRole } from '@app/core/models/user.model';

interface MenuItemConfig extends MenuItem {
  requiredRoles?: UserRole[];
  requiredPermissions?: string[];
  modes?: ('expert' | 'standard' | 'expert-client' | 'expert-all')[];
  requiresCompanyContext?: boolean; // New flag to indicate if company selection is needed
  id?: string; // Unique identifier for special filtering
}

@Component({
  selector: 'app-sidebar',
  imports: [
    NgClass,
    RouterLink,
    RouterLinkActive,
    AvatarModule,
    ButtonModule,
    MenuModule,
    DialogModule,
    TranslateModule,
    TooltipModule,
    SidebarGroupComponent,
    SidebarGroupLabelComponent
  ],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css',
})
export class Sidebar {
  // === Inputs ===
  readonly Width = input<number>(240);
  readonly CollapsedWidth = input<number>(70);
  readonly Collapsible = input<boolean>(false);
  readonly Collapsed = input<boolean>(false);
  readonly CollapsedChange = output<boolean>();
  readonly showCloseButton = input<boolean>(false);
  readonly closeButtonClick = output<void>();
  readonly className = input<string | string[] | Record<string, boolean> | null>(null);

  // === Internal state ===
  private readonly isCollapsedSignal = signal(this.Collapsed());
  private readonly isContentCollapsedSignal = signal(this.Collapsed());
  private readonly authService = inject(AuthService);
  private readonly contextService = inject(CompanyContextService);
  private readonly router = inject(Router);
  private toggleTimeout: any;

  // === Company Selection Dialog State ===
  readonly showCompanyRequiredDialog = signal(false);
  readonly pendingNavigationRoute = signal<string | null>(null);

  constructor() {
    // Sync internal state when input changes externally
    effect(() => {
      const val = this.Collapsed();
      this.isCollapsedSignal.set(val);
      this.isContentCollapsedSignal.set(val);
    });
  }

  // === Exposed collapsed state for templates ===
  // This now reflects the delayed content state
  readonly isSidebarCollapsed = computed(() => this.isContentCollapsedSignal());

  // === Current user info ===
  readonly currentUser = this.authService.currentUser;
  readonly userDisplayName = computed(() => {
    const user = this.currentUser();
    if (!user) return '';
    return user.username || user.email;
  });
  
  readonly userRoleLabel = computed(() => {
    const role = this.currentUser()?.role;
    const roleLabels: Record<string, string> = {
      [UserRole.ADMIN]: 'user.role.admin',
      [UserRole.RH]: 'user.role.hr',
      [UserRole.MANAGER]: 'user.role.manager',
      [UserRole.EMPLOYEE]: 'user.role.employee',
      [UserRole.CABINET]: 'user.role.cabinet',
      [UserRole.ADMIN_PAYZEN]: 'user.role.adminPayzen'
    };
    return role ? roleLabels[role] || role : '';
  });

  // === Company Context Info ===
  readonly currentCompanyName = this.contextService.companyName;
  readonly isExpertMode = this.contextService.isExpertMode;
  readonly isClientView = this.contextService.isClientView;
  readonly hasMultipleMemberships = computed(() => this.contextService.memberships().length > 1);

  // === Computed Route Prefix based on mode ===
  readonly routePrefix = computed(() => this.isExpertMode() ? '/expert' : '/app');

  // === Computed width ===
  readonly currentWidth = computed(() =>
    this.isCollapsedSignal() ? this.CollapsedWidth() : this.Width()
  );

  // === Dynamic Menu Label ===
  readonly menuGroupLabel = computed(() => {
    if (this.isExpertMode()) {
      return this.isClientView() ? 'expert.clientManagement' : 'expert.myCabinet';
    }
    return 'nav.mainMenu';
  });

  // === Behavior ===
  toggle() {
    if (!this.Collapsible()) return;

    const next = !this.isCollapsedSignal();
    this.isCollapsedSignal.set(next);
    this.CollapsedChange.emit(next);

    // Clear any existing timeout to handle rapid toggles
    if (this.toggleTimeout) {
      clearTimeout(this.toggleTimeout);
    }

    if (next) {
      // Collapsing: Delay content change to allow width animation to start/finish
      this.toggleTimeout = setTimeout(() => {
        this.isContentCollapsedSignal.set(true);
      }, 200); // 200ms delay for smoother collapse
    } else {
      // Expanding: Delay content change slightly less or same
      this.toggleTimeout = setTimeout(() => {
        this.isContentCollapsedSignal.set(false);
      }, 200);
    }
  }

  // === Menu Items Template (routes will be prefixed dynamically) ===
  private readonly menuItemsTemplate: MenuItemConfig[] = [
    // ─────────────────────────────────────────────────────────────
    // EXPERT MODE - PERSISTENT MENU ITEMS (Always visible for CABINET)
    // Dashboard, Société, Salariés, Congés always visible
    // ─────────────────────────────────────────────────────────────
    { 
      label: 'nav.dashboard', 
      icon: 'pi pi-home', 
      routerLink: '/dashboard',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN],
      modes: ['expert-all'], // Show in both expert views (portfolio and client)
      requiresCompanyContext: false // Dashboard doesn't require company selection
    },
    { 
      label: 'nav.company', 
      icon: 'pi pi-building', 
      routerLink: '/company',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN],
      modes: ['expert-all'],
      requiresCompanyContext: false // Allow access to manage Cabinet or Client
    },
    { 
      label: 'nav.employees', 
      icon: 'pi pi-users', 
      routerLink: '/employees',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN],
      modes: ['expert-all'],
      requiresCompanyContext: false // Allow access to manage Cabinet or Client
    },
    {
      label: 'nav.absences',
      icon: 'pi pi-calendar-times',
      routerLink: '/absences/hr',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN],
      modes: ['expert-all'],
      requiresCompanyContext: false
    },
    { 
      label: 'nav.leave', 
      icon: 'pi pi-calendar', 
      routerLink: '/leave',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN],
      modes: ['expert-all'],
      requiresCompanyContext: false // Allow access to manage Cabinet or Client
    },
    { 
      label: 'nav.salaryPackages', 
      icon: 'pi pi-money-bill', 
      routerLink: '/salary-packages',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN],
      modes: ['expert-all'],
      requiresCompanyContext: false
    },
    
    // ─────────────────────────────────────────────────────────────
    // STANDARD MODE (Regular company users)
    // Shows: Dashboard, RH Core items, Reports, Permissions
    // ─────────────────────────────────────────────────────────────
    { 
      label: 'nav.dashboard', 
      icon: 'pi pi-home', 
      routerLink: '/dashboard',
      requiredRoles: [UserRole.ADMIN, UserRole.RH, UserRole.MANAGER, UserRole.EMPLOYEE],
      modes: ['standard']
    },
    { 
      label: 'nav.employees', 
      icon: 'pi pi-users', 
      routerLink: '/employees',
      requiredRoles: [UserRole.ADMIN, UserRole.RH, UserRole.MANAGER],
      modes: ['standard']
    },
    // Attendance menu removed per request (hidden from sidebar)
    {
      label: 'nav.absences',
      icon: 'pi pi-calendar-times',
      routerLink: '/absences',
      requiredRoles: [UserRole.EMPLOYEE],
      modes: ['standard']
    },
    {
      label: 'nav.absencesTeam',
      icon: 'pi pi-users',
      routerLink: '/absences/team',
      requiredRoles: [UserRole.MANAGER, UserRole.ADMIN, UserRole.RH],
      modes: ['standard'],
      // Will be filtered dynamically based on hasSubordinates
      id: 'team-absences'
    },
    {
      label: 'nav.absences',
      icon: 'pi pi-calendar-times',
      routerLink: '/absences/hr',
      requiredRoles: [UserRole.ADMIN, UserRole.RH],
      modes: ['standard']
    },
    {
      label: 'nav.overtime',
      icon: 'pi pi-clock',
      routerLink: '/overtime',
      requiredRoles: [UserRole.EMPLOYEE],
      modes: ['standard']
    },
    {
      label: 'nav.overtimeManagement',
      icon: 'pi pi-check-circle',
      routerLink: '/overtime-management',
      requiredRoles: [UserRole.ADMIN, UserRole.RH, UserRole.MANAGER],
      modes: ['standard']
    },
    { 
      label: 'nav.leave', 
      icon: 'pi pi-calendar', 
      routerLink: '/leave',
      requiredRoles: [UserRole.ADMIN, UserRole.RH, UserRole.MANAGER, UserRole.EMPLOYEE],
      modes: ['standard']
    },
    { 
      label: 'nav.payroll', 
      icon: 'pi pi-wallet', 
      routerLink: '/payroll',
      requiredRoles: [UserRole.ADMIN, UserRole.RH],
      modes: ['standard']
    },
    { 
      label: 'nav.salaryPackages', 
      icon: 'pi pi-money-bill', 
      routerLink: '/salary-packages',
      requiredRoles: [UserRole.ADMIN, UserRole.RH],
      modes: ['standard']
    },
    { 
      label: 'nav.reports', 
      icon: 'pi pi-chart-bar', 
      routerLink: '/reports',
      requiredRoles: [UserRole.ADMIN, UserRole.RH],
      modes: ['standard']
    },
    { 
      label: 'nav.attendanceReport', 
      icon: 'pi pi-file', 
      routerLink: '/reports/attendance',
      requiredRoles: [UserRole.ADMIN, UserRole.RH, UserRole.MANAGER],
      modes: ['standard']
    },
    { 
      label: 'nav.userManagement', 
      icon: 'pi pi-users', 
      routerLink: '/permissions',
      requiredRoles: [UserRole.ADMIN],
      modes: ['standard']
    }
  ];

  // === Filtered menu items based on user role with dynamic route prefix ===
  readonly menuItems = computed(() => {
    const user = this.currentUser();
    const prefix = this.routePrefix();
    const isExpert = this.isExpertMode();
    const isClientView = this.isClientView();
    
    // Use context role if available, fallback to user role
    // This ensures the role matches the selected membership context
    const contextRole = this.contextService.role();
    const effectiveRole = contextRole ?? user?.role;
    
    // Determine current mode
    let currentMode: 'expert' | 'standard' | 'expert-client' | 'expert-all' = 'standard';
    if (isExpert) {
      currentMode = isClientView ? 'expert-client' : 'expert';
    }

    if (!user || !effectiveRole) return [];

    return this.menuItemsTemplate
      .filter(item => {
        // Filter team absences based on hasSubordinates
        if (item.id === 'team-absences' && !this.authService.hasSubordinates()) {
          return false;
        }

        // 1. Check Mode
        if (item.modes) {
          // For expert-all, show if in any expert mode
          if (item.modes.includes('expert-all') && isExpert) {
            // Continue to role check
          } else if (!item.modes.includes(currentMode) && !item.modes.includes('expert-all')) {
            return false;
          }
        }

        // 2. Check Role
        // If no role restrictions, show to everyone
        if (!item.requiredRoles || item.requiredRoles.length === 0) {
          // Still check permissions if present
          if (item.requiredPermissions && item.requiredPermissions.length > 0) {
            return item.requiredPermissions.some(p => this.contextService.hasPermission(p) || (user?.permissions ?? []).includes(p));
          }
          return true;
        }
        // Check if effective role is in the required roles
        if (!item.requiredRoles.includes(effectiveRole as UserRole)) return false;

        // 3. Check Permissions (if specified)
        if (item.requiredPermissions && item.requiredPermissions.length > 0) {
          const hasPermission = item.requiredPermissions.some(p => this.contextService.hasPermission(p) || (user?.permissions ?? []).includes(p));
          if (!hasPermission) return false;
        }

        // 4. Check Employee Mode restrictions
        // Show only the appropriate page based on mode
        // NOTE: Admin and RH should still see both Attendance and Absences regardless of personal mode
        if (user?.mode) {
          const userMode = user.mode.toLowerCase();
          const isPrivilegedRole = effectiveRole === UserRole.ADMIN || effectiveRole === UserRole.RH;
          // Mode 'attendance' or 'presence' = attendance only (hide absence menu)
          if ((userMode === 'attendance' || userMode === 'presence') && item.routerLink?.includes('/absences')) {
            if (!isPrivilegedRole) return false;
          }
          // Mode 'absence' = absence only (hide attendance menu)
          if (userMode === 'absence' && item.routerLink?.includes('/attendance')) {
            if (!isPrivilegedRole) return false;
          }
        }

        return true;
      })
      .map(item => ({
        ...item,
        routerLink: `${prefix}${item.routerLink}`,
        // Keep track if this item requires company context
        requiresCompanyContext: item.requiresCompanyContext ?? false
      }));
  });

  // === Check if navigation requires company selection ===
  readonly hasSelectedClient = computed(() => this.isClientView());

  // === Navigation Handler for items requiring company context ===
  handleNavigation(item: MenuItemConfig, event: Event): boolean {
    const isExpert = this.isExpertMode();
    const hasClient = this.isClientView();
    
    // If in expert mode and item requires company context but no client is selected
    if (isExpert && item.requiresCompanyContext && !hasClient) {
      event.preventDefault();
      event.stopPropagation();
      
      // Store the intended route
      this.pendingNavigationRoute.set(item.routerLink || null);
      
      // Show the company selection dialog
      this.showCompanyRequiredDialog.set(true);
      
      return false;
    }
    
    return true;
  }

  // === Close company required dialog ===
  closeCompanyRequiredDialog(): void {
    this.showCompanyRequiredDialog.set(false);
    this.pendingNavigationRoute.set(null);
  }

  // === Navigate to select a company (via header dropdown) ===
  goToSelectCompany(): void {
    this.showCompanyRequiredDialog.set(false);
    // The user should use the header company dropdown to select a company
    // We can emit an event or just close the dialog
    // Optionally scroll to top to make the header visible
  }

  // === Profile Menu Items ===
  readonly profileMenuItems = computed<MenuItem[]>(() => {
    const user = this.currentUser();
    const prefix = this.routePrefix();
    const items: MenuItem[] = [];

    // 1. User Info Header
    if (user) {
      items.push({
        id: 'user-header',
        label: user.email,
        icon: 'pi pi-envelope',
        disabled: true
      });
      items.push({ separator: true });
    }

    // 2. Mon Compte (Personal)
    items.push({
      label: 'nav.myProfile',
      icon: 'pi pi-user',
      routerLink: `${prefix}/profile`
    });

    // 3. Gestion (Specific to Cabinets & Admins)
    if (user && [UserRole.CABINET, UserRole.ADMIN, UserRole.RH].includes(user.role as UserRole)) {
      items.push({ separator: true });

      // Show Switch Workspace if user has multiple memberships OR is a CABINET (Expert Comptable)
      if (this.hasMultipleMemberships() || user.role === UserRole.CABINET) {
        items.push({
          label: 'contextSelection.switchWorkspace',
          icon: 'pi pi-sync',
          command: () => this.switchWorkspace()
        });
      }

      if ([UserRole.ADMIN, UserRole.RH, UserRole.ADMIN_PAYZEN].includes(user.role as UserRole)) {
        items.push({
          label: 'nav.companySettings',
          icon: 'pi pi-cog',
          routerLink: `${this.routePrefix()}/company`
        });
      }
    }

    items.push({ separator: true });

    // 4. Support
    items.push({
      label: 'nav.help',
      icon: 'pi pi-question-circle',
      url: 'https://docs.payzen.ma',
      target: '_blank'
    });

    items.push({ separator: true });

    // 5. Logout
    items.push({
      id: 'logout',
      label: 'auth.logout',
      icon: 'pi pi-sign-out'
    });

    return items;
  });

  // === Methods ===
  logout(): void {
    this.authService.logout();
  }

  switchWorkspace(): void {
    // Clear current context but keep memberships
    this.contextService.clearContext();
    // Navigate to context selection
    this.router.navigate(['/select-context']);
  }

  closeSidebar(): void {
    this.closeButtonClick.emit();
  }
}
