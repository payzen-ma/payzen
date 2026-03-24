import { Injectable, signal, inject } from '@angular/core';
import { Router, NavigationEnd, ActivatedRoute } from '@angular/router';
import { filter } from 'rxjs/operators';
import { CompanyContextService } from './companyContext.service';

export interface BreadcrumbItem {
  label: string;
  url?: string;
  icon?: string;
}

@Injectable({
  providedIn: 'root'
})
export class BreadcrumbService {
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private contextService = inject(CompanyContextService);

  readonly items = signal<BreadcrumbItem[]>([]);

  constructor() {
    // Listen to route changes
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.updateBreadcrumbs();
    });

    // Listen to context changes to update company name in breadcrumb
    this.contextService.contextChanged$.subscribe(() => {
      this.updateBreadcrumbs();
    });
  }

  private updateBreadcrumbs() {
    const breadcrumbs: BreadcrumbItem[] = [];
    
    // 1. Add Home/Dashboard root
    if (this.contextService.isExpertMode()) {
      breadcrumbs.push({ label: 'Cabinet Portfolio', url: '/cabinet/dashboard', icon: 'pi pi-briefcase' });
    } else {
      breadcrumbs.push({ label: 'Dashboard', url: '/app/dashboard', icon: 'pi pi-home' });
    }

    // 2. Add Context (Company Name) if active and not in portfolio view
    const context = this.contextService.currentContext();
    if (context && context.companyName && this.router.url !== '/cabinet/dashboard') {
       // If in expert client view
       if (context.isExpertMode && context.isClientView) {
         breadcrumbs.push({ label: context.companyName, icon: 'pi pi-building' });
       } 
       // If in standard mode
       else if (!context.isExpertMode) {
         // Usually redundant with Dashboard if dashboard is company-specific, 
         // but good for deep pages like /app/employees
       }
    }

    // 3. Add Route-specific segments
    // This is a simplified implementation. A robust one would traverse the ActivatedRoute tree.
    // For now, we'll map specific known routes.
    const url = this.router.url;
    
    if (url.includes('/permissions')) {
      breadcrumbs.push({ label: 'Permissions', icon: 'pi pi-lock' });
    } else if (url.includes('/audit-log')) {
      breadcrumbs.push({ label: 'Audit Log', icon: 'pi pi-history' });
    } else if (url.includes('/employees')) {
      breadcrumbs.push({ label: 'Employees', icon: 'pi pi-users' });
      if (url.includes('/create')) {
        breadcrumbs.push({ label: 'New Employee' });
      } else if (url.match(/\/employees\/\d+/)) {
        breadcrumbs.push({ label: 'Employee Profile' });
      }
    } else if (url.includes('/company')) {
      breadcrumbs.push({ label: 'Company Settings', icon: 'pi pi-cog' });
    }

    this.items.set(breadcrumbs);
  }
}
