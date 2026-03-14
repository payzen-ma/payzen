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

interface BadgeConfig {
  count: number;
  color: string;
}

interface MenuItemConfig extends MenuItem {
  requiredRoles?: UserRole[];
  requiredPermissions?: string[];
  modes?: ('expert' | 'standard' | 'expert-client' | 'expert-all')[];
  requiresCompanyContext?: boolean; // New flag to indicate if company selection is needed
  id?: string; // Unique identifier for special filtering
  groupe?: string; // Optional group for organizing menu items
  itemBadge?: BadgeConfig | null; // Badge configuration (renamed to avoid conflict with PrimeNG MenuItem)
  highlight?: boolean; // Highlight menu item (used for special features)
  notImplemented?: boolean; // Flag to mark items that are not yet implemented
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
  readonly routePrefix = computed(() => {
    // In expert mode, if viewing a client, use the standard '/app' routes
    // so client-specific pages (salary-packages, leave, etc.) resolve correctly.
    if (this.isExpertMode()) {
      return this.isClientView() ? '/app' : '/expert';
    }
    return '/app';
  });

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
    // VUE D'ENSEMBLE
    // ─────────────────────────────────────────────────────────────
    { 
      label: 'nav.dashboard', 
      icon: 'pi pi-home', 
      routerLink: '/dashboard',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH, UserRole.MANAGER, UserRole.EMPLOYEE],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'overview',
      itemBadge: null
    },
    { 
      label: 'nav.compliance', 
      icon: 'pi pi-shield', 
      routerLink: '/compliance',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'overview',
      itemBadge: null,
      notImplemented: true
    },
    { 
      label: 'nav.hrIndicators', 
      icon: 'pi pi-chart-line', 
      routerLink: '/hr-indicators',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'overview',
      itemBadge: null,
      notImplemented: true
    },

    // ─────────────────────────────────────────────────────────────
    // PAIE
    // ─────────────────────────────────────────────────────────────
    { 
      label: 'nav.payroll', 
      icon: 'pi pi-wallet', 
      routerLink: '/payroll/bulletin',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'payroll',
    },
    {
      label: 'nav.payslips',
      icon: 'pi pi-file-pdf',
      routerLink: '/payroll/payslip',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH, UserRole.MANAGER],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'payroll',
      itemBadge: null
    },
    {
      label: 'nav.myPayslip',
      icon: 'pi pi-file-pdf',
      routerLink: '/payroll/payslip',
      requiredRoles: [UserRole.EMPLOYEE],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'payroll',
      itemBadge: null
    },
    {
      label: 'nav.simulation',
      icon: 'pi pi-calculator',
      routerLink: '/payroll/simulation',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'payroll',
      itemBadge: null
    },
    {
      label: 'nav.socialDeclarations',
      icon: 'pi pi-file-check',
      routerLink: '/social-declarations',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'payroll',
      itemBadge: null,
      notImplemented: true
    },
    {
      label: 'nav.payrollExports',
      icon: 'pi pi-download',
      routerLink: '/payroll/exports',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'payroll',
      itemBadge: null,
      notImplemented: false
    },

    // ─────────────────────────────────────────────────────────────
    // COLLABORATEURS
    // ─────────────────────────────────────────────────────────────
    { 
      label: 'nav.employees', 
      icon: 'pi pi-users', 
      routerLink: '/employees',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH, UserRole.MANAGER],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'employees',
      itemBadge: null
    },
    {
      label: 'nav.absences',
      icon: 'pi pi-calendar-times',
      routerLink: '/absences/hr',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'employees',
      itemBadge: null
    },
    {
      label: 'nav.leave',
      icon: 'pi pi-calendar',
      routerLink: '/hr-leave-management',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'employees',
      itemBadge: null
    },
    {
      label: 'nav.overtime',
      icon: 'pi pi-clock',
      routerLink: '/overtime-management',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'employees',
      itemBadge: null
    },
    {
      label: 'nav.workTime',
      icon: 'pi pi-clock',
      routerLink: '/work-time',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'employees',
      itemBadge: null,
      notImplemented: true
    },
    {
      label: 'nav.pointages',
      icon: 'pi pi-clock',
      routerLink: '/payroll/pointages',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'employees',
      itemBadge: null,
      notImplemented: false
    },
    {
      label: 'nav.pointageImport',
      icon: 'pi pi-upload',
      routerLink: '/payroll/pointage-import',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'employees',
      itemBadge: null,
      notImplemented: false
    },
    {
      label: 'nav.performance',
      icon: 'pi pi-chart-bar',
      routerLink: '/performance',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH, UserRole.MANAGER],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'employees',
      itemBadge: null,
      notImplemented: true
    },
    {
      label: 'nav.expenseReports',
      icon: 'pi pi-money-bill',
      routerLink: '/expense-reports',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'employees',
      itemBadge: null,
      notImplemented: true
    },

    // ─────────────────────────────────────────────────────────────
    // INTELLIGENCE
    // ─────────────────────────────────────────────────────────────
    {
      label: 'nav.aiCopilot',
      icon: 'pi pi-sparkles',
      routerLink: '/ai-copilot',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'intelligence',
      itemBadge: null,
      highlight: true,
      notImplemented: true
    },
    {
      label: 'nav.payLang',
      icon: 'pi pi-bolt',
      routerLink: '/paylang',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'intelligence',
      itemBadge: null,
      highlight: true,
      notImplemented: true
    },
    {
      label: 'nav.legalRepository',
      icon: 'pi pi-book',
      routerLink: '/legal-repository',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'intelligence',
      itemBadge: null,
      notImplemented: true
    },

    // ─────────────────────────────────────────────────────────────
    // ADMINISTRATION
    // ─────────────────────────────────────────────────────────────
    { 
      label: 'nav.reports', 
      icon: 'pi pi-chart-bar', 
      routerLink: '/reports',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'administration',
      itemBadge: null,
      notImplemented: true
    },
    {
      label: 'nav.hrProcedures',
      icon: 'pi pi-file',
      routerLink: '/hr-procedures',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'administration',
      itemBadge: null,
      notImplemented: true
    },
    {
      label: 'nav.import',
      icon: 'pi pi-upload',
      routerLink: '/import',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'administration',
      itemBadge: null,
      notImplemented: true
    },
    {
      label: 'nav.history',
      icon: 'pi pi-history',
      routerLink: '/history',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN, UserRole.RH],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'administration',
      itemBadge: null,
      notImplemented: true
    },
    { 
      label: 'nav.settings', 
      icon: 'pi pi-cog', 
      routerLink: '/company',
      requiredRoles: [UserRole.CABINET, UserRole.ADMIN_PAYZEN, UserRole.ADMIN],
      modes: ['expert-all', 'standard'],
      requiresCompanyContext: false,
      groupe: 'administration',
      itemBadge: null
    }
  ];

  // === Group configuration ===
  private readonly groupConfig: Record<string, { label: string; order: number }> = {
    'overview': { label: 'nav.groups.overview', order: 1 },
    'payroll': { label: 'nav.groups.payroll', order: 2 },
    'employees': { label: 'nav.groups.employees', order: 3 },
    'intelligence': { label: 'nav.groups.intelligence', order: 4 },
    'administration': { label: 'nav.groups.administration', order: 5 }
  };

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

        // Hide Leave and Absences entirely in expert mode sidebar
        if (isExpert && (item.label === 'nav.leave' || item.label === 'nav.absences')) {
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
        // NOTE: Admin, RH and users in expert mode bypass these restrictions
        // since they manage all employees rather than viewing their own data.
        if (user?.mode) {
          const userMode = user.mode.toLowerCase();
          const isPrivilegedRole = effectiveRole === UserRole.ADMIN || effectiveRole === UserRole.RH || isExpert;
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
      .map(item => {
        return {
          ...item,
          routerLink: `${prefix}${item.routerLink}`,
          // Keep track if this item requires company context
          requiresCompanyContext: item.requiresCompanyContext ?? false
        };
      });
  });

  // === Grouped menu items for display ===
  readonly groupedMenuItems = computed(() => {
    const items = this.menuItems();
    const grouped = new Map<string, MenuItemConfig[]>();
    
    // Group items by their groupe property
    items.forEach(item => {
      const groupKey = item.groupe || 'ungrouped';
      if (!grouped.has(groupKey)) {
        grouped.set(groupKey, []);
      }
      grouped.get(groupKey)!.push(item);
    });

    // Convert to array and sort by group order
    const result = Array.from(grouped.entries())
      .map(([key, items]) => ({
        key,
        label: this.groupConfig[key]?.label || key,
        order: this.groupConfig[key]?.order || 999,
        items
      }))
      .sort((a, b) => a.order - b.order);

    return result;
  });

  // === Check if navigation requires company selection ===
  readonly hasSelectedClient = computed(() => this.isClientView());

  // === Navigation Handler for items requiring company context ===
  handleNavigation(item: MenuItemConfig, event: Event): boolean {
    // Block navigation for not implemented items
    if (item.notImplemented) {
      event.preventDefault();
      event.stopPropagation();
      return false;
    }
    
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

    // 3. Switch Workspace – visible for any user who can have multiple contexts:
    //    • has multiple memberships already loaded, OR
    //    • is a CABINET / RH user, OR
    //    • is an employee whose company is flagged as isCabinetExpert
    const canSwitchWorkspace =
      this.hasMultipleMemberships() ||
      user?.role === UserRole.CABINET ||
      user?.role === UserRole.RH ||
      !!user?.isCabinetExpert;

    if (canSwitchWorkspace) {
      items.push({ separator: true });
      items.push({
        label: 'contextSelection.switchWorkspace',
        icon: 'pi pi-sync',
        command: () => this.switchWorkspace()
      });
    }

    // 4. Company Settings – only for Admins and HR
    if (user && [UserRole.ADMIN, UserRole.RH, UserRole.ADMIN_PAYZEN].includes(user.role as UserRole)) {
      if (!canSwitchWorkspace) items.push({ separator: true });
      items.push({
        label: 'nav.companySettings',
        icon: 'pi pi-cog',
        routerLink: `${this.routePrefix()}/company`
      });
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
    // Rebuild memberships (clears any employee standard-mode lock so
    // the expert option reappears on the context-selection page)
    this.authService.rebuildMemberships();
    // Clear current context but keep memberships
    this.contextService.clearContext();
    // Navigate to context selection
    this.router.navigate(['/select-context']);
  }

  closeSidebar(): void {
    this.closeButtonClick.emit();
  }
}
