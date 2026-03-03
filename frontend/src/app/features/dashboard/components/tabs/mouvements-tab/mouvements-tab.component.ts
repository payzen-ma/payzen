import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { MouvementsRhData } from '../../../state/dashboard-hr.models';
import { SectionHeaderComponent } from '../../shared/section-header/section-header.component';
import { KpiCardComponent } from '../../shared/kpi-card/kpi-card.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';

@Component({
  selector: 'app-mouvements-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, TableModule, SectionHeaderComponent, KpiCardComponent, StatusBadgeComponent],
  templateUrl: './mouvements-tab.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MouvementsTabComponent {
  readonly data = input.required<MouvementsRhData>();
  readonly selectedContractType = signal<string>('all');

  readonly contractTypeOptions = computed(() => {
    const unique = [...new Set(this.data().history.map(row => row.type).filter(Boolean))].sort((a, b) => a.localeCompare(b));
    return [
      { label: 'Tous les types', value: 'all' },
      ...unique.map(type => ({ label: type, value: type }))
    ];
  });

  readonly filteredHistory = computed(() => {
    const selected = this.selectedContractType();
    if (selected === 'all') {
      return this.data().history;
    }
    return this.data().history.filter(row => row.type === selected);
  });

  onContractTypeChange(value: string | null | undefined): void {
    this.selectedContractType.set(value || 'all');
  }
}
