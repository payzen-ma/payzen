import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet],
  template: `
    <div class="flex h-screen bg-gray-50 overflow-hidden">
      <!-- Sidebar -->
      <aside 
        [class]="'sidebar flex flex-col font-sans shadow-md transition-all duration-300 ease-in-out overflow-hidden fixed left-0 top-0 h-full bg-white border-r border-gray-200 z-30 ' + (isCollapsed() ? 'w-20' : 'w-64')">
        
        <!-- Brand -->
        <div class="flex items-center py-6 border-b border-gray-200" 
             [class.px-6]="!isCollapsed()" 
             [class.px-2]="isCollapsed()" 
             [class.justify-between]="!isCollapsed()" 
             [class.justify-center]="isCollapsed()">
          <div *ngIf="!isCollapsed()" class="flex items-center gap-2">
            <span class="text-primary font-bold text-2xl tracking-tight">PayZen</span>
            <span class="bg-primary/10 text-primary text-xs font-bold px-2 py-0.5 rounded-full uppercase tracking-wider">
              Monde
            </span>
          </div>

          <button
            type="button"
            (click)="toggleSidebar()"
            class="hidden md:flex items-center justify-center w-8 h-8 rounded-full text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-all"
            [attr.aria-label]="isCollapsed() ? 'Expand sidebar' : 'Collapse sidebar'">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" class="w-4 h-4">
              <path stroke-linecap="round" stroke-linejoin="round" [attr.d]="isCollapsed() ? 'M13.5 4.5 21 12m0 0-7.5 7.5M21 12H3' : 'M10.5 19.5 3 12m0 0 7.5-7.5M3 12h18'" />
            </svg>
          </button>
        </div>

        <!-- Navigation -->
        <nav class="flex-1 flex flex-col overflow-y-auto overflow-x-hidden" 
             [class]="isCollapsed() ? 'gap-1 px-2 pt-2' : 'gap-1 px-3 pt-2'">
          


          <a *ngFor="let item of menuItems"
             [href]="item.link"
             class="group flex items-center rounded-lg transition-all duration-200 hover:bg-gray-50 border border-transparent hover:border-gray-200"
             [class]="isCollapsed() ? 'justify-center p-3' : 'justify-start gap-3 px-4 py-3'"
             [title]="isCollapsed() ? item.label : ''">
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" 
                 class="w-5 h-5 text-gray-600 group-hover:text-primary transition-colors">
              <path stroke-linecap="round" stroke-linejoin="round" [attr.d]="item.icon" />
            </svg>
            <span *ngIf="!isCollapsed()" class="font-medium text-sm text-gray-700 group-hover:text-gray-900 whitespace-nowrap">
              {{ item.label }}
            </span>
          </a>
        </nav>

        <!-- User Profile -->
        <div class="mt-auto border-t border-gray-200">
          <div class="p-4">
            <a href="/settings"
               class="w-full hover:bg-gray-50 flex cursor-pointer items-center rounded-lg transition-all duration-200 border border-transparent hover:border-gray-200 p-2"
               [class]="isCollapsed() ? 'justify-center' : 'gap-3'"
               [title]="isCollapsed() ? 'Admin User' : ''">
              <div class="relative">
                <div class="w-10 h-10 bg-gray-100 text-gray-600 rounded-full flex items-center justify-center font-semibold text-sm">
                  A
                </div>
                <span class="absolute bottom-0 right-0 w-2.5 h-2.5 bg-green-500 border-2 border-white rounded-full"></span>
              </div>

              <div *ngIf="!isCollapsed()" class="flex-1 text-left overflow-hidden">
                <div class="font-semibold text-sm text-gray-900 truncate">Admin User</div>
                <div class="text-xs text-gray-500 truncate">Administrateur</div>
              </div>
              
              <svg *ngIf="!isCollapsed()" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-4 h-4 text-gray-400">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9.594 3.94c.09-.542.56-.94 1.11-.94h2.593c.55 0 1.02.398 1.11.94l.213 1.281c.063.374.313.686.645.87.074.04.147.083.22.127.325.196.72.257 1.075.124l1.217-.456a1.125 1.125 0 0 1 1.37.49l1.296 2.247a1.125 1.125 0 0 1-.26 1.431l-1.003.827c-.293.241-.438.613-.43.992a7.723 7.723 0 0 1 0 .255c-.008.378.137.75.43.991l1.004.827c.424.35.534.955.26 1.43l-1.298 2.247a1.125 1.125 0 0 1-1.369.491l-1.217-.456c-.355-.133-.75-.072-1.076.124a6.47 6.47 0 0 1-.22.128c-.331.183-.581.495-.644.869l-.213 1.281c-.09.543-.56.94-1.11.94h-2.594c-.55 0-1.019-.398-1.11-.94l-.213-1.281c-.062-.374-.312-.686-.644-.87a6.52 6.52 0 0 1-.22-.127c-.325-.196-.72-.257-1.076-.124l-1.217.456a1.125 1.125 0 0 1-1.369-.49l-1.297-2.247a1.125 1.125 0 0 1 .26-1.431l1.004-.827c.292-.24.437-.613.43-.991a6.932 6.932 0 0 1 0-.255c.007-.38-.138-.751-.43-.992l-1.004-.827a1.125 1.125 0 0 1-.26-1.43l1.297-2.247a1.125 1.125 0 0 1 1.37-.491l1.216.456c.356.133.751.072 1.076-.124.072-.044.146-.086.22-.128.332-.183.582-.495.644-.869l.214-1.28Z" />
                <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
              </svg>
            </a>

            <div class="mt-2">
              <button
                type="button"
                (click)="logout()"
                [class]="isCollapsed() ? 'w-full flex items-center justify-center p-2 rounded-lg text-gray-500 hover:text-red-600 hover:bg-red-50 transition-all' : 'w-full flex items-center justify-center gap-2 px-4 py-2 rounded-lg text-sm font-medium text-gray-500 hover:text-red-600 hover:bg-red-50 transition-all'"
                [title]="isCollapsed() ? 'Déconnexion' : ''">
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 9V5.25A2.25 2.25 0 0 0 13.5 3h-6a2.25 2.25 0 0 0-2.25 2.25v13.5A2.25 2.25 0 0 0 7.5 21h6a2.25 2.25 0 0 0 2.25-2.25V15M12 9l-3 3m0 0 3 3m-3-3h12.75" />
                </svg>
                <span *ngIf="!isCollapsed()">Déconnexion</span>
              </button>
            </div>
          </div>
        </div>
      </aside>

      <!-- Main Content Area -->
      <div class="flex-1 flex flex-col h-screen" [class]="isCollapsed() ? 'ml-20' : 'ml-64'">
        <!-- Header -->
        <header class="flex items-center justify-between px-6 py-4 bg-white border-b border-gray-200 h-16 flex-shrink-0">
          <div class="flex items-center gap-4">
            <h1 class="text-xl font-semibold text-gray-800">PayZen Backoffice</h1>
          </div>
        </header>

        <!-- Content -->
        <main class="flex-1 overflow-y-auto overflow-x-hidden">
          <router-outlet></router-outlet>
        </main>

        <!-- Footer -->
        <footer class="bg-white border-t border-gray-200 py-4 px-6 flex-shrink-0">
          <div class="text-center text-sm text-gray-500">
            © 2026 PayZen. Tous droits réservés.
          </div>
        </footer>
      </div>
    </div>
  `
})
export class AdminLayoutComponent {
  private authService = inject(AuthService);
  isCollapsed = signal(false);

