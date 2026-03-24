import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html'
})
export class PagesDashboardComponent {
  // Statistics
  stats = {
    totalCompanies: 3,
    totalEmployees: 245,
    activeCompanies: 2,
    accountingFirms: 1
  };

  // Recent companies
  recentCompanies = [
    { id: 1, name: 'Tech Solutions SARL', employees: 85, status: 'active', createdAt: '2024-01-15' },
    { id: 2, name: 'Cabinet Comptable Expert', employees: 42, status: 'active', createdAt: '2024-02-20' },
    { id: 3, name: 'Innovation Hub', employees: 118, status: 'inactive', createdAt: '2024-03-10' }
  ];

  // Companies by city (geographic distribution)
  companiesByCity = [
    { city: 'Casablanca', count: 1, percentage: 33.3 },
    { city: 'Rabat', count: 1, percentage: 33.3 },
    { city: 'Marrakech', count: 1, percentage: 33.3 }
  ];

  // Employees distribution by company
  employeesDistribution = [
    { name: 'Tech Solutions SARL', employees: 85 },
    { name: 'Innovation Hub', employees: 118 },
    { name: 'Cabinet Expert', employees: 42 }
  ];

  getStatusClass(status: string): string {
    return status === 'active' 
      ? 'bg-green-100 text-green-800' 
      : 'bg-gray-100 text-gray-800';
  }

  getStatusLabel(status: string): string {
    return status === 'active' ? 'Actif' : 'Inactif';
  }

  getMaxEmployees(): number {
    return Math.max(...this.employeesDistribution.map(c => c.employees));
  }

  getEmployeePercentage(employees: number): number {
    const max = this.getMaxEmployees();
    return (employees / max) * 100;
  }
}
