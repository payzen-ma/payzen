import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService, User } from '../../services/auth.service';

interface MenuItem {
  label: string;
  icon: string;
  routerLink: string;
  description?: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html'
})
export class SidebarComponent {
  @Input() isOpen: boolean = true;
  @Input() isMobile: boolean = false;
  @Output() toggle = new EventEmitter<void>();

  isCollapsed = signal(false);

  menuItems: MenuItem[] = [
    {
      label: 'Tableau de bord',
      icon: '📊',
      routerLink: '/dashboard',
      description: 'Vue d\'ensemble'
    },
    {
      label: 'Entreprises',
      icon: '🏢',
      routerLink: '/companies',
      description: 'Gestion des entreprises'
    },
    {
      label: 'Utilisateurs',
      icon: '👥',
      routerLink: '/users',
      description: 'Gestion des utilisateurs'
    },
    {
      label: 'Rôles',
      icon: '🔐',
      routerLink: '/roles',
      description: 'Gestion des rôles'
    },
    {
      label: 'Permissions',
      icon: '🔑',
      routerLink: '/permissions',
      description: 'Liste des permissions'
    },
    {
      label: 'Jours Fériés',
      icon: '📅',
      routerLink: '/holidays',
      description: 'Gestion des jours fériés globaux'
    },
    {
      label: "Journal d'événements",
      icon: '📜',
      routerLink: '/event-log',
      description: 'Logs des sociétés et employés'
    },
    {
      label: 'Paie & Réglementations',
      icon: '💼',
      routerLink: '/payroll-referentiel',
      description: 'Paramètres légaux et éléments de paie'
    }
  ];

  constructor(private authService: AuthService) {}

  toggleCollapse(): void {
    this.isCollapsed.update(state => !state);
  }

  onMenuItemClick(): void {
    if (this.isMobile) {
      this.toggle.emit();
    }
  }

  getCurrentUser(): User {
    const user = this.authService.getCurrentUser();
    return user || { id: 0, name: 'Utilisateur', email: 'user@payzen.com', role: 'N/A' };
  }

  logout(): void {
    this.authService.logout();
  }

  getTooltipPosition(): string {
    return this.isCollapsed() ? 'right' : 'none';
  }
}
