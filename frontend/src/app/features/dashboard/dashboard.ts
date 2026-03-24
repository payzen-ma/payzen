import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MultiSelectModule } from 'primeng/multiselect';
import { SelectModule } from 'primeng/select';
import { TabsModule } from 'primeng/tabs';
import { DashboardHrHttpRepository } from './data/dashboard-hr.http.repository';
import { DashboardHrRepository } from './data/dashboard-hr.repository';
import { DashboardHrMockRepository } from './data/dashboard-hr.mock.repository';
import { DashboardHrStore } from './state/dashboard-hr.store';
import { VueGlobaleTabComponent } from './components/tabs/vue-globale-tab/vue-globale-tab.component';
import { MouvementsTabComponent } from './components/tabs/mouvements-tab/mouvements-tab.component';
import { MasseSalarialeTabComponent } from './components/tabs/masse-salariale-tab/masse-salariale-tab.component';
import { PariteDiversiteTabComponent } from './components/tabs/parite-diversite-tab/parite-diversite-tab.component';
import { ConformiteSocialeTabComponent } from './components/tabs/conformite-sociale-tab/conformite-sociale-tab.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MultiSelectModule,
    SelectModule,
    TabsModule,
    VueGlobaleTabComponent,
    MouvementsTabComponent,
    MasseSalarialeTabComponent,
    PariteDiversiteTabComponent,
    ConformiteSocialeTabComponent
  ],
  providers: [
    DashboardHrStore,
    DashboardHrMockRepository,
    { provide: DashboardHrRepository, useClass: DashboardHrHttpRepository }
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Dashboard {
  constructor(readonly store: DashboardHrStore) {}

  sourceBadgeLabel(): string {
    return this.store.dataSource().toUpperCase();
  }

  onTabChange(value: string | number | undefined): void {
    if (value !== undefined) {
      this.store.setActiveTab(String(value));
    }
  }

  selectedMonth(): string {
    return this.store.filters().month;
  }

  onMonthChange(value: string | null | undefined): void {
    if (value) {
      this.store.setMonth(value);
    }
  }

  departmentOptions(): Array<{ label: string; value: string }> {
    return this.store.availableDepartments().map(department => ({
      label: department,
      value: department
    }));
  }

  selectedDepartments(): string[] {
    return this.store.filters().departments;
  }

  onDepartmentsChange(values: string[] | null | undefined): void {
    this.store.setDepartments(values ?? []);
  }

  parityOptions(): Array<{ label: string; value: string }> {
    return [
      { label: 'Tous', value: 'all' },
      { label: 'Femmes', value: 'F' },
      { label: 'Hommes', value: 'H' }
    ];
  }

  selectedParityMode(): string {
    const parity = this.store.filters().parity;
    if (parity.includes('F') && parity.includes('H')) {
      return 'all';
    }
    if (parity.includes('F')) {
      return 'F';
    }
    if (parity.includes('H')) {
      return 'H';
    }
    return 'all';
  }

  onParityChange(value: string | null | undefined): void {
    if (value === 'F') {
      this.store.setParity(['F']);
      return;
    }
    if (value === 'H') {
      this.store.setParity(['H']);
      return;
    }
    this.store.setParity(['F', 'H']);
  }
}
