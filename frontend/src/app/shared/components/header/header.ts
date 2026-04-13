import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { Company } from '@app/core/models/company.model';
import { CompanyMembership } from '@app/core/models/membership.model';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyService } from '@app/core/services/company.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { TranslateModule } from '@ngx-translate/core';
import { BadgeModule } from 'primeng/badge';
import { ButtonModule } from 'primeng/button';
import { Select } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { filter } from 'rxjs/operators';
import { LanguageSwitcher } from '../language-switcher/language-switcher';

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
  // Cabinet (expert) company name: find the membership matching cabinetId so we can display it
  readonly cabinetName = computed(() => {
    const current = this.contextService.currentContext();
    const cabId = current?.cabinetId;
    if (!cabId) return null;
    const membership = this.contextService.memberships().find((m: CompanyMembership) => m.companyId === cabId);
    return membership?.companyName ?? null;
  });

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
      // Ensure we have a cabinet context before showing clients
      const current = this.contextService.currentContext();
      if (!current?.cabinetId) return [];

      // Return only client companies — the portfolio (cabinet) entry is intentionally
      // removed so experts manage clients from this selector only.
      return this.clientCompanies();
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
    // Show the selector only in expert mode. This prevents the PrimeNG
    // `p-select` label (company name) from appearing for standard/company users.
    return this.isExpertMode();
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
        // Switch to client view and navigate to client dashboard
        this.contextService.switchToClientContext({
          id: company.id,
          legalName: company.legalName
        }, false);
        // After switching context, redirect user to the client dashboard
        this.router.navigate(['/dashboard']);
      }
    } else {
      // Handle Standard Mode Switching (Multi-membership)
      this.contextService.switchContext(company.id);
    }
  }
}
