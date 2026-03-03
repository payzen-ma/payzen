import { Component, signal, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { MenuItem } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';
import { Sidebar } from '../../shared/sidebar/sidebar';
import { Header } from '../../shared/components/header/header';
import { LanguageSwitcher } from '../../shared/components/language-switcher/language-switcher';
import { BreadcrumbComponent } from '../../shared/components/breadcrumb/breadcrumb';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    Sidebar,
    Header,
    LanguageSwitcher,
    BreadcrumbComponent,
    CommonModule,
    RouterModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule,
    ButtonModule,
    TranslateModule
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.css',
})
export class MainLayout {
  isSidebarOpen = signal(false);
  searchQuery = signal('');
  readonly isTablet = signal(false);
  private readonly destroyRef = inject(DestroyRef);

  // Menu items - Using PrimeIcons (pi pi-*)
  menuItems: MenuItem[] = [
    {
      label: 'Tableau de bord',
      icon: 'pi pi-home',
      routerLink: '/dashboard'
    },
    {
      label: 'Entreprise',
      icon: 'pi pi-building',
      routerLink: '/company'
    },
    {
      label: 'Employés',
      icon: 'pi pi-users',
      routerLink: '/employees'
    },
    {
      label: 'Paie',
      icon: 'pi pi-wallet',
      routerLink: '/payroll'
    },
    {
      label: 'Rapports',
      icon: 'pi pi-chart-bar',
      routerLink: '/reports'
    }
  ];

  constructor(private router: Router) {
    if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
      const mql = window.matchMedia('(min-width: 768px) and (max-width: 1023px)');

      const update = () => {
        this.isTablet.set(mql.matches);
        // Ensure the temporary mobile drawer state doesn't apply on tablet.
        if (mql.matches) {
          this.isSidebarOpen.set(false);
        }
      };

      update();

      const listener = () => update();
      if (typeof mql.addEventListener === 'function') {
        mql.addEventListener('change', listener);
        this.destroyRef.onDestroy(() => mql.removeEventListener('change', listener));
      } else {
        // Safari < 14
        // eslint-disable-next-line deprecation/deprecation
        mql.addListener(listener);
        // eslint-disable-next-line deprecation/deprecation
        this.destroyRef.onDestroy(() => mql.removeListener(listener));
      }
    }
  }

  toggleSidebar() {
    this.isSidebarOpen.update(open => !open);
  }

  closeSidebar() {
    this.isSidebarOpen.set(false);
  }

  isActiveRoute(route: string): boolean {
    return this.router.url === route;
  }

  onSearch(event: Event) {
    const value = (event.target as HTMLInputElement).value;
    this.searchQuery.set(value);
  }
}
