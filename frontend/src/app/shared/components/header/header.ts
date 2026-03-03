import { Component, inject, computed, signal, OnInit, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, NavigationEnd } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { Select } from 'primeng/select';
import { BadgeModule } from 'primeng/badge';
import { TagModule } from 'primeng/tag';
import { LanguageSwitcher } from '../language-switcher/language-switcher';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { CompanyService } from '@app/core/services/company.service';
import { Company } from '@app/core/models/company.model';
import { AuthService } from '@app/core/services/auth.service';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [
    CommonModule, 
    FormsModule, 
    TranslateModule, 
    ButtonModule, 
    TooltipModule, 
    Select,
    BadgeModule,
    TagModule,
    LanguageSwitcher
  ],
  templateUrl: './header.html',
  styleUrl: './header.css',
})
export class Header implements OnInit {
  private readonly contextService = inject(CompanyContextService);
  private readonly companyService = inject(CompanyService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  // === Reactive Context Signals ===
  readonly isExpertMode = this.contextService.isExpertMode;
  readonly isClientView = this.contextService.isClientView;
  readonly companyName = this.contextService.companyName;
  readonly companyId = this.contextService.companyId;

  // === Companies for dropdown ===
  readonly clientCompanies = signal<Company[]>([]);
  readonly isLoadingCompanies = signal<boolean>(false);
  
  // === Track current route for context ===
  readonly currentRoute = signal<string>('');

  constructor() {
    // Effect to load companies whenever expert mode changes
    effect(() => {
      if (this.isExpertMode()) {
        this.loadCompanies();
      }
    });
    
    // Track navigation to maintain context
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: NavigationEnd) => {
      this.currentRoute.set(event.urlAfterRedirects);
    });
  }
  
  // Computed: All selectable options (Portfolio + Client Companies OR Memberships)
  readonly companyOptions = computed(() => {
    // If Expert Mode: Show Portfolio + Managed Companies
    if (this.isExpertMode()) {
      const current = this.contextService.currentContext();
      if (!current?.cabinetId) return [];

      // Create portfolio option with all required Company fields
      const portfolioOption: Company = {
        id: current.cabinetId,
        legalName: current.companyName || 'Portfolio',
        ice: 'PORTFOLIO',
        cnss: '',
        address: '',
        city: '',
        postalCode: '',
        country: 'Morocco',
        phone: '',
        email: '',
        rc: '',
        patente: '',
        taxRegime: 'IS' as any,
        fiscalYear: new Date().getFullYear(),
        employeeCount: 0,
        hrParameters: {
          workingDays: [],
          workingHoursPerDay: 8,
          workingHoursPerWeek: 40,
          leaveCalculationMode: 'MONTHLY',
          absenceCalculationMode: 'DAYS',
          annualLeaveDays: 22,
          publicHolidays: [],
          probationPeriodDays: 90,
          noticePeriodDays: 30
        },
        documents: {
          cnss_attestation: null,
          amo: null,
          logo: null,
          rib: null,
          other: []
        },
        isActive: true,
        createdAt: new Date(),
        updatedAt: new Date()
      };

      // Return portfolio first, then client companies
      return [portfolioOption, ...this.clientCompanies()];
    } 
    
    // If Standard Mode: Show Memberships as Company objects
    const memberships = this.contextService.memberships();
    if (memberships.length > 1) {
      return memberships.map(m => ({
        id: m.companyId,
        legalName: m.companyName,
        ice: m.role, // Storing role in ICE for display purposes temporarily
        // Mock other required fields
        cnss: '', address: '', city: '', postalCode: '', country: '', phone: '', email: '',
        hrParameters: {} as any, documents: {} as any, isActive: true, createdAt: new Date(), updatedAt: new Date()
      } as Company));
    }

    return [];
  });

  // Computed: Currently selected company from options
  readonly selectedCompany = computed(() => {
    const id = this.companyId();
    return this.companyOptions().find(c => c.id === id) || null;
  });

  // === Computed: Should show client indicator? ===
  readonly showClientIndicator = computed(() => 
    this.isExpertMode() && this.isClientView()
  );

  // === Computed: Should show company selector? ===
  readonly showCompanySelector = computed(() => {
    // Hide selector on the select-context page or on dashboard immediately
    const route = this.currentRoute();
    if (route && (route.startsWith('/select-context') || route.startsWith('/app'))) return false;

    // If user has multiple memberships, show selector in standard mode
    if (this.contextService.memberships().length > 1) return true;

    // In expert mode, hide the selector when the selected company is the expert's own portfolio (cabinet)
    if (this.isExpertMode()) {
      const selected = this.selectedCompany();
      const cabinetId = this.contextService.currentContext()?.cabinetId;
      const expertCompanyId = this.auth?.currentUser?.() ? this.auth.currentUser()?.companyId : undefined;
      if (!selected) return true; // no selection yet -> show
      // compare as strings to avoid type mismatches
      const selId = String(selected.id ?? '');
      const cabId = cabinetId !== undefined && cabinetId !== null ? String(cabinetId) : '';
      const expId = expertCompanyId !== undefined && expertCompanyId !== null ? String(expertCompanyId) : '';
      return selId !== cabId && selId !== expId;
    }

    return false;
  });

  ngOnInit(): void {
    // Load companies if in expert mode
    if (this.isExpertMode()) {
      this.loadCompanies();
    }
  }

  // === Actions ===
  switchClient(): void {
    // Reset to portfolio view and navigate to cabinet dashboard
    this.contextService.resetToPortfolioContext();
    this.router.navigate(['/cabinet/dashboard']);
  }

  loadCompanies(): void {
    this.isLoadingCompanies.set(true);
    this.companyService.getManagedCompanies().subscribe({
      next: (companies) => {
        console.log('[Header] loaded managed companies:', companies);
        this.clientCompanies.set(companies);
        this.isLoadingCompanies.set(false);
      },
      error: (err) => {
        console.error('Failed to load companies', err);
        this.isLoadingCompanies.set(false);
      }
    });
  }

  onCompanyChange(company: Company): void {
    if (!company) return;

    // If selecting the same company, do nothing
    if (company.id === this.companyId()) return;

    // Handle Expert Mode Switching
    if (this.isExpertMode()) {
      const cabinetId = this.contextService.currentContext()?.cabinetId;
      
      // Check if selecting portfolio/cabinet
      if (company.id === cabinetId) {
        // Switch back to portfolio view
        this.contextService.resetToPortfolioContext();
        // Navigate to cabinet dashboard
        this.router.navigate(['/cabinet/dashboard']);
      } else {
        // Switch to client view - don't navigate, stay on current page
        // Components will auto-refresh via contextChanged$ subscription
        this.contextService.switchToClientContext({
          id: company.id,
          legalName: company.legalName
        }, false); // Don't navigate
      }
    } else {
      // Handle Standard Mode Switching (Multi-membership)
      this.contextService.switchContext(company.id);
    }
  }
}
