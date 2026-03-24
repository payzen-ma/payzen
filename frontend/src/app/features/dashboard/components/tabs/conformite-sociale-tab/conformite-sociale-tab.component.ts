import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TableModule } from 'primeng/table';
import { ConformiteSocialeData } from '../../../state/dashboard-hr.models';
import { SectionHeaderComponent } from '../../shared/section-header/section-header.component';
import { KpiCardComponent } from '../../shared/kpi-card/kpi-card.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';

@Component({
  selector: 'app-conformite-sociale-tab',
  standalone: true,
  imports: [CommonModule, TableModule, SectionHeaderComponent, KpiCardComponent, StatusBadgeComponent],
  templateUrl: './conformite-sociale-tab.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConformiteSocialeTabComponent {
  readonly data = input.required<ConformiteSocialeData>();
}