  constructor(private auth: AuthService) {}

  menuItems = [
    { 
      label: 'Dashboard', 
      link: '/dashboard',
      icon: 'M3 13.125C3 12.504 3.504 12 4.125 12h2.25c.621 0 1.125.504 1.125 1.125v6.75C7.5 20.496 6.996 21 6.375 21h-2.25A1.125 1.125 0 0 1 3 19.875v-6.75ZM9.75 8.625c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125v11.25c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V8.625ZM16.5 4.125c0-.621.504-1.125 1.125-1.125h2.25C20.496 3 21 3.504 21 4.125v15.75c0 .621-.504 1.125-1.125 1.125h-2.25a1.125 1.125 0 0 1-1.125-1.125V4.125Z'
    },
    { 
      label: 'Entreprises', 
      link: '/companies',
      icon: 'M2.25 21h19.5m-18-18v18m10.5-18v18m6-13.5V21M6.75 6.75h.75m-.75 3h.75m-.75 3h.75m3-6h.75m-.75 3h.75m-.75 3h.75M6.75 21v-3.375c0-.621.504-1.125 1.125-1.125h2.25c.621 0 1.125.504 1.125 1.125V21M3 3h12m-.75 4.5H21m-3.75 3.75h.008v.008h-.008v-.008Zm0 3h.008v.008h-.008v-.008Zm0 3h.008v.008h-.008v-.008Z'
    },
    {
      label: 'Packages salariaux',
      link: '/package-salary',
      icon: 'M12 2.25c-4.556 0-8.25 3.694-8.25 8.25 0 4.555 3.694 8.25 8.25 8.25 4.555 0 8.25-3.695 8.25-8.25 0-4.556-3.695-8.25-8.25-8.25Zm0 3.75a.75.75 0 0 1 .75.75v3.75h3.75a.75.75 0 0 1 0 1.5h-4.5a.75.75 0 0 1-.75-.75V6.75a.75.75 0 0 1 .75-.75Zm-6 13.5h12a.75.75 0 0 1 0 1.5h-12a.75.75 0 0 1 0-1.5Z'
    },
    { 
      label: 'Rôles', 
      link: '/roles',
      icon: 'M9 12.75 11.25 15 15 9.75m-3-7.036A11.959 11.959 0 0 1 3.598 6 11.99 11.99 0 0 0 3 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285Z'
    },
    { 
      label: 'Permissions', 
      link: '/permissions',
      icon: 'M15.75 5.25a3 3 0 0 1 3 3m3 0a6 6 0 0 1-7.029 5.912c-.563-.097-1.159.026-1.563.43L10.5 17.25H8.25v2.25H6v2.25H2.25v-2.818c0-.597.237-1.17.659-1.591l6.499-6.499c.404-.404.527-1 .43-1.563A6 6 0 1 1 21.75 8.25Z'
    },
    {
      label: 'Jours Fériés',
      link: '/holidays',
      icon: 'M6.75 3v2.25M17.25 3v2.25M3 18.75V7.5a2.25 2.25 0 0 1 2.25-2.25h13.5A2.25 2.25 0 0 1 21 7.5v11.25m-18 0A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75m-18 0v-7.5A2.25 2.25 0 0 1 5.25 9h13.5A2.25 2.25 0 0 1 21 11.25v7.5'
    },
    {
      label: 'Référentiel',
      link: '/referentiel',
      icon: 'M3 6h18M3 12h18M3 18h18'
    },
    {
      label: "Journal d'événements",
      link: '/event-log',
      icon: 'M3 5.25h18M3 12h18M3 18.75h18'
    },
  ];

  toggleSidebar(): void {
    this.isCollapsed.update(state => !state);
  }

  logout(): void {
    this.authService.logout();
  }
}
